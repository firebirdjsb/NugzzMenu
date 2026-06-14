using HarmonyLib;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Tiles;
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

    [HarmonyPatch(typeof(BuildUpdate_Grid), "PositionObjectInFrontOfPlayer")]
    internal static class PlaceAnywhereGridPositionPatch
    {
        private static void Postfix(BuildUpdate_Grid __instance)
        {
            BuildingService.Instance.ApplyPreciseGridPosition(__instance);
        }
    }

    [HarmonyPatch(typeof(BuildUpdate_Grid), "LateUpdate")]
    internal static class PlaceAnywhereGridLatePositionPatch
    {
        private static void Postfix(BuildUpdate_Grid __instance)
        {
            BuildingService.Instance.ApplyPreciseGridPosition(__instance);
        }
    }

    [HarmonyPatch(typeof(BuildUpdate_Grid), "Place")]
    internal static class PlaceAnywhereGridCommitPatch
    {
        private static bool Prefix(BuildUpdate_Grid __instance, ref GridItem __result)
        {
            if (!BuildingService.Instance.PlaceAnywhere)
                return true;
            if (BuildingService.Instance.CanCommitGridPlacement(__instance))
                return true;

            __result = null;
            BuildingService.Instance.ReportInvalidGridPlacement();
            return false;
        }

        private static void Postfix(GridItem __result)
        {
            BuildingService.Instance.CommitPreviewGrid(__result);
        }
    }

    [HarmonyPatch(typeof(BuildableItem), nameof(BuildableItem.SetCulled))]
    internal static class PlaceAnywhereItemCullingPatch
    {
        private static void Prefix(BuildableItem __instance, ref bool culled)
        {
            if (culled && BuildingService.Instance.IsSyntheticGridItem(__instance))
                culled = false;
        }
    }

    [HarmonyPatch(typeof(BuildableItem), nameof(BuildableItem.CanBePickedUp))]
    internal static class PlaceAnywhereItemPickupPatch
    {
        private static void Postfix(BuildableItem __instance, ref string reason, ref bool __result)
        {
            if (__result || !BuildingService.Instance.CanReturnOutsideItem(__instance))
                return;

            if (!BuildingService.Instance.HasInventorySpaceFor(__instance))
            {
                reason = "Inventory full";
                return;
            }

            reason = string.Empty;
            __result = true;
        }
    }

    [HarmonyPatch(typeof(BuildManager), nameof(BuildManager.CreateGridItem))]
    internal static class PlaceAnywhereGridItemCreatedPatch
    {
        private static void Postfix(GridItem __result)
        {
            BuildingService.Instance.RegisterOutsideItem(__result);
        }
    }

    [HarmonyPatch(typeof(BuildManager), nameof(BuildManager.CreateSurfaceItem))]
    internal static class PlaceAnywhereSurfaceItemCreatedPatch
    {
        private static void Postfix(SurfaceItem __result)
        {
            BuildingService.Instance.RegisterOutsideItem(__result);
        }
    }

    [HarmonyPatch(typeof(BuildManager), nameof(BuildManager.CreateProceduralGridItem))]
    internal static class PlaceAnywhereProceduralItemCreatedPatch
    {
        private static void Postfix(ProceduralGridItem __result)
        {
            BuildingService.Instance.RegisterOutsideItem(__result);
        }
    }

    [HarmonyPatch(typeof(Grid), "Awake")]
    internal static class PlaceAnywhereGridAwakePatch
    {
        private static bool Prefix(Grid __instance)
        {
            return !BuildingService.Instance.IsSyntheticGrid(__instance);
        }
    }

    [HarmonyPatch(typeof(Tile), "Awake")]
    internal static class PlaceAnywhereTileAwakePatch
    {
        private static bool Prefix(Tile __instance)
        {
            return !BuildingService.Instance.IsSyntheticTile(__instance);
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
