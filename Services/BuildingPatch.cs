using System;
using HarmonyLib;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(GrowContainerMoistureDisplay), "LateUpdate")]
    internal static class GrowContainerMoistureDisplaySafetyPatch
    {
        private static Exception Finalizer(
            GrowContainerMoistureDisplay __instance, Exception __exception)
        {
            if (__exception == null)
                return null;

            BuildingService.Instance.HandleBrokenGrowComponent(__instance, __exception);
            return null;
        }
    }

    [HarmonyPatch(typeof(GrowContainerInteraction), "LateUpdate")]
    internal static class GrowContainerInteractionSafetyPatch
    {
        private static Exception Finalizer(
            GrowContainerInteraction __instance, Exception __exception)
        {
            if (__exception == null)
                return null;

            BuildingService.Instance.HandleBrokenGrowComponent(__instance, __exception);
            return null;
        }
    }

    [HarmonyPatch(typeof(InteractionManager), "GetHoveredBuildableItem")]
    internal static class HoveredBuildableItemSafetyPatch
    {
        private static Exception Finalizer(
            InteractionManager __instance,
            ref BuildableItem __result,
            Exception __exception)
        {
            if (__exception == null)
                return null;

            TryGetHoveredBuildableItem(__instance, out __result);
            return null;
        }

        private static bool TryGetHoveredBuildableItem(
            InteractionManager interactionManager,
            out BuildableItem item)
        {
            item = null;
            if (interactionManager == null)
                return false;

            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                Camera camera = playerCamera?.Camera != null
                    ? playerCamera.Camera
                    : Camera.main;
                if (camera == null)
                    return false;

                Ray ray = new Ray(camera.transform.position, camera.transform.forward);
                RaycastHit[] hits = Physics.SphereCastAll(
                    ray,
                    InteractionManager.RayRadius,
                    InteractionManager.MaxInteractionRange,
                    interactionManager.Interaction_SearchMask,
                    QueryTriggerInteraction.Collide);

                float nearestDistance = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider == null || hit.distance >= nearestDistance)
                        continue;

                    BuildableItem candidate =
                        hit.collider.GetComponentInParent<BuildableItem>();
                    if (candidate == null || candidate.isGhost || candidate.IsDestroyed)
                        continue;

                    nearestDistance = hit.distance;
                    item = candidate;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Safe buildable hover lookup failed: " + ex.Message);
                item = null;
            }

            return item != null;
        }
    }
}
