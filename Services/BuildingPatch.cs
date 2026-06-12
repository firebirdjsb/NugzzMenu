using HarmonyLib;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using UnityEngine;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(BuildUpdate_Grid), "CheckIntersections")]
    internal static class PlaceAnywhereGridCheckPatch
    {
        private static void Prefix(BuildUpdate_Grid __instance)
        {
            BuildingService.Instance.PrepareGridPlacement(__instance);
        }

        private static void Postfix(BuildUpdate_Grid __instance)
        {
            BuildingService.Instance.ForceGridValid(__instance);
        }
    }

    [HarmonyPatch(typeof(BuildUpdate_Grid), "Place")]
    internal static class PlaceAnywhereGridCommitPatch
    {
        private static void Postfix(GridItem __result)
        {
            BuildingService.Instance.CommitPreviewGrid(__result);
        }
    }

    [HarmonyPatch(typeof(BuildUpdate_Surface), "IsSurfaceValidForItem")]
    internal static class PlaceAnywhereSurfaceValidationPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (!BuildingService.Instance.PlaceAnywhere)
                return true;

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildUpdate_Surface), "LateUpdate")]
    internal static class PlaceAnywhereSurfaceLateUpdatePatch
    {
        private static void Postfix(BuildUpdate_Surface __instance)
        {
            BuildingService.Instance.ForceSurfaceValid(__instance);
        }
    }

    [HarmonyPatch(typeof(BuildUpdate_ProceduralGrid), "CheckGridIntersections")]
    internal static class PlaceAnywhereProceduralGridPatch
    {
        private static void Postfix(BuildUpdate_ProceduralGrid __instance)
        {
            BuildingService.Instance.ForceProceduralValid(__instance);
        }
    }
}
