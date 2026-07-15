using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Trash;
using UnityEngine;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(TrashBag_Equippable), "GetHoveredTrashContainer")]
    internal static class TrashBagHoveredContainerPatch
    {
        private const float InteractionDistance = 2.75f;
        private const float AimAssistRadius = 0.08f;
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        private static bool Prefix(ref TrashContainer __result)
        {
            __result = FindHoveredContainer();
            return false;
        }

        private static TrashContainer FindHoveredContainer()
        {
            try
            {
                Camera camera = PlayerCamera.Instance?.Camera ?? Camera.main;
                if (camera == null)
                    return null;

                Ray ray = new Ray(camera.transform.position, camera.transform.forward);
                int hitCount = Physics.SphereCastNonAlloc(
                    ray,
                    AimAssistRadius,
                    Hits,
                    InteractionDistance,
                    ~0,
                    QueryTriggerInteraction.Collide);

                TrashContainer nearest = null;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = Hits[i];
                    if (hit.collider == null || hit.distance >= nearestDistance)
                        continue;

                    TrashContainerCollider marker =
                        hit.collider.GetComponent<TrashContainerCollider>() ??
                        hit.collider.GetComponentInParent<TrashContainerCollider>();
                    TrashContainer candidate = marker?.Container ??
                        hit.collider.GetComponentInParent<TrashContainer>();
                    if (candidate == null || !candidate.CanBeBagged())
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
    }
}
