using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.Avatar;
using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.Clothing;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.Core.Avatar;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.NPCs.Behaviour;
using Il2CppScheduleOne.NPCs.Schedules;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.Management;
using UnityEngine;
using ScheduleAvatar = Il2CppScheduleOne.AvatarFramework.Avatar;

namespace NugzzMenu.Services
{
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

internal static class ThirdPersonRaycastPatchController
    {
        private const string HarmonyId = "com.xunfairx.nugzzmenu.thirdperson-raycasts";
        private static HarmonyLib.Harmony _harmony;

        internal static void SetInstalled(bool installed)
        {
            if (installed == (_harmony != null))
                return;

            try
            {
                if (!installed)
                {
                    _harmony.UnpatchSelf();
                    _harmony = null;
                    return;
                }

                MethodBase raycast = AccessTools.Method(
                    typeof(PlayerCamera), nameof(PlayerCamera.LookRaycast));
                MethodBase spherecast = AccessTools.Method(
                    typeof(PlayerCamera), nameof(PlayerCamera.LookSpherecast));
                MethodInfo raycastPrefix = AccessTools.Method(
                    typeof(ThirdPersonLookRaycastPatch), "Prefix");
                MethodInfo spherecastPrefix = AccessTools.Method(
                    typeof(ThirdPersonLookSpherecastPatch), "Prefix");
                if (raycast == null || spherecast == null ||
                    raycastPrefix == null || spherecastPrefix == null)
                {
                    return;
                }

                _harmony = new HarmonyLib.Harmony(HarmonyId);
                _harmony.Patch(raycast, prefix: new HarmonyMethod(raycastPrefix));
                _harmony.Patch(spherecast, prefix: new HarmonyMethod(spherecastPrefix));
            }
            catch (System.NotSupportedException ex)
            {
                DebugLogService.Instance.Verbose("PlayerCamera.LookRaycast/LookSpherecast patch skipped (method stripped): " + ex.Message);
                try { _harmony?.UnpatchSelf(); } catch { }
                _harmony = null;
            }
            catch (Exception ex)
            {
                try { _harmony?.UnpatchSelf(); } catch { }
                _harmony = null;
                DebugLogService.Instance.VerboseWarning(
                    "Third-person raycast patch switch failed: " + ex.Message);
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

    [HarmonyPatch]
    internal static class AvatarApplyAccessorySettingsSafetyPatch
    {
        private const string TrashCrownPath = "Avatar/Accessories/Head/TrashCrown/TrashCrown";
        private static long _nextWarningAtMs;

        // This legacy method is absent after the AvatarObject refactor. Resolve it
        // at runtime instead of retaining a hard reference to removed settings APIs.
        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod() =>
            typeof(ScheduleAvatar).GetMethod("ApplyAccessorySettings",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static System.Exception Finalizer(
            object[] __args,
            System.Exception __exception)
        {
            if (__exception == null)
                return null;

            bool hasTrashCrown = false;
            try
            {
                object settings = __args != null && __args.Length > 0 ? __args[0] : null;
                object accessories = S1ApiService.ReadPublicMember(settings, "AccessorySettings");
                if (accessories != null)
                {
                    int count = Convert.ToInt32(S1ApiService.ReadPublicMember(accessories, "Count"));
                    PropertyInfo item = accessories.GetType().GetProperty("Item");
                    for (int i = 0; item != null && i < Math.Min(count, 256); i++)
                    {
                        object accessory = item.GetValue(accessories, new object[] { i });
                        if (string.Equals(S1ApiService.ReadPublicMember(accessory, "path") as string, TrashCrownPath,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            hasTrashCrown = true;
                            break;
                        }
                    }
                }
            }
            catch { }

            return HandleAccessoryFailure(__exception, hasTrashCrown);
        }

        internal static Exception HandleAccessoryFailure(Exception exception, bool hasTrashCrown)
        {
            if (!ShouldSuppressAccessoryException(exception, hasTrashCrown))
                return exception;

            long now = Environment.TickCount64;
            if (now >= _nextWarningAtMs)
            {
                _nextWarningAtMs = now + 30000L;
                // Logging must not turn a contained native failure into a new
                // exception in the caller's clothing update.
                try
                {
                    Debug.LogWarning("[Nugzz] Contained avatar accessory failure" +
                        (hasTrashCrown ? " while applying the trash crown" : string.Empty) +
                        ". Clothing data is unchanged. Error: " + exception);
                }
                catch { }
            }

            return null;
        }

        internal static bool ShouldSuppressAccessoryException(
            Exception exception, bool hasTrashCrown)
        {
            if (exception == null)
                return false;

            string text;
            try { text = exception.ToString(); }
            catch { text = exception.Message ?? string.Empty; }

            // Preserve the existing missing-prefab guard. IL2CPP also wraps native
            // null references, so check their text as well as the managed type.
            bool missingPrefab = text.IndexOf("Object you want to instantiate is null",
                StringComparison.OrdinalIgnoreCase) >= 0;
            if (missingPrefab)
                return true;

            // Only contain the additional null-reference failure for this known
            // accessory. Unexpected errors from other clothing retain vanilla behavior.
            return hasTrashCrown && (exception is NullReferenceException ||
                text.IndexOf("NullReferenceException", StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }

    [HarmonyPatch(typeof(AvatarAppearance), "ApplyAvatarObject")]
    internal static class AvatarObjectApplicationSafetyPatch
    {
        private static void Prefix(SerializedAvatarObject obj, out bool __state)
        {
            __state = false;
            try
            {
                // Compare the registered crown's actual avatar-object ID. New
                // clothing IDs and legacy resource paths need not be identical.
                var definition = ManagerCacheService.Instance.Registry?._GetItem("trashcrown", false)
                    ?.TryCast<ClothingDefinition>();
                var crown = definition?.ClothingAvatarObject;
                __state = crown != null && !string.IsNullOrWhiteSpace(crown.Id) &&
                    string.Equals(obj.Id, crown.Id, StringComparison.OrdinalIgnoreCase);
            }
            catch { }
        }

        private static Exception Finalizer(Exception __exception, bool __state) =>
            AvatarApplyAccessorySettingsSafetyPatch.HandleAccessoryFailure(__exception, __state);
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

    [HarmonyPatch]
    internal static class NPCStayInBuildingEnterAnimationWaitSafetyPatch
    {
        private static bool _reported;

        private static bool Prepare() => TargetMethods().Any();

        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in typeof(NPCEvent_StayInBuilding).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                // Compiler-generated lambda numbers change when vanilla schedules
                // change. Only target this animation's parameterless wait predicates.
                if ((method.Name.StartsWith("_PlayEnterAnimation_b__", StringComparison.Ordinal) ||
                    method.Name.StartsWith("<PlayEnterAnimation>b__", StringComparison.Ordinal)) &&
                    method.ReturnType == typeof(bool) && method.GetParameters().Length == 0)
                    yield return method;
            }
        }

        private static Exception Finalizer(Exception __exception, ref bool __result)
        {
            if (__exception == null)
                return null;

            // A false result makes WaitUntil retry the same broken predicate every frame.
            __result = true;
            if (!_reported)
            {
                _reported = true;
                DebugLogService.Instance.VerboseWarning(
                    "Released an NPC enter-animation wait after its target became unavailable.");
            }
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

    [HarmonyPatch(typeof(MushroomSpawnEquipped), "Update")]
    internal static class MushroomSpawnToolFallbackPatch
    {
        private static bool Prefix(MushroomSpawnEquipped __instance)
        {
            return !GrowToolFallbackService.Instance.RunMushroomSpawnUpdate(__instance);
        }

        private static Exception Finalizer(MushroomSpawnEquipped __instance, Exception __exception)
        {
            return GrowToolFallbackService.Instance.HandleToolUpdateException(__instance, __exception);
        }
    }

    [HarmonyPatch(typeof(Equippable_SprayBottle), "Update")]
    internal static class SprayBottleToolFallbackPatch
    {
        private static bool Prefix(Equippable_SprayBottle __instance)
        {
            return !GrowToolFallbackService.Instance.RunSprayBottleUpdate(__instance);
        }

        private static Exception Finalizer(
            Equippable_SprayBottle __instance,
            Exception __exception)
        {
            return GrowToolFallbackService.Instance.HandleToolUpdateException(
                __instance,
                __exception);
        }
    }

}
