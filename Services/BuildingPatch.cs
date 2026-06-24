using System;
using HarmonyLib;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.Interaction;
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
        private static Exception Finalizer(BuildUpdate_Grid __instance, Exception __exception)
        {
            if (__exception != null &&
                BuildingService.Instance.HandleGridLateUpdateException(__instance, __exception))
            {
                return null;
            }

            return __exception;
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

        private static Exception Finalizer(
            BuildUpdate_Grid __instance, ref GridItem __result, Exception __exception)
        {
            if (__exception != null &&
                BuildingService.Instance.HandleGridPlaceException(__instance, __exception))
            {
                __result = null;
                return null;
            }

            return __exception;
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

    [HarmonyPatch(typeof(GridItem), "ProcessGridData")]
    internal static class PlaceAnywhereGridDataPatch
    {
        private static void Prefix(GridItem __instance)
        {
            BuildingService.Instance.EnsureSyntheticGridForNetworkItem(__instance);
        }
    }

    [HarmonyPatch(typeof(GridItem), "SetGridData")]
    internal static class PlaceAnywhereSetGridDataPatch
    {
        private static void Prefix(Il2CppSystem.Guid gridGUID)
        {
            BuildingService.Instance.EnsureSyntheticGridForNetworkGuid(gridGUID);
        }
    }

    [HarmonyPatch(typeof(BuildableItem), nameof(BuildableItem.CanBePickedUp))]
    internal static class PlaceAnywhereItemPickupPatch
    {
        private static bool Prefix(BuildableItem __instance, ref string reason, ref bool __result)
        {
            if (!BuildingService.Instance.CanReturnOutsideItem(__instance))
                return true;

            if (!BuildingService.Instance.TryEnsureItemInstance(__instance))
            {
                reason = "Item data is missing";
                __result = false;
                return false;
            }

            return true;
        }

        private static void Postfix(BuildableItem __instance, ref string reason, ref bool __result)
        {
            if (__result || !BuildingService.Instance.CanReturnOutsideItem(__instance))
                return;

            if (!BuildingService.Instance.TryEnsureItemInstance(__instance))
            {
                reason = "Item data is missing";
                return;
            }

            if (!BuildingService.Instance.HasInventorySpaceFor(__instance))
            {
                reason = "Inventory full";
                return;
            }

            reason = string.Empty;
            __result = true;
        }
    }

    [HarmonyPatch(typeof(InteractionManager), "GetHoveredBuildableItem")]
    internal static class PlaceAnywhereHoveredBuildableSafetyPatch
    {
        private static Exception Finalizer(
            InteractionManager __instance, ref BuildableItem __result, Exception __exception)
        {
            if (__exception == null)
                return null;

            BuildingService.Instance.TryGetHoveredBuildableItem(
                __instance, out __result);
            return null;
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
        private static bool Prefix(
            BuildUpdate_Surface __instance,
            Surface surface,
            Collider hitCollider,
            ref bool __result)
        {
            if (!BuildingService.Instance.CanOverridePlacementValidation)
                return true;

            if (!BuildingService.Instance.CanOverrideSurfacePlacement(
                    __instance, surface, hitCollider))
            {
                __result = false;
                return false;
            }

            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(BuildUpdate_Surface), "LateUpdate")]
    internal static class PlaceAnywhereSurfaceLateUpdatePatch
    {
        private static bool Prefix(BuildUpdate_Surface __instance)
        {
            return BuildingService.Instance.ShouldRunSurfaceLateUpdate(__instance);
        }

        private static void Postfix(BuildUpdate_Surface __instance)
        {
            BuildingService.Instance.ForceSurfaceValid(__instance);
        }

        private static Exception Finalizer(BuildUpdate_Surface __instance, Exception __exception)
        {
            if (__exception != null &&
                BuildingService.Instance.HandleSurfaceLateUpdateException(__instance, __exception))
            {
                return null;
            }

            return __exception;
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
}
