using System;
using HarmonyLib;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Recovers right-click buildable lookup when stale world objects break the vanilla query.
    /// This does not alter placement, validation, snapping, or build distance.
    /// </summary>
    [HarmonyPatch(typeof(InteractionManager), "GetHoveredBuildableItem")]
    internal static class HoveredBuildableItemSafetyPatch
    {
        private const float SafeModeDuration = 10f;
        private static readonly RaycastHit[] Hits = new RaycastHit[48];
        private static float _safeModeUntil;
        private static float _nextLogTime;

        private static bool Prefix(
            InteractionManager __instance,
            ref BuildableItem __result)
        {
            if (Time.unscaledTime >= _safeModeUntil)
                return true;

            __result = FindHoveredBuildableItem(__instance);
            return false;
        }

        private static Exception Finalizer(
            InteractionManager __instance,
            ref BuildableItem __result,
            Exception __exception)
        {
            if (__exception == null)
                return null;

            _safeModeUntil = Time.unscaledTime + SafeModeDuration;
            __result = FindHoveredBuildableItem(__instance);

            if (Time.unscaledTime >= _nextLogTime)
            {
                _nextLogTime = Time.unscaledTime + 30f;
                DebugLogService.Instance.VerboseWarning(
                    "Recovered a stale buildable hover reference; using safe lookup temporarily.");
            }

            return null;
        }

        private static BuildableItem FindHoveredBuildableItem(
            InteractionManager interactionManager)
        {
            if (interactionManager == null)
                return null;

            try
            {
                BuildableItem hovered = ResolveBuildable(
                    interactionManager.HoveredValidInteractableObject);
                hovered ??= ResolveBuildable(interactionManager.HoveredInteractableObject);
                if (IsUsable(hovered))
                    return hovered;

                PlayerCamera playerCamera = PlayerCamera.Instance;
                Camera camera = playerCamera?.Camera != null
                    ? playerCamera.Camera
                    : Camera.main;
                if (camera == null)
                    return null;

                LayerMask searchMask = interactionManager.Interaction_SearchMask;
                if (searchMask.value == 0)
                    searchMask = Physics.DefaultRaycastLayers;

                if (playerCamera != null &&
                    playerCamera.LookRaycast(
                        InteractionManager.MaxInteractionRange,
                        out RaycastHit directHit,
                        searchMask,
                        true,
                        InteractionManager.RayRadius))
                {
                    BuildableItem direct = ResolveBuildable(directHit.collider);
                    if (IsUsable(direct))
                        return direct;
                }

                Ray ray = new Ray(camera.transform.position, camera.transform.forward);
                int hitCount = Physics.SphereCastNonAlloc(
                    ray,
                    InteractionManager.RayRadius,
                    Hits,
                    InteractionManager.MaxInteractionRange,
                    searchMask,
                    QueryTriggerInteraction.Collide);

                BuildableItem nearest = null;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = Hits[i];
                    Collider collider = hit.collider;
                    if (collider == null || hit.distance >= nearestDistance)
                        continue;

                    BuildableItem candidate = ResolveBuildable(collider);
                    if (!IsUsable(candidate))
                        continue;

                    nearest = candidate;
                    nearestDistance = hit.distance;
                }

                return nearest;
            }
            catch
            {
                return null;
            }
        }

        private static BuildableItem ResolveBuildable(Component component)
        {
            if (component == null)
                return null;

            try
            {
                Transform current = component.transform;
                while (current != null)
                {
                    BuildableItem buildable = current.GetComponent<BuildableItem>();
                    if (buildable != null)
                        return buildable;

                    current = current.parent;
                }
            }
            catch { }

            return null;
        }

        private static bool IsUsable(BuildableItem item)
        {
            try
            {
                return item != null && !item.isGhost && !item.IsDestroyed;
            }
            catch
            {
                return false;
            }
        }
    }
}
