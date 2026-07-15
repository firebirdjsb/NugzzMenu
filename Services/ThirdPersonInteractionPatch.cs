using System;
using HarmonyLib;
using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.NPCs.Schedules;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Management;
using UnityEngine;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.LookRaycast))]
    internal static class ThirdPersonLookRaycastPatch
    {
        private static bool Prefix(float range, ref RaycastHit hit, LayerMask layerMask, bool includeTriggers, float radius, ref bool __result)
        {
            try
            {
                if (!CameraService.Instance.ShouldUseCustomInteractionRaycasts)
                    return true;

                if (!CameraService.Instance.TryThirdPersonInteractionRaycast(range, layerMask, includeTriggers, radius, out RaycastHit patchedHit))
                    return true;
                if (patchedHit.collider == null)
                    return true;

                hit = patchedHit;
                __result = patchedHit.collider != null;
                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] LookRaycast patch error: " + ex.Message);
                return true;
            }
        }

    }

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.LookSpherecast))]
    internal static class ThirdPersonLookSpherecastPatch
    {
        private static bool Prefix(float range, float radius, ref RaycastHit hit, LayerMask layerMask, ref bool __result)
        {
            try
            {
                if (!CameraService.Instance.ShouldUseCustomInteractionRaycasts)
                    return true;

                if (!CameraService.Instance.TryThirdPersonInteractionRaycast(range, layerMask, true, radius, out RaycastHit patchedHit))
                    return true;
                if (patchedHit.collider == null)
                    return true;

                hit = patchedHit;
                __result = patchedHit.collider != null;
                return false;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] LookSpherecast patch error: " + ex.Message);
                return true;
            }
        }

    }

    [HarmonyPatch(typeof(PunchController), "ExecuteHit")]
    internal static class SafePunchHitPatch
    {
        private static bool Prefix(PunchController __instance, float power)
        {
            if (!CameraService.Instance.ShouldUseCustomCombatHit)
                return true;

            return !CameraService.Instance.ExecutePunchSafely(__instance, power);
        }

        private static Exception Finalizer(PunchController __instance, float power, Exception __exception)
        {
            if (__exception == null)
                return null;

            try
            {
                CameraService.Instance.ExecutePunchSafely(__instance, power);
                Debug.LogWarning("[Nugzz] Suppressed vanilla punch hit exception: " + __exception.Message);
                return null;
            }
            catch
            {
                return __exception;
            }
        }
    }

    [HarmonyPatch(typeof(Equippable_MeleeWeapon), "ExecuteHit")]
    internal static class SafeMeleeWeaponHitPatch
    {
        private static bool Prefix(Equippable_MeleeWeapon __instance, float power)
        {
            if (!CameraService.Instance.ShouldUseCustomCombatHit)
                return true;

            return !CameraService.Instance.ExecuteMeleeSafely(__instance, power);
        }

        private static Exception Finalizer(Equippable_MeleeWeapon __instance, float power, Exception __exception)
        {
            if (__exception == null)
                return null;

            try
            {
                CameraService.Instance.ExecuteMeleeSafely(__instance, power);
                Debug.LogWarning("[Nugzz] Suppressed vanilla melee hit exception: " + __exception.Message);
                return null;
            }
            catch
            {
                return __exception;
            }
        }
    }

    [HarmonyPatch(typeof(Avatar), nameof(Avatar.ApplyAccessorySettings))]
    internal static class AvatarApplyAccessorySettingsSafetyPatch
    {
        private static System.Exception Finalizer(System.Exception __exception)
        {
            if (__exception == null)
                return null;

            string text = "";
            try { text = __exception.ToString(); }
            catch { text = __exception.Message ?? ""; }

            if (text.IndexOf("Object you want to instantiate is null", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                text.IndexOf("ApplyAccessorySettings", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.LogWarning("[Nugzz] Suppressed null avatar accessory prefab in ApplyAccessorySettings");
                return null;
            }

            return __exception;
        }
    }

    [HarmonyPatch(typeof(NPCEvent_StayInBuilding), nameof(NPCEvent_StayInBuilding.PlayEnterAnimation))]
    internal static class NPCStayInBuildingEnterAnimationSafetyPatch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception == null)
                return null;

            DebugLogService.Instance.VerboseWarning(
                "Suppressed NPC stay-in-building enter animation error: " + __exception.Message);
            return null;
        }
    }

    [HarmonyPatch(typeof(NPCEvent_StayInBuilding), "RpcLogic___PlayEnterAnimation_2166136261")]
    internal static class NPCStayInBuildingEnterAnimationRpcSafetyPatch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception == null)
                return null;

            DebugLogService.Instance.VerboseWarning(
                "Suppressed NPC stay-in-building enter animation RPC error: " + __exception.Message);
            return null;
        }
    }

    [HarmonyPatch(typeof(NPCEvent_StayInBuilding), "_PlayEnterAnimation_b__19_1")]
    internal static class NPCStayInBuildingEnterAnimationWaitSafetyPatch
    {
        private static Exception Finalizer(Exception __exception, ref bool __result)
        {
            if (__exception == null)
                return null;

            __result = false;
            DebugLogService.Instance.VerboseWarning(
                "Suppressed NPC stay-in-building wait predicate error: " + __exception.Message);
            return null;
        }
    }

    [HarmonyPatch(typeof(CoweringBehaviour), nameof(CoweringBehaviour.SetCowering))]
    internal static class NPCCoweringBehaviourSafetyPatch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception == null)
                return null;

            DebugLogService.Instance.VerboseWarning(
                "Suppressed NPC cowering animation error: " + __exception.Message);
            return null;
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "LateUpdate")]
    internal static class ThirdPersonCameraLateUpdatePatch
    {
        private static void Postfix()
        {
            if (CameraService.Instance.ThirdPersonEnabled)
                CameraService.Instance.ApplyThirdPersonCameraLate();
        }
    }

    [HarmonyPatch(typeof(ManagementWorldspaceCanvas), "Update")]
    internal static class ManagementCanvasVanillaUpdateSafetyPatch
    {
        private static void Prefix(ManagementWorldspaceCanvas __instance)
        {
            ManagementClipboardService.Instance.PrepareCanvasForVanillaUpdate(__instance);
        }

        private static Exception Finalizer(
            ManagementWorldspaceCanvas __instance,
            Exception __exception)
        {
            return ManagementClipboardService.Instance.HandleCanvasUpdateException(
                __instance,
                __exception);
        }
    }

    [HarmonyPatch(typeof(ObjectSelector), "Update")]
    internal static class ManagementObjectSelectorFallbackPatch
    {
        private static bool Prefix(ObjectSelector __instance)
        {
            if (ManagementClipboardService.Instance.RunObjectSelectorUpdate(__instance))
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(NPCSelector), "Update")]
    internal static class ManagementNpcSelectorFallbackPatch
    {
        private static bool Prefix(NPCSelector __instance)
        {
            if (ManagementClipboardService.Instance.RunNpcSelectorUpdate(__instance))
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(TransitEntitySelector), "Update")]
    internal static class ManagementTransitSelectorFallbackPatch
    {
        private static bool Prefix(TransitEntitySelector __instance)
        {
            if (ManagementClipboardService.Instance.RunTransitSelectorUpdate(__instance))
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(Equippable_Trimmers), "Update")]
    internal static class TrimmersToolFallbackPatch
    {
        private static bool Prefix(Equippable_Trimmers __instance)
        {
            return !GrowToolFallbackService.Instance.RunTrimmersUpdate(__instance);
        }

        private static Exception Finalizer(Equippable_Trimmers __instance, Exception __exception)
        {
            return GrowToolFallbackService.Instance.HandleToolUpdateException(__instance, __exception);
        }
    }

    [HarmonyPatch(typeof(Equippable_Pourable), "Update")]
    internal static class PourableToolFallbackPatch
    {
        private static bool Prefix(Equippable_Pourable __instance)
        {
            return !GrowToolFallbackService.Instance.RunPourableUpdate(__instance);
        }

        private static Exception Finalizer(Equippable_Pourable __instance, Exception __exception)
        {
            return GrowToolFallbackService.Instance.HandleToolUpdateException(__instance, __exception);
        }
    }

    [HarmonyPatch(typeof(Equippable_Seed), "Update")]
    internal static class SeedToolFallbackPatch
    {
        private static bool Prefix(Equippable_Seed __instance)
        {
            return !GrowToolFallbackService.Instance.RunSeedUpdate(__instance);
        }

        private static Exception Finalizer(Equippable_Seed __instance, Exception __exception)
        {
            return GrowToolFallbackService.Instance.HandleToolUpdateException(__instance, __exception);
        }
    }

}
