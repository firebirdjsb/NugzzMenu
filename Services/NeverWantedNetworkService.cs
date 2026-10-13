using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppFishNet.Connection;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    // The native SendAnimationTrigger server reader validates OwnerMatches.
    // No actor ID is accepted in the payload: __instance is the requesting pawn.
    internal static class NeverWantedNetworkService
    {
        internal const string RequestPrefix = "Nugzz.NeverWanted/";
        private static readonly Dictionary<Player, float> Remote = new();
        private static readonly List<Player> Expired = new();
        private static float _nextSend;
        private static bool? _lastSent;

        internal static bool IsAuthority => ManagerCacheService.Instance.LocalPlayer?.IsServerInitialized == true;
        internal static bool Protects(Player player) => IsAuthority && player != null &&
            SessionAuthorityService.Instance.IsClientApproved(player) &&
            Remote.TryGetValue(player, out float until) && Time.unscaledTime < until;

        internal static bool TryParse(string trigger, out bool enabled)
        {
            enabled = false;
            if (trigger == null || !trigger.StartsWith(RequestPrefix, StringComparison.Ordinal)) return false;
            enabled = trigger == RequestPrefix + "1";
            return true;
        }

        internal static void Receive(Player source, string trigger)
        {
            if (!IsAuthority || source == null || !source.IsServerInitialized ||
                (trigger != RequestPrefix + "0" && trigger != RequestPrefix + "1")) return;
            if (trigger == RequestPrefix + "0") { Remote.Remove(source); return; }
            if (!SessionAuthorityService.Instance.FeaturesAllowed || !SessionAuthorityService.Instance.IsClientApproved(source)) return;
            if (Remote.Count >= 32 && !Remote.ContainsKey(source)) return;
            Remote[source] = Time.unscaledTime + 10f;
            NeverWantedService.RefreshImmediately();
        }

        internal static void LocalChanged() { _lastSent = null; _nextSend = 0f; }

        internal static void Update()
        {
            if (IsAuthority)
            {
                Expired.Clear();
                foreach (var entry in Remote)
                    if (entry.Key == null || !entry.Key.IsServerInitialized ||
                        !SessionAuthorityService.Instance.IsClientApproved(entry.Key) || Time.unscaledTime >= entry.Value)
                        Expired.Add(entry.Key);
                foreach (Player player in Expired) Remote.Remove(player);
                return;
            }
            Player local = ManagerCacheService.Instance.LocalPlayer;
            // The matching-build heartbeat prevents reserved requests reaching an
            // older/unmodified host's Animator. Local protection stays available.
            if (local == null || !local.IsOwner || !local.IsClientInitialized ||
                !LocalAnimationNetworkService.Instance.CanSynchronizeBody) return;
            bool enabled = PlayerCheatService.Instance.NeverWanted && SessionAuthorityService.Instance.FeaturesAllowed;
            if (_lastSent == enabled && (!enabled || Time.unscaledTime < _nextSend)) return;
            local.SendAnimationTrigger(RequestPrefix + (enabled ? "1" : "0"));
            _lastSent = enabled; _nextSend = Time.unscaledTime + 3f;
        }

        internal static void Reset()
        { Remote.Clear(); Expired.Clear(); _lastSent = null; _nextSend = 0f; NeverWantedService.RefreshImmediately(); }
    }

    [HarmonyPatch(typeof(Player), "SetAnimationTrigger_Networked", new[] { typeof(NetworkConnection), typeof(string) })]
    internal static class NeverWantedRequestPatch
    {
        private static bool Prefix(Player __instance, string trigger)
        {
            if (!NeverWantedNetworkService.TryParse(trigger, out _)) return true;
            try { NeverWantedNetworkService.Receive(__instance, trigger); }
            catch (Exception ex) { DebugLogService.Instance.VerboseWarning("Never Wanted request failed: " + ex.Message); }
            return false;
        }
    }
}
