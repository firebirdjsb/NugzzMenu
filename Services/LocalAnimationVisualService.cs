using System;
using System.Collections.Generic;
using Il2CppScheduleOne.AvatarFramework.Animation;
using Il2CppScheduleOne.Core.Equipping.Framework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal sealed class LocalAnimationVisualService
    {
        internal static readonly LocalAnimationVisualService Instance = new();
        private sealed class Visual
        {
            internal Player Player;
            internal AvatarAnimation Rig;
            internal GameObject Root;
            internal Transform Wheel, Seat, Hip, AvatarRoot;
            internal Vector3 Origin, AvatarPosition;
            internal Quaternion FacingOffset;
            internal float EndsAt, WheelSpeed;
            internal int Action, EvaluatedFrame = -1;
        }
        private readonly Dictionary<int, Visual> _visuals = new();
        private readonly List<int> _expired = new();
        private EquippableData _phone;
        private GameObject _laptop;
        private AvatarSeat _chair;
        private Motorbike _motorcycle;
        internal EquippableData PhoneData => _phone;
        internal Motorbike MotorcycleAsset
        { get { CanCreate(LocalAnimationCatalog.Motorcycle, out _); return _motorcycle; } }
        internal bool CanCreate(int action, out string reason)
        {
            reason = string.Empty;
            if (action == LocalAnimationCatalog.Phone && _phone == null)
                foreach (var data in Resources.FindObjectsOfTypeAll<EquippableData>())
                    if (data != null && data.name == "PhoneEquippable") { _phone = data; break; }
            if (action == LocalAnimationCatalog.Laptop)
            {
                if (_laptop == null) foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                    if (go != null && go.name == "Laptop" && go.GetComponentsInChildren<Renderer>(true).Length > 0) { _laptop = go; break; }
                if (_chair == null) foreach (var seat in Resources.FindObjectsOfTypeAll<AvatarSeat>())
                    if (seat != null && seat.SittingPoint != null && seat.gameObject.name.StartsWith("Chair_Business_Sittable", StringComparison.Ordinal)) { _chair = seat; break; }
            }
            if (action == LocalAnimationCatalog.Motorcycle && _motorcycle == null)
                foreach (var bike in Resources.FindObjectsOfTypeAll<Motorbike>())
                    if (bike != null && bike.Seat?.SittingPoint != null && bike.BurnoutController?.rearWheelVisual != null) { _motorcycle = bike; break; }
            bool found = action switch
            {
                LocalAnimationCatalog.Phone => _phone != null,
                LocalAnimationCatalog.Laptop => _laptop != null && _chair != null,
                LocalAnimationCatalog.Motorcycle => _motorcycle != null,
                _ => LocalAnimationFxService.Instance.CanCreate(action)
            };
            if (!found) reason = "The native " + LocalAnimationCatalog.Actions[action].Label + " asset is not loaded yet; try again after loading into town";
            return found;
        }
        internal bool Start(Player player, int action, float duration, float elapsed = 0f)
        {
            if (player == null || !LocalAnimationCatalog.IsValid(action) || duration <= 0f || !LocalAnimationRenderService.EnsureInitialized()) return false;
            Stop(player);
            if (action == LocalAnimationCatalog.Body) return LocalBodyAnimationService.Instance.Start(player, duration, elapsed);
            if (action != LocalAnimationCatalog.Laptop && action != LocalAnimationCatalog.Motorcycle)
                return LocalAnimationFxService.Instance.Start(player, action, duration);
            GameObject root = null;
            try
            {
                if (!CanCreate(action, out _) || !LocalAnimationPlacementService.Ground(player, player.transform.position, out Vector3 ground)) return false;
                Transform hip = LocalAnimationPlacementService.Hip(player);
                Transform avatarRoot = LocalAnimationPlacementService.PoseRoot(player);
                if (hip == null || hip.parent == null || avatarRoot == null) return false;
                root = new GameObject("Nugzz local animation visual");
                bool laptop = action == LocalAnimationCatalog.Laptop;
                Transform source = laptop ? _chair.transform : _motorcycle.transform;
                Transform sourceSeat = laptop ? _chair.SittingPoint : _motorcycle.Seat.SittingPoint;
                var transforms = new Dictionary<int, Transform>();
                Transform copiedModel = NativeVisualCloneService.Copy(source, root.transform, transforms);
                Transform seat = transforms[sourceSeat.GetInstanceID()];
                root.transform.SetPositionAndRotation(ground, LocalAnimationPlacementService.Facing(source, sourceSeat, player.transform.rotation));
                var visual = new Visual { Player = player, Rig = player.Avatar.Animation, Root = root, Seat = seat,
                    Hip = hip, AvatarRoot = avatarRoot, AvatarPosition = avatarRoot.localPosition,
                    Origin = player.transform.position, Action = action,
                    FacingOffset = Quaternion.Inverse(player.transform.rotation) * root.transform.rotation,
                    EndsAt = Time.unscaledTime + Math.Min(duration, LocalAnimationCatalog.Duration) };
                if (laptop)
                {
                    Transform model = NativeVisualCloneService.Copy(_laptop.transform, root.transform, new());
                    model.SetPositionAndRotation(seat.position + player.transform.forward * 0.43f + Vector3.up * 0.23f, player.transform.rotation);
                }
                else
                {
                    visual.Wheel = transforms[_motorcycle.BurnoutController.rearWheelVisual.GetInstanceID()];
                    visual.WheelSpeed = _motorcycle.BurnoutController.rearWheelRPM * 6f;
                }
                _visuals[player.GetInstanceID()] = visual;
                if (!LocalAnimationFxService.Instance.Start(player, action, duration, copiedModel))
                { Stop(player); return false; }
                return true;
            }
            catch (Exception ex)
            {
                if (root != null) UnityEngine.Object.Destroy(root);
                DebugLogService.Instance.VerboseWarning("Local animation visual failed: " + ex.Message);
                return false;
            }
        }
        internal void LateUpdate()
        {
            foreach (Visual visual in _visuals.Values)
                if (Valid(visual) && visual.Hip != null && visual.Seat != null)
                {
                    visual.Root.transform.rotation = visual.Player.transform.rotation * visual.FacingOffset;
                    var definition = LocalAnimationCatalog.Actions[visual.Action];
                    foreach (string name in definition.EnabledBools) visual.Rig._animator.SetBool(name, true);
                    foreach (string name in definition.DisabledBools) visual.Rig._animator.SetBool(name, false);
                    if (visual.EvaluatedFrame != Time.frameCount)
                    {
                        // Native player/IK updates can replace the seated pose after
                        // our Update. Evaluate owned flags once before aligning the
                        // whole avatar; camera callbacks must not advance animation.
                        visual.Rig._animator.Update(0f);
                        visual.EvaluatedFrame = Time.frameCount;
                    }
                    // Move every visual/attachment together. Moving the hip alone
                    // after culling can separate rigid hair from the skinned body.
                    LocalAnimationPlacementService.Align(visual.AvatarRoot, visual.Hip, visual.Seat);
                }
        }
        private static bool Valid(Visual v) => v.Player != null && v.Root != null && Time.unscaledTime < v.EndsAt &&
            !v.Player.IsRagdolled && v.Player.CurrentVehicle == null && v.Player.Health != null && v.Player.Health.IsAlive &&
            v.Player.Avatar?.Animation == v.Rig && LocalAnimationPlacementService.Hip(v.Player) == v.Hip &&
            v.Player.Avatar.transform == v.AvatarRoot && (v.Player.transform.position - v.Origin).sqrMagnitude <= 0.36f &&
            SessionAuthorityService.Instance.FeaturesAllowed;
        private static void Dispose(Visual visual)
        {
            LocalAnimationFxService.Instance.Stop(visual.Player);
            if (visual.Player != null && !visual.Player.IsRagdolled && visual.Player.Health != null && visual.Player.Health.IsAlive &&
                visual.Player.Avatar?.Animation == visual.Rig && visual.Player.Avatar.transform == visual.AvatarRoot &&
                visual.Player.CurrentVehicle == null && visual.AvatarRoot != null)
                visual.AvatarRoot.localPosition = visual.AvatarPosition;
            if (visual.Root != null) UnityEngine.Object.Destroy(visual.Root);
        }
        internal void Stop(Player player)
        {
            LocalBodyAnimationService.Instance.Stop(player);
            LocalAnimationFxService.Instance.Stop(player);
            try { if (player != null && _visuals.Remove(player.GetInstanceID(), out Visual visual)) Dispose(visual); } catch { }
        }
        internal void Update()
        {
            LocalBodyAnimationService.Instance.Update(); LocalAnimationFxService.Instance.Update(); _expired.Clear();
            foreach (var entry in _visuals)
            {
                try
                {
                    Visual v = entry.Value;
                    if (!Valid(v)) _expired.Add(entry.Key);
                    else
                    {
                        foreach (string name in LocalAnimationCatalog.Actions[v.Action].EnabledBools) v.Rig._animator.SetBool(name, true);
                        foreach (string name in LocalAnimationCatalog.Actions[v.Action].DisabledBools) v.Rig._animator.SetBool(name, false);
                        if (v.Wheel != null) v.Wheel.Rotate(Vector3.right, v.WheelSpeed * Time.deltaTime, Space.Self);
                    }
                }
                catch { _expired.Add(entry.Key); }
            }
            foreach (int id in _expired) { Dispose(_visuals[id]); _visuals.Remove(id); }
        }
        internal void Reset()
        {
            LocalBodyAnimationService.Instance.Reset();
            LocalAnimationFxService.Instance.Reset();
            foreach (Visual visual in _visuals.Values) Dispose(visual);
            _visuals.Clear(); _expired.Clear(); _phone = null; _laptop = null; _chair = null; _motorcycle = null;
        }
    }
}
