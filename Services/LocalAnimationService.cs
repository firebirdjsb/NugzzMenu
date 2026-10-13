using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Core.Equipping.Framework;
using Il2CppScheduleOne.Equipping.Framework;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal sealed class LocalAnimationService
    {
        internal static readonly LocalAnimationService Instance = new();
        private readonly Dictionary<string, bool> _previous = new(StringComparer.Ordinal);
        private Player _player;
        private Animator _animator;
        private IEquippedItemHandler _phone;
        private Vector3 _origin;
        private float _endsAt, _nextActionAt;
        private string _failure = string.Empty;
        internal int ActiveAction { get; private set; } = -1;
        internal string Status => ActiveAction < 0 ? (string.IsNullOrEmpty(_failure) ? "Stopped" : _failure) :
            LocalAnimationCatalog.Actions[ActiveAction].Label + " · " +
            Math.Max(0, (int)Math.Ceiling(_endsAt - Time.unscaledTime)) + "s";

        internal bool Play(int action)
        {
            if (!LocalAnimationCatalog.IsValid(action)) return false;
            if (!SessionAuthorityService.Instance.FeaturesAllowed)
                return Fail("The host must approve NugzzMenu first");
            if (Time.unscaledTime < _nextActionAt) return false;
            if (action == LocalAnimationCatalog.Body && !LocalAnimationNetworkService.Instance.CanSynchronizeBody)
                return Fail("Host needs this matching animation/motorcycle build");
            Player player = GameManagerService.Instance.GetLocalPlayer();
            try
            {
                if (player == null || !player.IsLocalPlayer || !player.IsOwner || !player.IsClientInitialized)
                    return Fail("Wait for your player to finish joining");
                if (player.IsRagdolled || player.CurrentVehicle != null || player.Health == null || !player.Health.IsAlive)
                    return Fail("Stand up and leave the vehicle first");
                if (NativeActivityGateService.Instance.TryGetBlockReason(out string reason))
                    return Fail("Animation unavailable while " + reason);
                Animator animator = player.Avatar?.Animation?._animator;
                if (animator == null || animator.runtimeAnimatorController == null)
                    return Fail("Your avatar animator is not ready");
                var definition = LocalAnimationCatalog.Actions[action];
                var parameters = new HashSet<string>(StringComparer.Ordinal);
                foreach (var parameter in animator.parameters)
                    if (parameter.type == AnimatorControllerParameterType.Bool) parameters.Add(parameter.name);
                foreach (string name in definition.EnabledBools)
                    if (!parameters.Contains(name)) return Fail("This avatar does not support " + name);
                foreach (string name in definition.DisabledBools)
                    if (!parameters.Contains(name)) return Fail("This avatar does not support " + name);
                if (!LocalAnimationVisualService.Instance.CanCreate(action, out string visualReason))
                    return Fail(visualReason);
                if (action == LocalAnimationCatalog.Body && !LocalBodyAnimationService.Instance.CanAnimate(player))
                    return Fail("Your avatar's body joints are not ready");
                Stop(false);
                _failure = string.Empty;
                _player = player;
                _animator = animator;
                _origin = player.transform.position;
                ActiveAction = action;
                _endsAt = Time.unscaledTime + LocalAnimationCatalog.Duration;
                _nextActionAt = Time.unscaledTime + 0.3f;
                // Preserve only the parameters this action owns. Native seats and inventory
                // remain under the game's control; we never clear every animator parameter.
                foreach (string name in definition.EnabledBools) Remember(name);
                foreach (string name in definition.DisabledBools) Remember(name);
                if (action == LocalAnimationCatalog.Phone)
                {
                    EquippableData phone = LocalAnimationVisualService.Instance.PhoneData;
                    var equipper = player.GetComponent<PlayerNetworkedEquipper>();
                    if (phone == null || equipper == null || !equipper.CanEquip(phone))
                    { Stop(false); return Fail("Put away the item in your hand before using the phone animation"); }
                    _phone = player.Equip_Networked(phone);
                    if (_phone == null) { Stop(false); return Fail("The native phone could not be equipped"); }
                }
                foreach (string name in definition.DisabledBools) player.SendAnimationBool(name, false);
                foreach (string name in definition.EnabledBools) player.SendAnimationBool(name, true);
                if (!LocalAnimationVisualService.Instance.Start(player, action, LocalAnimationCatalog.Duration))
                { Stop(false); return Fail("The native animation visual could not be created"); }
                // SendAnimationBool is an owner-checked ServerRpc -> ObserversRpc. Do not
                // call SetAnimationBool on clients; that is the server's broadcast method.
                LocalAnimationNetworkService.Instance.SendRequest(player, action);
                NotificationService.Instance.Status("Playing " + definition.Label + " (20s; move or Stop to end)");
                return true;
            }
            catch (Exception ex)
            {
                Stop(false);
                return Fail("Animation could not start: " + ex.Message);
            }
        }

        private void Remember(string name) => _previous[name] = _animator.GetBool(name);

        internal void Stop(bool feedback = true)
        {
            Player player = _player;
            bool active = ActiveAction >= 0;
            ActiveAction = -1;
            _player = null;
            if (player != null)
            {
                // Unequip only the temporary handler we created, never the hotbar item.
                try { if (_phone != null) player.Unequip(_phone); } catch { }
                foreach (var previous in _previous)
                {
                    try
                    {
                        if (player.IsOwner && player.IsClientInitialized)
                            player.SendAnimationBool(previous.Key, previous.Value);
                        else if (_animator != null) _animator.SetBool(previous.Key, previous.Value);
                    }
                    catch { }
                }
                LocalAnimationVisualService.Instance.Stop(player);
                try { if (active) LocalAnimationNetworkService.Instance.SendRequest(player, -1); } catch { }
            }
            _phone = null;
            _animator = null;
            _previous.Clear();
            if (feedback) { _failure = string.Empty; NotificationService.Instance.Status("Local animation stopped"); }
        }

        internal void Update()
        {
            if (ActiveAction >= 0)
            {
                try
                {
                    if (_player == null || !SessionAuthorityService.Instance.FeaturesAllowed ||
                        !_player.IsOwner || !_player.IsClientInitialized || Time.unscaledTime >= _endsAt ||
                        _player.Avatar?.Animation?._animator != _animator ||
                        _player.IsRagdolled || _player.CurrentVehicle != null || !_player.Health.IsAlive ||
                        (_player.transform.position - _origin).sqrMagnitude > 0.36f ||
                        _player.Velocity.sqrMagnitude > 0.5f ||
                        NativeActivityGateService.Instance.TryGetBlockReason(out _)) Stop(false);
                }
                catch { Stop(false); }
            }
            LocalAnimationNetworkService.Instance.Update();
            LocalAnimationVisualService.Instance.Update();
        }

        internal void ResetForScene()
        {
            Stop(false);
            _failure = string.Empty;
            _nextActionAt = 0f;
            LocalAnimationNetworkService.Instance.Reset();
            LocalAnimationVisualService.Instance.Reset();
        }

        private static bool Fail(string reason)
        { Instance._failure = reason; NotificationService.Instance.Warning(reason); return false; }
    }
}
