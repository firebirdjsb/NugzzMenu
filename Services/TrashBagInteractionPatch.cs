using System;
using HarmonyLib;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Trash;
using UnityEngine;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(TrashBag_Equippable), "GetHoveredTrashContainer")]
    internal static class TrashBagHoveredContainerPatch
    {
        private const float InteractionDistance = 2.75f;
        private const float AimAssistRadius = 0.18f;
        private static readonly RaycastHit[] Hits = new RaycastHit[64];

        private static void Postfix(ref TrashContainer __result)
        {
            if (__result == null)
                __result = FindHoveredContainer();
        }

        private static Exception Finalizer(
            ref TrashContainer __result,
            Exception __exception)
        {
            if (__exception != null)
                __result = FindHoveredContainer();

            return null;
        }

        private static TrashContainer FindHoveredContainer()
        {
            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                Camera camera = playerCamera?.Camera ?? Camera.main;
                if (camera == null)
                    return null;

                LayerMask mask = InteractionManager.Instance != null
                    ? InteractionManager.Instance.Interaction_SearchMask
                    : (LayerMask)~0;

                if (playerCamera != null &&
                    playerCamera.LookRaycast(
                        InteractionDistance,
                        out RaycastHit directHit,
                        mask,
                        true,
                        0f))
                {
                    TrashContainer direct = ResolveContainer(directHit.collider);
                    if (direct != null)
                        return direct;
                }

                Ray ray = new Ray(camera.transform.position, camera.transform.forward);
                int hitCount = Physics.SphereCastNonAlloc(
                    ray,
                    AimAssistRadius,
                    Hits,
                    InteractionDistance,
                    mask,
                    QueryTriggerInteraction.Collide);

                TrashContainer nearest = null;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = Hits[i];
                    if (hit.collider == null || hit.distance >= nearestDistance)
                        continue;

                    TrashContainer candidate = ResolveContainer(hit.collider);
                    if (candidate == null)
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

        private static TrashContainer ResolveContainer(Collider collider)
        {
            if (collider == null)
                return null;

            Transform current = collider.transform;
            while (current != null)
            {
                TrashContainer container = current.GetComponent<TrashContainer>();
                if (container != null)
                    return container;

                TrashContainerCollider marker =
                    current.GetComponent<TrashContainerCollider>();
                if (marker?.Container != null)
                    return marker.Container;

                TrashContainerVisuals visuals =
                    current.GetComponent<TrashContainerVisuals>();
                if (visuals?.TrashContainer != null)
                    return visuals.TrashContainer;

                TrashContainerItem item = current.GetComponent<TrashContainerItem>();
                if (item?.Container != null)
                    return item.Container;

                current = current.parent;
            }

            TrashContainer child =
                collider.GetComponentInChildren<TrashContainer>(true);
            if (child != null)
                return child;

            TrashContainerCollider childMarker =
                collider.GetComponentInChildren<TrashContainerCollider>(true);
            if (childMarker?.Container != null)
                return childMarker.Container;

            TrashContainerVisuals childVisuals =
                collider.GetComponentInChildren<TrashContainerVisuals>(true);
            if (childVisuals?.TrashContainer != null)
                return childVisuals.TrashContainer;

            TrashContainerItem childItem =
                collider.GetComponentInChildren<TrashContainerItem>(true);
            return childItem?.Container;
        }
    }
}
