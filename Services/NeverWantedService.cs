using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class NeverWantedService
    {
        private static float _nextCleanup;
        internal static void RefreshImmediately() => _nextCleanup = 0f;
        internal static IEnumerable<MethodBase> Targets(Type type, params string[] names)
        {
            foreach (string name in names)
            {
                MethodBase method = type.GetMethod(name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null) yield return method;
            }
        }
        internal static bool Protects(Player player)
        {
            try
            {
                return SessionAuthorityService.Instance.FeaturesAllowed && player != null &&
                    ((PlayerCheatService.Instance.NeverWanted && player == ManagerCacheService.Instance.LocalPlayer) ||
                     NeverWantedNetworkService.Protects(player));
            }
            catch { return false; }
        }

        internal static bool ProtectsCode(string code)
        {
            try
            {
                Player local = ManagerCacheService.Instance.LocalPlayer;
                if (string.IsNullOrEmpty(code)) return false;
                if (Protects(local) && code == local.PlayerCode) return true;
                if (Player.PlayerList != null) foreach (Player player in Player.PlayerList)
                    if (Protects(player) && code == player.PlayerCode) return true;
                return false;
            }
            catch { return false; }
        }

        internal static bool IsProtectedPoliceTarget(CombatBehaviour behaviour, NetworkObject target)
        {
            try
            {
                return target != null && behaviour?.Npc?.TryCast<PoliceOfficer>() != null &&
                    Protects(target.GetComponent<Player>());
            }
            catch { return false; }
        }

        internal static void Update()
        {
            try
            {
                NeverWantedNetworkService.Update();
                Player local = ManagerCacheService.Instance.LocalPlayer;
                ClearProtectedCrime(local);
                if (!NeverWantedNetworkService.IsAuthority) return;
                if (Player.PlayerList != null) foreach (Player player in Player.PlayerList)
                    if (player != local) ClearProtectedCrime(player);
                if (Time.unscaledTime < _nextCleanup) return;
                _nextCleanup = Time.unscaledTime + 0.25f;

                var officers = PoliceOfficer.Officers;
                for (int i = 0; officers != null && i < officers.Count; i++)
                {
                    var officer = officers[i];
                    if (officer == null) continue;
                    try
                    {
                        if (Protects(officer.PursuitBehaviour?.TargetPlayer) && officer.PursuitBehaviour.Enabled)
                            officer.PursuitBehaviour.Disable_Server();
                        if (Protects(officer.VehiclePursuitBehaviour?.Target) && officer.VehiclePursuitBehaviour.Enabled)
                            officer.VehiclePursuitBehaviour.Disable_Server();
                        if (Protects(officer.BodySearchBehaviour?.TargetPlayer) && officer.BodySearchBehaviour.Enabled)
                            officer.BodySearchBehaviour.Disable_Server();
                        if (Protects(officer.currentBodySearchInvestigation?.Target))
                            officer.StopBodySearchInvestigation();
                        var combat = officer.Behaviour?.CombatBehaviour;
                        if (combat?.Target?.NetworkObject != null && combat.Enabled &&
                            Protects(combat.Target.NetworkObject.GetComponent<Player>()))
                            combat.Disable_Server();
                    }
                    catch (Exception ex) { DebugLogService.Instance.VerboseException("Police cleanup", ex); }
                }
            }
            catch (Exception ex) { DebugLogService.Instance.VerboseException("Never Wanted", ex); }
        }

        private static void ClearProtectedCrime(Player player)
        {
            if (!Protects(player)) return;
            var crime = player.CrimeData;
            if (crime == null) return;
            if (crime.Crimes != null && crime.Crimes.Count > 0) crime.ClearCrimes();
            if (crime.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
                crime.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.None);
            if (crime.CurrentArrestProgress > 0f) crime.SetArrestProgress(0f);
            if (crime.CurrentBodySearchProgress > 0f) crime.SetBodySearchProgress(0f);
        }
    }

    [HarmonyPatch]
    internal static class NeverWantedCrimePatch
    {
        private static IEnumerable<MethodBase> TargetMethods() => NeverWantedService.Targets(typeof(PlayerCrimeData),
            nameof(PlayerCrimeData.AddCrime), nameof(PlayerCrimeData.Escalate));
        private static bool Prefix(PlayerCrimeData __instance) => !NeverWantedService.Protects(__instance?.Player);
    }

    [HarmonyPatch(typeof(PlayerCrimeData), nameof(PlayerCrimeData.SetPursuitLevel))]
    internal static class NeverWantedPursuitLevelPatch
    {
        private static void Prefix(PlayerCrimeData __instance, ref PlayerCrimeData.EPursuitLevel level)
        {
            if (NeverWantedService.Protects(__instance?.Player)) level = PlayerCrimeData.EPursuitLevel.None;
        }
    }

    [HarmonyPatch]
    internal static class NeverWantedPoliceDetectionPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() => NeverWantedService.Targets(typeof(PoliceOfficer),
            "ShouldNoticeGeneralCrime", "CanInvestigatePlayer");
        private static bool Prefix(Player player, ref bool __result)
        {
            if (!NeverWantedService.Protects(player)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class NeverWantedPoliceStartPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() => NeverWantedService.Targets(typeof(PoliceOfficer),
            nameof(PoliceOfficer.BeginFootPursuit_Networked), nameof(PoliceOfficer.BeginVehiclePursuit_Networked),
            nameof(PoliceOfficer.BeginBodySearch_Networked), "RpcLogic___BeginFootPursuit_3615296227",
            "RpcLogic___BeginBodySearch_3615296227", "RpcLogic___BeginVehiclePursuit_1834136777");
        private static bool Prefix(string playerCode) => !NeverWantedService.ProtectsCode(playerCode);
    }

    [HarmonyPatch(typeof(PoliceOfficer), nameof(PoliceOfficer.ConductBodySearch))]
    internal static class NeverWantedBodySearchPatch
    {
        private static bool Prefix(Player player) => !NeverWantedService.Protects(player);
    }

    [HarmonyPatch]
    internal static class NeverWantedPoliceCombatPatch
    {
        private static IEnumerable<MethodBase> TargetMethods() => NeverWantedService.Targets(typeof(CombatBehaviour),
            nameof(CombatBehaviour.SetTargetAndEnable_Server), nameof(CombatBehaviour.RpcLogic___SetTargetAndEnable_Server_3323014238));
        private static bool Prefix(CombatBehaviour __instance, NetworkObject target) =>
            !NeverWantedService.IsProtectedPoliceTarget(__instance, target);
    }
}
