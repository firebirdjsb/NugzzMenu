using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using Il2CppFishNet.Connection;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal sealed class LocalAnimationNetworkService
    {
        internal static readonly LocalAnimationNetworkService Instance = new();
        internal const string RequestPrefix = "Nugzz.LocalAnimation/";
        internal const string StateVariable = "Nugzz.LocalAnimation.State";
        private const string BodyReady = "body12-ready", BodyAck = "body12";
        private readonly Dictionary<int, float> _bodyPeers = new();
        private float _hostBodyReadyUntil;
        // Solo games still run FishNet's local server without a Steam lobby.
        // This is a native authority check, independent of client approval.
        internal static bool IsLocalAuthority
        { get { try { return Player.Local != null && Player.Local.IsLocalPlayer && Player.Local.IsServerInitialized; } catch { return false; } } }
        internal bool CanSynchronizeBody => IsLocalAuthority ||
            Time.unscaledTime < _hostBodyReadyUntil;
        internal bool MatchingPeer(Player player) => player != null && (player.IsLocalPlayer ||
            (SessionAuthorityService.Instance.IsClientApproved(player) && _bodyPeers.TryGetValue(ClientId(player), out float until) && Time.unscaledTime < until));
        private sealed class ActiveState
        {
            internal Player Player;
            internal int Action;
            internal float EndsAt, StartedAt;
            internal Vector3 Origin;
            internal readonly HashSet<int> InformedClients = new();
        }
        private readonly Dictionary<int, ActiveState> _active = new();
        private readonly List<int> _expired = new();
        private float _nextSnapshotAt;

        internal void SendRequest(Player player, int action)
        {
            if (player == null || !player.IsOwner || !player.IsClientInitialized) return;
            if (IsLocalAuthority) HandleHostRequest(player, action);
            else
                // This ServerRpc's reader validates OwnerMatches(conn). Intercept ONLY our
                // reserved messages at its native broadcast call; they must never reach
                // Animator.SetTrigger. IL2CPP inlines the generated RpcLogic method here.
                player.SendAnimationTrigger(RequestPrefix + (action + 1).ToString(CultureInfo.InvariantCulture));
        }

        internal static bool TryParseRequest(string trigger, out int action)
        {
            action = -1;
            if (trigger == null || !trigger.StartsWith(RequestPrefix, StringComparison.Ordinal)) return false;
            action = -2;
            if (string.Equals(trigger, RequestPrefix + BodyAck, StringComparison.Ordinal))
            { action = -3; return true; }
            if (trigger.Length != RequestPrefix.Length + 1) return true;
            char code = trigger[RequestPrefix.Length];
            if (code >= '0' && code <= '8') action = code - '1';
            else action = -2;
            return true;
        }

        internal void HandleHostRequest(Player source, int action)
        {
            if (!IsLocalAuthority || source == null ||
                !source.IsServerInitialized || (action != -1 && action != -3 && !LocalAnimationCatalog.IsValid(action))) return;
            int id = ClientId(source);
            if (id < 0) return;
            if (action == -3)
            {
                if (SessionAuthorityService.Instance.IsClientApproved(source)) _bodyPeers[id] = Time.unscaledTime + 10f;
                return;
            }
            if (action == -1)
            {
                if (_active.Remove(id))
                { LocalAnimationVisualService.Instance.Stop(source); Broadcast(id, -1, 0); }
                return;
            }
            if (!SessionAuthorityService.Instance.IsClientApproved(source)) return;
            if (action == LocalAnimationCatalog.Body && !source.IsLocalPlayer &&
                (!_bodyPeers.TryGetValue(id, out float readyUntil) || Time.unscaledTime >= readyUntil)) return;
            if (source.IsRagdolled || source.CurrentVehicle != null || source.Health == null || !source.Health.IsAlive) return;
            if (_active.TryGetValue(id, out ActiveState previous) &&
                Time.unscaledTime - previous.StartedAt < 0.25f) return;
            if (!source.IsLocalPlayer &&
                !LocalAnimationVisualService.Instance.Start(source, action, LocalAnimationCatalog.Duration)) return;
            _active[id] = new ActiveState { Player = source, Action = action,
                EndsAt = Time.unscaledTime + LocalAnimationCatalog.Duration,
                StartedAt = Time.unscaledTime, Origin = source.transform.position };
            InformNewClients(id, _active[id]);
        }

        internal bool TryReceiveNetworkValue(Player source, string name, string value)
        {
            if (!string.Equals(name, StateVariable, StringComparison.Ordinal)) return false;
            // Only the known host may identify another actor in an observer message.
            if (IsLocalAuthority || !SessionAuthorityService.IsHostPlayer(source) ||
                !SessionAuthorityService.Instance.FeaturesAllowed)
                return true;
            if (string.Equals(value, BodyReady, StringComparison.Ordinal))
            {
                _hostBodyReadyUntil = Time.unscaledTime + 10f;
                Player local = Player.Local;
                if (local != null && local.IsOwner && local.IsClientInitialized)
                    local.SendAnimationTrigger(RequestPrefix + BodyAck);
                return true;
            }
            if (!TryParseState(value, out int id, out int action, out int seconds, out int elapsedMs)) return true;
            if (action == LocalAnimationCatalog.Body && !CanSynchronizeBody) return true;
            var players = Player.PlayerList;
            if (players == null) return true;
            for (int i = 0; i < players.Count; i++)
            {
                Player actor = players[i];
                if (actor == null || ClientId(actor) != id) continue;
                if (actor.IsLocalPlayer)
                {
                    if (action == LocalAnimationCatalog.Body && LocalAnimationService.Instance.ActiveAction == action)
                        LocalBodyAnimationService.Instance.Synchronize(actor, seconds, elapsedMs / 1000f);
                    continue;
                }
                if (action < 0) LocalAnimationVisualService.Instance.Stop(actor);
                else LocalAnimationVisualService.Instance.Start(actor, action, seconds, elapsedMs / 1000f);
                break;
            }
            return true;
        }

        internal static bool TryParseState(string value, out int id, out int action, out int seconds)
            => TryParseState(value, out id, out action, out seconds, out _);

        internal static bool TryParseState(string value, out int id, out int action, out int seconds, out int elapsedMs)
        {
            id = action = seconds = -1; elapsedMs = 0;
            if (string.IsNullOrEmpty(value) || value.Length > 32) return false;
            string[] parts = value.Split('|');
            if ((parts.Length != 3 && parts.Length != 4) || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out id) || id < 0 ||
                !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int code) || code < 0 || code > 8 ||
                !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out seconds) || seconds < 0 || seconds > LocalAnimationCatalog.Duration) return false;
            action = code - 1;
            if (action == LocalAnimationCatalog.Body)
                return parts.Length == 4 && seconds > 0 &&
                    int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out elapsedMs) && elapsedMs >= 0 && elapsedMs < 20000 &&
                    seconds == (int)Math.Ceiling(LocalAnimationCatalog.Duration - elapsedMs / 1000f);
            return parts.Length == 3 && (action == -1 ? seconds == 0 : seconds > 0);
        }

        private static void Broadcast(int id, int action, int seconds) =>
            PlayerValueRpcService.BroadcastToApprovedClients(Player.Local, StateVariable,
                id.ToString(CultureInfo.InvariantCulture) + "|" + (action + 1).ToString(CultureInfo.InvariantCulture) + "|" + seconds.ToString(CultureInfo.InvariantCulture));

        internal void Update()
        {
            if (!IsLocalAuthority) return;
            _expired.Clear();
            foreach (var entry in _active)
            {
                try
                {
                    ActiveState state = entry.Value;
                    Player p = state.Player;
                    if (p == null || !p.IsServerInitialized || !SessionAuthorityService.Instance.IsClientApproved(p) ||
                        Time.unscaledTime >= state.EndsAt || p.IsRagdolled || p.CurrentVehicle != null ||
                        p.Health == null || !p.Health.IsAlive || (p.transform.position - state.Origin).sqrMagnitude > 0.36f)
                        _expired.Add(entry.Key);
                }
                catch { _expired.Add(entry.Key); }
            }
            foreach (int id in _expired)
            {
                LocalAnimationVisualService.Instance.Stop(_active[id].Player);
                _active.Remove(id);
                Broadcast(id, -1, 0);
            }
            if (Time.unscaledTime >= _nextSnapshotAt)
            {
                _nextSnapshotAt = Time.unscaledTime + 2f;
                PlayerValueRpcService.BroadcastToApprovedClients(Player.Local, StateVariable, BodyReady);
                foreach (var entry in _active)
                {
                    try { InformNewClients(entry.Key, entry.Value); }
                    catch { /* A native player can disappear while its observer RPC is queued. */ }
                }
            }
        }

        private void InformNewClients(int actorId, ActiveState state)
        {
            var players = Player.PlayerList;
            if (players == null) return;
            bool newObserver = false;
            int seconds = Math.Max(1, (int)Math.Ceiling(state.EndsAt - Time.unscaledTime));
            string value = actorId.ToString(CultureInfo.InvariantCulture) + "|" + (state.Action + 1).ToString(CultureInfo.InvariantCulture) + "|" + seconds.ToString(CultureInfo.InvariantCulture);
            if (state.Action == LocalAnimationCatalog.Body)
            {
                int elapsedMs = Math.Max(0, Math.Min(19999, (int)Math.Floor((Time.unscaledTime - state.StartedAt) * 1000f)));
                seconds = (int)Math.Ceiling(LocalAnimationCatalog.Duration - elapsedMs / 1000f);
                value = actorId.ToString(CultureInfo.InvariantCulture) + "|7|" + seconds.ToString(CultureInfo.InvariantCulture) + "|" + elapsedMs.ToString(CultureInfo.InvariantCulture);
            }
            for (int i = 0; i < players.Count; i++)
            {
                Player recipient = players[i];
                int clientId = ClientId(recipient);
                if (recipient == null || recipient.IsLocalPlayer || clientId < 0) continue;
                if (!SessionAuthorityService.Instance.IsClientApproved(recipient))
                { state.InformedClients.Remove(clientId); continue; }
                if (state.InformedClients.Contains(clientId)) continue;
                if (state.Action == LocalAnimationCatalog.Body &&
                    (!_bodyPeers.TryGetValue(clientId, out float readyUntil) || Time.unscaledTime >= readyUntil)) continue;
                if (PlayerValueRpcService.SendToClient(Player.Local, recipient, StateVariable, value))
                { state.InformedClients.Add(clientId); newObserver = true; }
            }
            if (newObserver)
            {
                // Native animation bool RPCs are not buffered for a newly joined observer.
                // The host may broadcast the still-active pose; clients never call this API.
                foreach (string name in LocalAnimationCatalog.Actions[state.Action].DisabledBools)
                    state.Player.SetAnimationBool(name, false);
                foreach (string name in LocalAnimationCatalog.Actions[state.Action].EnabledBools)
                    state.Player.SetAnimationBool(name, true);
            }
        }

        internal void Reset()
        { _active.Clear(); _expired.Clear(); _bodyPeers.Clear(); _hostBodyReadyUntil = 0f; _nextSnapshotAt = 0f; }
        private static int ClientId(Player player)
        { try { return player?.Owner?.ClientId ?? -1; } catch { return -1; } }
    }

    // Both SendAnimationTrigger and its owner-checked server reader call this method
    // directly in 0.4.7f12. Patching only RpcLogic___SendAnimationTrigger misses those calls.
    [HarmonyPatch(typeof(Player), "SetAnimationTrigger_Networked", new[] { typeof(NetworkConnection), typeof(string) })]
    internal static class LocalAnimationRequestPatch
    {
        private static bool Prefix(Player __instance, string trigger)
        {
            if (!LocalAnimationNetworkService.TryParseRequest(trigger, out int action)) return true;
            try { LocalAnimationNetworkService.Instance.HandleHostRequest(__instance, action); }
            catch (Exception ex) { DebugLogService.Instance.VerboseWarning("Local animation request failed: " + ex.Message); }
            return false;
        }
    }
}
