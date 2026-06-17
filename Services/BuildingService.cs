using System;
using System.Collections.Generic;
using System.Globalization;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Lighting;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Temperature;
using Il2CppScheduleOne.Tiles;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Creates real tile grids beneath world-space placement previews so grid buildables can use
    /// the native placement and networking path outside normal property bounds.
    /// </summary>
    public sealed class BuildingService
    {
        private static readonly BuildingService _instance = new BuildingService();
        public static BuildingService Instance => _instance;

        private const int GridRadius = 12;
        private const float GridRegionSize = 8f;
        private const float GridHeightStep = 0.05f;
        private const string GridObjectPrefix = "Nugzz_PlaceAnywhereGrid_";
        private const string SyntheticGridGuidPrefix = "4e55475a";
        private const string SyntheticGridGuidVersion = "8501";
        private const float InvalidPlacementLogInterval = 1f;
        private const float OutsidePickupRange = 6f;
        private const float OutsidePickupCooldown = 0.25f;
        private const float PlacementFloorRange = 30f;
        private const float PlacementFloorGraceTime = 0.5f;
        private const float MinimumFloorNormalY = 0.45f;
        private const float PlacementPlaneMaxDistance = 18f;
        private const float PlacementMaxFrameMove = 2.5f;
        private const float SurfaceUpdateLogInterval = 1f;
        private bool _placeAnywhere;
        private GameObject _previewRoot;
        private Grid _previewGrid;
        private Vector3 _previewOrigin;
        private float _nextInvalidPlacementLogTime;
        private float _nextHostOnlyNoticeTime;
        private float _nextOutsidePickupTime;
        private GameObject _positionedGhost;
        private Vector3 _placementFloorPoint;
        private float _lastPlacementFloorTime;
        private int _lastPlacementFloorFrame = -1;
        private bool _hasPlacementFloorPoint;
        private float _placementFloorY;
        private float _nextSurfaceUpdateLogTime;
        private readonly HashSet<string> _outsidePlacedItemGuids =
            new HashSet<string>(StringComparer.Ordinal);

        public bool PlaceAnywhere => _placeAnywhere;
        public bool CanOverridePlacementValidation => _placeAnywhere && HasPlacementAuthority();

        private BuildingService() { }

        public void SetPlaceAnywhere(bool enabled)
        {
            if (enabled && !HasPlacementAuthority())
            {
                _placeAnywhere = false;
                DestroyUnusedPreview();
                ReportHostOnlyPlacement();
                return;
            }

            _placeAnywhere = enabled;
            if (!enabled)
                DestroyUnusedPreview();
            NotificationService.Instance.Notify(enabled ? "Place Anywhere: ON" : "Place Anywhere: OFF");
        }

        public void PrepareGridPlacement(BuildUpdate_Grid buildUpdate)
        {
            if (!_placeAnywhere || buildUpdate == null)
                return;
            if (!HasPlacementAuthority())
            {
                ReportHostOnlyPlacement();
                DestroyUnusedPreview();
                return;
            }

            try
            {
                GridItem buildable = buildUpdate.BuildableItemClass;
                GameObject ghost = buildUpdate.GhostModel;
                if (buildable == null || ghost == null)
                    return;

                Vector3 origin = buildable.OriginFootprint != null
                    ? buildable.OriginFootprint.transform.position
                    : ghost.transform.position;
                if (!IsFinite(origin))
                {
                    ReportInvalidGridPlacement();
                    return;
                }

                EnsurePreviewGrid(origin);
                if (_previewGrid == null)
                    return;

                Physics.SyncTransforms();
                buildable.CalculateFootprintTileIntersections();
                ForceGridValid(buildUpdate);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Place-anywhere grid preparation failed: " + ex.Message);
            }
        }

        public void CommitPreviewGrid(GridItem placedItem)
        {
            if (!_placeAnywhere || _previewGrid == null || placedItem == null)
                return;

            RegisterOutsideItem(placedItem);
            RestorePlacedItemVisibility(placedItem);
            ConfigureGridColliders(_previewGrid, false);
            DebugLogService.Instance.Verbose("Committed place-anywhere grid " + _previewGrid.GUID);
            _previewRoot = null;
            _previewGrid = null;
            _previewOrigin = Vector3.zero;
        }

        public void ApplyPreciseGridPosition(BuildUpdate_Grid buildUpdate)
        {
            if (buildUpdate == null)
                return;

            try
            {
                GridItem buildable = buildUpdate.BuildableItemClass;
                GameObject ghost = buildUpdate.GhostModel;
                PlayerCamera playerCamera = PlayerCamera.Instance;
                if (buildable == null || ghost == null || playerCamera == null)
                    return;

                if (_positionedGhost != ghost)
                {
                    _positionedGhost = ghost;
                    _hasPlacementFloorPoint = false;
                    _lastPlacementFloorFrame = -1;
                }

                if (_lastPlacementFloorFrame != Time.frameCount)
                {
                    _lastPlacementFloorFrame = Time.frameCount;
                    Transform floorAnchor = GetGridPlacementAnchor(buildable, ghost);
                    if (TryGetPlacementFloorPoint(playerCamera, ghost, floorAnchor, out Vector3 floorPoint))
                    {
                        _placementFloorPoint = floorPoint;
                        _placementFloorY = floorPoint.y;
                        _lastPlacementFloorTime = Time.unscaledTime;
                        _hasPlacementFloorPoint = true;
                    }
                }

                if (!_hasPlacementFloorPoint ||
                    Time.unscaledTime - _lastPlacementFloorTime > PlacementFloorGraceTime)
                {
                    return;
                }

                Transform anchor = GetGridPlacementAnchor(buildable, ghost);
                Vector3 offset = _placementFloorPoint - anchor.position;
                if (offset.sqrMagnitude <= 0.000001f)
                    return;
                if (_placeAnywhere &&
                    offset.sqrMagnitude > PlacementMaxFrameMove * PlacementMaxFrameMove)
                {
                    offset = offset.normalized * PlacementMaxFrameMove;
                }

                ghost.transform.position += offset;
                Physics.SyncTransforms();
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Precise place-anywhere positioning failed: " + ex.Message);
            }
        }

        private static bool TryGetPlacementFloorPoint(
            PlayerCamera playerCamera, GameObject ghost, Transform anchor, out Vector3 floorPoint)
        {
            floorPoint = default;
            Camera camera = playerCamera.Camera != null ? playerCamera.Camera : Camera.main;
            if (camera == null || ghost == null)
                return false;

            Vector3 origin = camera.transform.position;
            Vector3 direction = camera.transform.forward;
            if (!IsFinite(origin) || !IsFinite(direction) || direction.sqrMagnitude < 0.0001f)
                return false;

            direction.Normalize();
            if (TryRaycastPlacementFloor(origin, direction, ghost, out floorPoint))
                return true;

            float planeY = anchor != null ? anchor.position.y : ghost.transform.position.y;
            if (!IsFinite(new Vector3(0f, planeY, 0f)) || Mathf.Abs(direction.y) < 0.03f)
                return false;

            float distance = (planeY - origin.y) / direction.y;
            if (distance <= 0.05f || distance > PlacementPlaneMaxDistance)
                return false;

            floorPoint = origin + direction * distance;
            return IsFinite(floorPoint);
        }

        private static bool TryRaycastPlacementFloor(
            Vector3 origin, Vector3 direction, GameObject ghost, out Vector3 floorPoint)
        {
            floorPoint = default;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                direction,
                PlacementFloorRange,
                -1,
                QueryTriggerInteraction.Ignore);
            float nearestDistance = float.MaxValue;
            bool found = false;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                Collider collider = hit.collider;
                if (collider == null || hit.distance >= nearestDistance ||
                    hit.normal.y < MinimumFloorNormalY)
                {
                    continue;
                }

                Transform hitTransform = collider.transform;
                if (hitTransform.IsChildOf(ghost.transform) ||
                    collider.GetComponentInParent<Player>() != null ||
                    collider.GetComponentInParent<BuildableItem>() != null ||
                    collider.GetComponentInParent<Tile>() != null ||
                    collider.GetComponentInParent<FootprintTile>() != null)
                {
                    continue;
                }

                nearestDistance = hit.distance;
                floorPoint = hit.point;
                found = true;
            }

            return found;
        }

        private static Transform GetGridPlacementAnchor(GridItem buildable, GameObject ghost)
        {
            if (buildable?.OriginFootprint != null)
                return buildable.OriginFootprint.transform;
            if (buildable?.BuildPoint != null)
                return buildable.BuildPoint;
            return ghost != null ? ghost.transform : null;
        }

        public void ForceGridValid(BuildUpdate_Grid buildUpdate)
        {
            if (!_placeAnywhere || buildUpdate == null)
                return;
            try
            {
                buildUpdate._validPosition =
                    HasPlacementAuthority() &&
                    HasCompleteTileIntersections(buildUpdate.BuildableItemClass);
            }
            catch { }
        }

        public bool CanCommitGridPlacement(BuildUpdate_Grid buildUpdate)
        {
            if (!_placeAnywhere)
                return true;
            if (!HasPlacementAuthority())
            {
                ReportHostOnlyPlacement();
                DestroyUnusedPreview();
                return false;
            }
            if (buildUpdate == null || buildUpdate.BuildableItemClass == null)
                return false;
            if (_previewGrid == null || _previewRoot == null)
                return false;

            return HasCompleteTileIntersections(buildUpdate.BuildableItemClass);
        }

        public void EnsureSyntheticGridForNetworkItem(GridItem item)
        {
            if (item == null)
                return;

            try
            {
                string gridGuid = item._ownerGridGUID.ToString();
                if (!IsSyntheticGridGuid(gridGuid) || FindGrid(gridGuid) != null)
                    return;

                Vector3 origin;
                if (!TryDecodeSyntheticGridOrigin(gridGuid, out origin))
                {
                    origin = item.OriginFootprint != null
                        ? item.OriginFootprint.transform.position
                        : item.transform.position;
                }

                if (!IsFinite(origin))
                    return;

                CreateSyntheticGrid(origin, gridGuid, false, out _);
                DebugLogService.Instance.Verbose(
                    "Rebuilt network place-anywhere grid " + gridGuid);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Network place-anywhere grid rebuild failed: " + ex.Message);
            }
        }

        public void EnsureSyntheticGridForNetworkGuid(Il2CppSystem.Guid guid)
        {
            try
            {
                string gridGuid = guid.ToString();
                if (!IsSyntheticGridGuid(gridGuid) || FindGrid(gridGuid) != null ||
                    !TryDecodeSyntheticGridOrigin(gridGuid, out Vector3 origin))
                {
                    return;
                }

                CreateSyntheticGrid(origin, gridGuid, false, out _);
                DebugLogService.Instance.Verbose(
                    "Prepared network place-anywhere grid " + gridGuid);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Network place-anywhere grid preparation failed: " + ex.Message);
            }
        }

        public bool IsSyntheticGrid(Grid grid)
        {
            return grid != null && IsSyntheticPlacementTransform(grid.transform);
        }

        public bool IsSyntheticTile(Tile tile)
        {
            return tile != null && IsSyntheticPlacementTransform(tile.transform);
        }

        public bool IsSyntheticGridItem(BuildableItem item)
        {
            GridItem gridItem = item as GridItem;
            if (gridItem == null)
                return false;
            if (IsSyntheticGrid(gridItem.OwnerGrid))
                return true;

            try
            {
                return IsSyntheticGridGuid(gridItem._ownerGridGUID.ToString());
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetHoveredBuildableItem(
            InteractionManager interactionManager, out BuildableItem item)
        {
            item = null;
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

                    GridItem gridItem = candidate as GridItem;
                    if (gridItem != null)
                        EnsureSyntheticGridForNetworkItem(gridItem);

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

        public void RegisterOutsideItem(BuildableItem item)
        {
            if (!_placeAnywhere || item == null || item.isGhost)
                return;

            string guid = GetItemGuid(item);
            if (!string.IsNullOrEmpty(guid))
                _outsidePlacedItemGuids.Add(guid);
        }

        public bool CanReturnOutsideItem(BuildableItem item)
        {
            if (item == null || item.isGhost || item.IsDestroyed)
            {
                return false;
            }

            if (IsSyntheticGridItem(item))
                return true;

            string guid = GetItemGuid(item);
            if (!string.IsNullOrEmpty(guid) && _outsidePlacedItemGuids.Contains(guid))
                return true;

            return IsOutsideOwnedPropertyBounds(item) || IsOutsideAnyPropertyBounds(item);
        }

        public bool HasInventorySpaceFor(BuildableItem item)
        {
            if (!TryEnsureItemInstance(item))
                return false;

            try
            {
                PlayerInventory inventory = null;
                try { inventory = PlayerInventory.Instance; } catch { }
                inventory ??= ManagerCacheService.Instance.PlayerInventory;
                if (inventory == null)
                    return false;

                try
                {
                    if (inventory.CanItemFitInInventory(item.ItemInstance, 1))
                        return true;
                }
                catch { }

                var slots = inventory.GetAllInventorySlots();
                if (slots == null)
                    return false;

                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot == null)
                        continue;

                    try
                    {
                        if (!slot.IsAddLocked &&
                            (slot.GetCapacityForItem(item.ItemInstance, true) > 0 ||
                             slot.GetCapacityForItem(item.ItemInstance, false) > 0))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        try
                        {
                            if (!slot.IsAddLocked && slot.ItemInstance == null)
                                return true;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Pickup inventory capacity check failed: " + ex.Message);
            }

            return false;
        }

        public void UpdateOutsideItemPickup(bool menuOpen)
        {
            if (menuOpen || ManagementClipboardService.Instance.IsActive() ||
                Time.unscaledTime < _nextOutsidePickupTime ||
                !Input.GetMouseButtonDown(1))
            {
                return;
            }

            try
            {
                BuildManager buildManager = BuildManager.Instance;
                if (buildManager != null && buildManager.isBuilding)
                    return;

                BuildableItem item = FindLookedAtOutsideItem();
                if (item == null)
                    return;

                _nextOutsidePickupTime = Time.unscaledTime + OutsidePickupCooldown;
                if (!TryEnsureItemInstance(item))
                {
                    NotificationService.Instance.Notify("Item data is not ready");
                    return;
                }

                if (!HasInventorySpaceFor(item))
                {
                    NotificationService.Instance.Notify("Inventory full");
                    return;
                }

                if (!item.CanBePickedUp(out string reason))
                {
                    NotificationService.Instance.Notify(
                        string.IsNullOrWhiteSpace(reason) ? "Item cannot be picked up" : reason);
                    return;
                }

                string guid = GetItemGuid(item);
                item.PickupItem();
                if (!string.IsNullOrEmpty(guid))
                    _outsidePlacedItemGuids.Remove(guid);
                NotificationService.Instance.Notify("Item returned to inventory");
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException(
                    "Outside item pickup failed", ex);
                NotificationService.Instance.Notify("Could not pick up item");
            }
        }

        public void ReportInvalidGridPlacement()
        {
            ReportInvalidGridPlacement(null);
        }

        private void ReportInvalidGridPlacement(Exception exception)
        {
            if (Time.unscaledTime < _nextInvalidPlacementLogTime)
                return;

            _nextInvalidPlacementLogTime = Time.unscaledTime + InvalidPlacementLogInterval;
            string suffix = exception != null
                ? ": " + exception.GetType().Name + " - " + exception.Message
                : string.Empty;
            DebugLogService.Instance.VerboseWarning(
                "Place-anywhere skipped placement because no complete tile intersection was available" + suffix);
        }

        public void ReportHostOnlyPlacement()
        {
            if (Time.unscaledTime < _nextHostOnlyNoticeTime)
                return;

            _nextHostOnlyNoticeTime = Time.unscaledTime + InvalidPlacementLogInterval;
            NotificationService.Instance.Notify(
                "Place Anywhere in multiplayer must be used by the lobby host");
            DebugLogService.Instance.VerboseWarning(
                "Blocked non-host place-anywhere placement to avoid multiplayer grid desync");
        }

        public void ForceSurfaceValid(BuildUpdate_Surface buildUpdate)
        {
            if (!CanOverridePlacementValidation || buildUpdate == null)
                return;
            try
            {
                buildUpdate.validPosition = CanForceCurrentSurfacePlacement(buildUpdate);
            }
            catch { }
        }

        public bool CanOverrideSurfacePlacement(
            BuildUpdate_Surface buildUpdate, Surface surface, Collider hitCollider)
        {
            if (!CanOverridePlacementValidation || buildUpdate == null ||
                surface == null || hitCollider == null)
            {
                return false;
            }

            try
            {
                return buildUpdate.GhostModel != null &&
                    buildUpdate.BuildableItemClass != null &&
                    buildUpdate.ItemInstance != null;
            }
            catch
            {
                return false;
            }
        }

        public bool ShouldRunSurfaceLateUpdate(BuildUpdate_Surface buildUpdate)
        {
            if (!CanOverridePlacementValidation || buildUpdate == null)
                return true;

            try
            {
                if (buildUpdate.GhostModel == null ||
                    buildUpdate.BuildableItemClass == null ||
                    buildUpdate.ItemInstance == null)
                {
                    buildUpdate.validPosition = false;
                    ReportSkippedSurfaceUpdate();
                    return false;
                }

                if (!TryPrepareSurfaceCandidate(buildUpdate))
                {
                    buildUpdate.validPosition = false;
                    ReportSkippedSurfaceUpdate();
                    return false;
                }

                Camera camera = PlayerCamera.Instance?.Camera != null
                    ? PlayerCamera.Instance.Camera
                    : Camera.main;
                Transform cameraTransform = camera != null ? camera.transform : null;
                Vector3 forward = cameraTransform != null
                    ? cameraTransform.forward
                    : Vector3.zero;
                if (cameraTransform == null || !IsFinite(cameraTransform.position) ||
                    !IsFinite(forward) || forward.sqrMagnitude < 0.0001f)
                {
                    buildUpdate.validPosition = false;
                    ReportSkippedSurfaceUpdate();
                    return false;
                }
            }
            catch
            {
                return true;
            }

            return true;
        }

        public bool HandleSurfaceLateUpdateException(
            BuildUpdate_Surface buildUpdate, Exception exception)
        {
            if (!CanOverridePlacementValidation || buildUpdate == null || exception == null)
                return false;

            try { buildUpdate.validPosition = false; } catch { }
            ReportSkippedSurfaceUpdate(exception);
            return true;
        }

        public bool HandleGridLateUpdateException(
            BuildUpdate_Grid buildUpdate, Exception exception)
        {
            if (!_placeAnywhere || buildUpdate == null || exception == null)
                return false;

            try { buildUpdate._validPosition = false; } catch { }
            ReportInvalidGridPlacement(exception);
            return true;
        }

        public bool HandleGridPlaceException(
            BuildUpdate_Grid buildUpdate, Exception exception)
        {
            if (!_placeAnywhere || buildUpdate == null || exception == null)
                return false;

            try { buildUpdate._validPosition = false; } catch { }
            DestroyUnusedPreview();
            ReportInvalidGridPlacement(exception);
            return true;
        }

        public void HandleBrokenGrowComponent(MonoBehaviour component, Exception exception)
        {
            if (component == null || exception == null)
                return;

            try
            {
                component.enabled = false;
                DebugLogService.Instance.VerboseWarning(
                    "Disabled broken grow component after RV/world-state repair: " +
                    component.GetIl2CppType()?.Name + " - " +
                    exception.GetType().Name + ": " + exception.Message);
            }
            catch { }
        }

        public void ForceProceduralValid(BuildUpdate_ProceduralGrid buildUpdate)
        {
            if (!CanOverridePlacementValidation || buildUpdate == null)
                return;
            try { buildUpdate.validPosition = true; } catch { }
        }

        private static bool CanForceCurrentSurfacePlacement(BuildUpdate_Surface buildUpdate)
        {
            if (buildUpdate == null)
                return false;

            try
            {
                return buildUpdate.GhostModel != null &&
                    buildUpdate.BuildableItemClass != null &&
                    buildUpdate.ItemInstance != null &&
                    buildUpdate.hoveredValidSurface != null;
            }
            catch
            {
                return false;
            }
        }

        private bool TryPrepareSurfaceCandidate(BuildUpdate_Surface buildUpdate)
        {
            try
            {
                Camera camera = PlayerCamera.Instance?.Camera != null
                    ? PlayerCamera.Instance.Camera
                    : Camera.main;
                if (camera == null)
                    return false;

                Transform cameraTransform = camera.transform;
                Vector3 origin = cameraTransform.position;
                Vector3 direction = cameraTransform.forward;
                if (!IsFinite(origin) || !IsFinite(direction) ||
                    direction.sqrMagnitude < 0.0001f)
                {
                    return false;
                }

                direction.Normalize();
                int mask = buildUpdate.DetectionMask.value;
                if (mask == 0)
                    mask = -1;

                RaycastHit[] hits = Physics.RaycastAll(
                    origin,
                    direction,
                    PlacementFloorRange,
                    mask,
                    QueryTriggerInteraction.Ignore);
                float nearestDistance = float.MaxValue;
                Surface nearestSurface = null;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    Collider collider = hit.collider;
                    if (collider == null || hit.distance >= nearestDistance)
                        continue;

                    if (buildUpdate.GhostModel != null &&
                        collider.transform.IsChildOf(buildUpdate.GhostModel.transform))
                    {
                        continue;
                    }

                    Surface surface = collider.GetComponentInParent<Surface>();
                    if (surface == null || !IsSurfaceTypeAllowed(buildUpdate.BuildableItemClass, surface))
                        continue;

                    nearestDistance = hit.distance;
                    nearestSurface = surface;
                }

                if (nearestSurface == null)
                    return false;

                buildUpdate.hoveredValidSurface = nearestSurface;
                return true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Surface placement candidate check failed: " + ex.Message);
                return true;
            }
        }

        private static bool IsSurfaceTypeAllowed(SurfaceItem item, Surface surface)
        {
            if (item == null || surface == null)
                return false;

            try
            {
                if (item.ValidSurfaceTypes == null ||
                    item.ValidSurfaceTypes.Count == 0)
                {
                    return true;
                }

                return item.ValidSurfaceTypes.Contains(surface.SurfaceType);
            }
            catch
            {
                return true;
            }
        }

        private void ReportSkippedSurfaceUpdate()
        {
            ReportSkippedSurfaceUpdate(null);
        }

        private void ReportSkippedSurfaceUpdate(Exception exception)
        {
            if (Time.unscaledTime < _nextSurfaceUpdateLogTime)
                return;

            _nextSurfaceUpdateLogTime = Time.unscaledTime + SurfaceUpdateLogInterval;
            string suffix = exception != null
                ? ": " + exception.GetType().Name + " - " + exception.Message
                : string.Empty;
            DebugLogService.Instance.VerboseWarning(
                "Skipped unsafe surface placement update while Build Anywhere was active" + suffix);
        }

        private void EnsurePreviewGrid(Vector3 origin)
        {
            string gridGuid = CreateSyntheticGridGuid(origin, out Vector3 gridOrigin);
            if (_previewGrid != null && _previewRoot != null &&
                string.Equals(
                    _previewGrid.GUID.ToString(),
                    gridGuid,
                    StringComparison.OrdinalIgnoreCase))
            {
                ConfigureGridColliders(_previewGrid, true);
                return;
            }

            if (_previewGrid != null)
                ConfigureGridColliders(_previewGrid, false);

            _previewOrigin = gridOrigin;
            _previewGrid = FindGrid(gridGuid);
            if (_previewGrid != null)
            {
                _previewRoot = _previewGrid.gameObject;
                ConfigureGridColliders(_previewGrid, true);
                return;
            }

            _previewGrid = CreateSyntheticGrid(
                gridOrigin, gridGuid, true, out _previewRoot);
        }

        private Grid CreateSyntheticGrid(
            Vector3 origin, string gridGuid, bool createColliders, out GameObject root)
        {
            root = null;
            Property property = GetPlacementProperty();
            if (property == null)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Place-anywhere needs at least one loaded property");
                return null;
            }

            root = new GameObject(GridObjectPrefix + gridGuid.Replace("-", string.Empty));
            root.SetActive(false);
            Transform parent = property.Container != null
                ? property.Container.transform
                : property.transform;
            root.transform.SetParent(parent, false);
            root.transform.position = origin;
            root.transform.rotation = Quaternion.identity;

            Grid grid = root.AddComponent<Grid>();
            grid._parentProperty = property;
            grid.Tiles = new Il2CppSystem.Collections.Generic.List<Tile>();
            grid.CoordinateTilePairs =
                new Il2CppSystem.Collections.Generic.List<CoordinateTilePair>();
            grid._coordinateToTile =
                new Il2CppSystem.Collections.Generic.Dictionary<Coordinate, Tile>();
            grid._cosmeticTemperatureEmitters =
                new Il2CppSystem.Collections.Generic.List<TemperatureEmitter>();
            grid._temperatureEmitters =
                new Il2CppSystem.Collections.Generic.List<TemperatureEmitter>();
            grid.SetGUID(new Il2CppSystem.Guid(gridGuid));

            try
            {
                if (property.Grids != null && !property.Grids.Contains(grid))
                    property.Grids.Add(grid);
            }
            catch { }

            int tileLayer = LayerMask.NameToLayer("Tile");
            if (tileLayer < 0)
                tileLayer = 0;

            for (int x = -GridRadius; x <= GridRadius; x++)
            {
                for (int y = -GridRadius; y <= GridRadius; y++)
                    CreateTile(root, grid, x, y, tileLayer, createColliders);
            }

            root.SetActive(true);
            Physics.SyncTransforms();
            DebugLogService.Instance.Verbose(
                "Created place-anywhere grid at " + origin + " property=" + property.PropertyCode);
            return grid;
        }

        private static void CreateTile(
            GameObject root, Grid grid, int x, int y, int layer, bool createCollider)
        {
            var tileObject = new GameObject("NugzzTile_" + x + "_" + y);
            tileObject.layer = layer;
            tileObject.transform.SetParent(root.transform, false);
            tileObject.transform.localPosition = new Vector3(x * 0.5f, 0f, y * 0.5f);
            tileObject.transform.localRotation = Quaternion.identity;

            if (createCollider)
            {
                var collider = tileObject.AddComponent<BoxCollider>();
                collider.size = new Vector3(0.5f, 0.1f, 0.5f);
                collider.center = Vector3.zero;
                collider.isTrigger = true;
                IgnoreTileForPlayers(collider);
            }

            Tile tile = tileObject.AddComponent<Tile>();
            tile.InitializePropertyTile(x, y, 1000f, grid);
            tile.BuildableOccupants =
                new Il2CppSystem.Collections.Generic.List<GridItem>();
            tile.OccupantTiles =
                new Il2CppSystem.Collections.Generic.List<FootprintTile>();
            var lightNode = tileObject.AddComponent<LightExposureNode>();
            lightNode.ambientExposure = 1f;
            tile.LightExposureNode = lightNode;

            var coordinate = new Coordinate(x, y);
            var pair = new CoordinateTilePair
            {
                coord = coordinate,
                tile = tile
            };
            grid.Tiles.Add(tile);
            grid.CoordinateTilePairs.Add(pair);
            if (!grid._coordinateToTile.ContainsKey(coordinate))
                grid._coordinateToTile.Add(coordinate, tile);
        }

        private static void IgnoreTileForPlayers(Collider tileCollider)
        {
            if (tileCollider == null)
                return;
            try
            {
                var players = Player.PlayerList;
                if (players == null)
                    return;
                for (int i = 0; i < players.Count; i++)
                {
                    var player = players[i];
                    if (player == null)
                        continue;
                    if (player.CapCol != null)
                        Physics.IgnoreCollision(player.CapCol, tileCollider, true);
                    if (player.CharacterController != null &&
                        player.CharacterController != player.CapCol)
                        Physics.IgnoreCollision(player.CharacterController, tileCollider, true);
                }
            }
            catch { }
        }

        private static bool HasCompleteTileIntersections(GridItem buildable)
        {
            if (buildable == null || buildable.CoordinateFootprintTilePairs == null ||
                buildable.CoordinateFootprintTilePairs.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < buildable.CoordinateFootprintTilePairs.Count; i++)
            {
                var footprint = buildable.CoordinateFootprintTilePairs[i]?.footprintTile;
                var detector = footprint?.tileDetector;
                if (detector?.intersectedTiles == null || detector.intersectedTiles.Count == 0)
                    return false;
            }

            return true;
        }

        private static string CreateSyntheticGridGuid(
            Vector3 origin, out Vector3 gridOrigin)
        {
            int regionX = Mathf.RoundToInt(origin.x / GridRegionSize);
            int regionZ = Mathf.RoundToInt(origin.z / GridRegionSize);
            int height = Mathf.RoundToInt(origin.y / GridHeightStep);
            gridOrigin = new Vector3(
                regionX * GridRegionSize,
                height * GridHeightStep,
                regionZ * GridRegionSize);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1:x4}-{2:x4}-{3:x4}-{4}00000000",
                SyntheticGridGuidPrefix,
                unchecked((ushort)(short)regionX),
                unchecked((ushort)(short)regionZ),
                unchecked((ushort)(short)height),
                SyntheticGridGuidVersion);
        }

        private static bool IsSyntheticGridGuid(string guid)
        {
            return !string.IsNullOrEmpty(guid) &&
                guid.StartsWith(SyntheticGridGuidPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryDecodeSyntheticGridOrigin(
            string guid, out Vector3 origin)
        {
            origin = default;
            string[] parts = guid?.Split('-');
            if (parts == null || parts.Length != 5 ||
                !string.Equals(
                    parts[0], SyntheticGridGuidPrefix,
                    StringComparison.OrdinalIgnoreCase) ||
                !parts[4].StartsWith(
                    SyntheticGridGuidVersion, StringComparison.OrdinalIgnoreCase) ||
                !ushort.TryParse(
                    parts[1], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out ushort encodedX) ||
                !ushort.TryParse(
                    parts[2], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out ushort encodedZ) ||
                !ushort.TryParse(
                    parts[3], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out ushort encodedHeight))
            {
                return false;
            }

            int regionX = unchecked((short)encodedX);
            int regionZ = unchecked((short)encodedZ);
            int height = unchecked((short)encodedHeight);
            origin = new Vector3(
                regionX * GridRegionSize,
                height * GridHeightStep,
                regionZ * GridRegionSize);
            return true;
        }

        private static Grid FindGrid(string guid)
        {
            var grids = Resources.FindObjectsOfTypeAll<Grid>();
            for (int i = 0; i < grids.Length; i++)
            {
                Grid grid = grids[i];
                if (grid != null &&
                    string.Equals(grid.GUID.ToString(), guid, StringComparison.OrdinalIgnoreCase))
                {
                    return grid;
                }
            }

            return null;
        }

        private static void ConfigureGridColliders(Grid grid, bool enabled)
        {
            if (grid == null || grid.Tiles == null)
                return;

            for (int i = 0; i < grid.Tiles.Count; i++)
            {
                Tile tile = grid.Tiles[i];
                Collider collider = tile != null ? tile.GetComponent<Collider>() : null;
                if (enabled && collider == null && tile != null)
                {
                    var boxCollider = tile.gameObject.AddComponent<BoxCollider>();
                    boxCollider.size = new Vector3(0.5f, 0.1f, 0.5f);
                    boxCollider.center = Vector3.zero;
                    boxCollider.isTrigger = true;
                    IgnoreTileForPlayers(boxCollider);
                    collider = boxCollider;
                }

                if (collider != null)
                    collider.enabled = enabled;
            }
        }

        public bool TryEnsureItemInstance(BuildableItem item)
        {
            if (item == null)
                return false;

            try
            {
                if (item.ItemInstance != null)
                    return true;
            }
            catch { }

            ItemInstance instance = TryLoadItemInstanceFromSaveData(item) ??
                TryCreateItemInstanceFromRegistry(item);
            if (instance == null)
                return false;

            try
            {
                item.ItemInstance = instance;
                DebugLogService.Instance.Verbose(
                    "Restored missing buildable item instance for pickup");
                return true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Failed to restore buildable item instance: " + ex.Message);
                return false;
            }
        }

        private static string GetItemGuid(BuildableItem item)
        {
            try
            {
                return item?.GUID.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool IsOutsideOwnedPropertyBounds(BuildableItem item)
        {
            Property parentProperty = item?.ParentProperty;
            if (parentProperty == null || Property.OwnedProperties == null ||
                !Property.OwnedProperties.Contains(parentProperty))
            {
                return false;
            }

            GameObject boundsRoot = parentProperty.BoundingBox;
            if (boundsRoot == null)
                return false;

            BoxCollider[] bounds = boundsRoot.GetComponentsInChildren<BoxCollider>(true);
            if (bounds == null || bounds.Length == 0)
                return false;

            Vector3 position = item.transform.position;
            for (int i = 0; i < bounds.Length; i++)
            {
                BoxCollider box = bounds[i];
                if (box != null && IsPointInsideBox(position, box))
                    return false;
            }

            return true;
        }

        private static bool IsOutsideAnyPropertyBounds(BuildableItem item)
        {
            if (item == null || Property.Properties == null)
                return false;

            bool foundBounds = false;
            Vector3 position = item.transform.position;
            for (int propertyIndex = 0; propertyIndex < Property.Properties.Count; propertyIndex++)
            {
                Property property = Property.Properties[propertyIndex];
                GameObject boundsRoot = property != null ? property.BoundingBox : null;
                if (boundsRoot == null)
                    continue;

                BoxCollider[] bounds = boundsRoot.GetComponentsInChildren<BoxCollider>(true);
                if (bounds == null || bounds.Length == 0)
                    continue;

                foundBounds = true;
                for (int boundsIndex = 0; boundsIndex < bounds.Length; boundsIndex++)
                {
                    BoxCollider box = bounds[boundsIndex];
                    if (box != null && IsPointInsideBox(position, box))
                        return false;
                }
            }

            return foundBounds;
        }

        private static ItemInstance TryLoadItemInstanceFromSaveData(BuildableItem item)
        {
            try
            {
                BuildableItemData data = item.GetBaseData();
                if (data == null || string.IsNullOrWhiteSpace(data.ItemString))
                    return null;

                return ItemDeserializer.LoadItem(data.ItemString);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Buildable item save-data restore failed: " + ex.Message);
                return null;
            }
        }

        private static ItemInstance TryCreateItemInstanceFromRegistry(BuildableItem item)
        {
            try
            {
                BuildableItemDefinition definition = FindBuildableDefinition(item);
                return definition != null ? definition.GetDefaultInstance(1) : null;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Buildable item registry restore failed: " + ex.Message);
                return null;
            }
        }

        private static BuildableItemDefinition FindBuildableDefinition(BuildableItem item)
        {
            if (item == null)
                return null;

            Registry registry = Registry.Instance;
            var definitions = registry != null ? registry.GetAllItems() : null;
            if (definitions == null)
                return null;

            string itemName = NormalizeBuildableName(item.name);
            Type itemType = item.GetType();

            for (int i = 0; i < definitions.Count; i++)
            {
                BuildableItemDefinition buildableDefinition =
                    TryCastItemDefinition<BuildableItemDefinition>(definitions[i]);
                BuildableItem builtItem = buildableDefinition?.BuiltItem;
                if (builtItem == null)
                    continue;

                if (builtItem == item)
                    return buildableDefinition;

                if (builtItem.GetType() == itemType &&
                    string.Equals(
                        NormalizeBuildableName(builtItem.name),
                        itemName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return buildableDefinition;
                }
            }

            return null;
        }

        private static T TryCastItemDefinition<T>(ItemDefinition definition)
            where T : ItemDefinition
        {
            if (definition == null)
                return null;

            try
            {
                return definition.TryCast<T>();
            }
            catch
            {
                return definition as T;
            }
        }

        private static string NormalizeBuildableName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            return name
                .Replace("(Clone)", string.Empty)
                .Replace("Ghost", string.Empty)
                .Trim();
        }

        private static bool HasPlacementAuthority()
        {
            try
            {
                return !LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost();
            }
            catch
            {
                return true;
            }
        }

        private static bool IsPointInsideBox(Vector3 worldPoint, BoxCollider box)
        {
            Vector3 localPoint = box.transform.InverseTransformPoint(worldPoint) - box.center;
            Vector3 halfSize = box.size * 0.5f;
            return Mathf.Abs(localPoint.x) <= halfSize.x &&
                Mathf.Abs(localPoint.y) <= halfSize.y &&
                Mathf.Abs(localPoint.z) <= halfSize.z;
        }

        private BuildableItem FindLookedAtOutsideItem()
        {
            PlayerCamera playerCamera = PlayerCamera.Instance;
            Camera camera = playerCamera?.Camera != null ? playerCamera.Camera : Camera.main;
            if (camera == null)
                return null;

            Ray ray = new Ray(camera.transform.position, camera.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(
                ray, OutsidePickupRange, -1, QueryTriggerInteraction.Collide);
            BuildableItem nearestItem = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider == null || hit.distance >= nearestDistance)
                    continue;

                BuildableItem item = hit.collider.GetComponentInParent<BuildableItem>();
                if (!CanReturnOutsideItem(item))
                    continue;

                nearestDistance = hit.distance;
                nearestItem = item;
            }

            return nearestItem;
        }

        private static void RestorePlacedItemVisibility(GridItem placedItem)
        {
            if (placedItem == null)
                return;

            try
            {
                placedItem.gameObject.SetActive(true);
                placedItem.SetCulled(false);

                if (placedItem.GameObjectsToCull != null)
                {
                    for (int i = 0; i < placedItem.GameObjectsToCull.Length; i++)
                    {
                        GameObject target = placedItem.GameObjectsToCull[i];
                        if (target != null)
                            target.SetActive(true);
                    }
                }

                if (placedItem.MeshesToCull != null)
                {
                    for (int i = 0; i < placedItem.MeshesToCull.Count; i++)
                    {
                        MeshRenderer renderer = placedItem.MeshesToCull[i];
                        if (renderer != null)
                            renderer.enabled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Placed item visibility restore failed: " + ex.Message);
            }
        }

        private static bool IsSyntheticPlacementTransform(Transform transform)
        {
            while (transform != null)
            {
                if (!string.IsNullOrEmpty(transform.name) &&
                    transform.name.StartsWith(GridObjectPrefix, StringComparison.Ordinal))
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private static Property GetPlacementProperty()
        {
            try
            {
                if (Property.OwnedProperties != null && Property.OwnedProperties.Count > 0)
                    return Property.OwnedProperties[0];
                if (Property.Properties != null && Property.Properties.Count > 0)
                    return Property.Properties[0];
            }
            catch { }
            return UnityEngine.Object.FindObjectOfType<Property>();
        }

        private void DestroyUnusedPreview()
        {
            try
            {
                if (_previewGrid != null)
                    ConfigureGridColliders(_previewGrid, false);
            }
            catch { }
            _previewRoot = null;
            _previewGrid = null;
            _previewOrigin = Vector3.zero;
            _positionedGhost = null;
            _hasPlacementFloorPoint = false;
            _lastPlacementFloorFrame = -1;
        }
    }
}
