using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Lighting;
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

        private const int GridRadius = 7;
        private const string GridObjectPrefix = "Nugzz_PlaceAnywhereGrid_";
        private const float InvalidPlacementLogInterval = 1f;
        private const float OutsidePickupRange = 6f;
        private const float OutsidePickupCooldown = 0.25f;
        private const float PlacementFloorRange = 30f;
        private const float PlacementFloorGraceTime = 0.2f;
        private const float MinimumFloorNormalY = 0.45f;
        private bool _placeAnywhere;
        private GameObject _previewRoot;
        private Grid _previewGrid;
        private Vector3 _previewOrigin;
        private float _nextInvalidPlacementLogTime;
        private float _nextOutsidePickupTime;
        private GameObject _positionedGhost;
        private Vector3 _placementFloorPoint;
        private float _lastPlacementFloorTime;
        private int _lastPlacementFloorFrame = -1;
        private bool _hasPlacementFloorPoint;
        private readonly HashSet<string> _outsidePlacedItemGuids =
            new HashSet<string>(StringComparer.Ordinal);

        public bool PlaceAnywhere => _placeAnywhere;

        private BuildingService() { }

        public void SetPlaceAnywhere(bool enabled)
        {
            _placeAnywhere = enabled;
            if (!enabled)
                DestroyUnusedPreview();
            NotificationService.Instance.Notify(enabled ? "Place Anywhere: ON" : "Place Anywhere: OFF");
        }

        public void PrepareGridPlacement(BuildUpdate_Grid buildUpdate)
        {
            if (!_placeAnywhere || buildUpdate == null)
                return;

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
            DebugLogService.Instance.Verbose("Committed place-anywhere grid " + _previewGrid.GUID);
            _previewRoot = null;
            _previewGrid = null;
            _previewOrigin = Vector3.zero;
        }

        public void ApplyPreciseGridPosition(BuildUpdate_Grid buildUpdate)
        {
            if (!_placeAnywhere || buildUpdate == null)
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
                    if (TryGetPlacementFloorPoint(playerCamera, ghost, out Vector3 floorPoint))
                    {
                        _placementFloorPoint = floorPoint;
                        _lastPlacementFloorTime = Time.unscaledTime;
                        _hasPlacementFloorPoint = true;
                    }
                }

                if (!_hasPlacementFloorPoint ||
                    Time.unscaledTime - _lastPlacementFloorTime > PlacementFloorGraceTime)
                {
                    return;
                }

                Transform anchor = buildable.OriginFootprint != null
                    ? buildable.OriginFootprint.transform
                    : buildable.BuildPoint != null
                        ? buildable.BuildPoint
                        : ghost.transform;
                Vector3 offset = _placementFloorPoint - anchor.position;
                if (offset.sqrMagnitude <= 0.000001f)
                    return;

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
            PlayerCamera playerCamera, GameObject ghost, out Vector3 floorPoint)
        {
            floorPoint = default;
            Camera camera = playerCamera.Camera != null ? playerCamera.Camera : Camera.main;
            if (camera == null)
                return false;

            RaycastHit[] hits = Physics.RaycastAll(
                camera.transform.position,
                camera.transform.forward,
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

        public void ForceGridValid(BuildUpdate_Grid buildUpdate)
        {
            if (!_placeAnywhere || buildUpdate == null)
                return;
            try
            {
                buildUpdate._validPosition = HasCompleteTileIntersections(buildUpdate.BuildableItemClass);
            }
            catch { }
        }

        public bool CanCommitGridPlacement(BuildUpdate_Grid buildUpdate)
        {
            if (!_placeAnywhere)
                return true;
            if (buildUpdate == null || buildUpdate.BuildableItemClass == null)
                return false;
            if (_previewGrid == null || _previewRoot == null)
                return false;

            return HasCompleteTileIntersections(buildUpdate.BuildableItemClass);
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
            return gridItem != null && IsSyntheticGrid(gridItem.OwnerGrid);
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
            if (item == null || item.isGhost || item.IsDestroyed || item.ItemInstance == null)
            {
                return false;
            }

            if (IsSyntheticGridItem(item))
                return true;

            string guid = GetItemGuid(item);
            if (!string.IsNullOrEmpty(guid) && _outsidePlacedItemGuids.Contains(guid))
                return true;

            return IsOutsideOwnedPropertyBounds(item);
        }

        public bool HasInventorySpaceFor(BuildableItem item)
        {
            if (item?.ItemInstance == null)
                return false;

            try
            {
                PlayerInventory inventory = ManagerCacheService.Instance.PlayerInventory ??
                    UnityEngine.Object.FindObjectOfType<PlayerInventory>();
                return inventory != null &&
                    inventory.CanItemFitInInventory(item.ItemInstance, 1);
            }
            catch
            {
                return false;
            }
        }

        public void UpdateOutsideItemPickup(bool menuOpen)
        {
            if (menuOpen || Time.unscaledTime < _nextOutsidePickupTime ||
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
            if (Time.unscaledTime < _nextInvalidPlacementLogTime)
                return;

            _nextInvalidPlacementLogTime = Time.unscaledTime + InvalidPlacementLogInterval;
            DebugLogService.Instance.VerboseWarning(
                "Place-anywhere skipped placement because no complete tile intersection was available");
        }

        public void ForceSurfaceValid(BuildUpdate_Surface buildUpdate)
        {
            if (!_placeAnywhere || buildUpdate == null)
                return;
            try { buildUpdate.validPosition = true; } catch { }
        }

        public void ForceProceduralValid(BuildUpdate_ProceduralGrid buildUpdate)
        {
            if (!_placeAnywhere || buildUpdate == null)
                return;
            try { buildUpdate.validPosition = true; } catch { }
        }

        private void EnsurePreviewGrid(Vector3 origin)
        {
            if (_previewGrid != null && _previewRoot != null)
            {
                if ((_previewOrigin - origin).sqrMagnitude > 0.0025f)
                {
                    _previewOrigin = origin;
                    _previewRoot.transform.position = origin;
                }
                return;
            }

            Property property = GetPlacementProperty();
            if (property == null)
            {
                DebugLogService.Instance.VerboseWarning("Place-anywhere needs at least one loaded property");
                return;
            }

            _previewOrigin = origin;
            _previewRoot = new GameObject(GridObjectPrefix + Guid.NewGuid().ToString("N"));
            _previewRoot.SetActive(false);
            Transform parent = property.Container != null
                ? property.Container.transform
                : property.transform;
            _previewRoot.transform.SetParent(parent, false);
            _previewRoot.transform.position = origin;
            _previewRoot.transform.rotation = Quaternion.identity;

            _previewGrid = _previewRoot.AddComponent<Grid>();
            string gridGuid = Guid.NewGuid().ToString();
            _previewGrid._parentProperty = property;
            _previewGrid.Tiles = new Il2CppSystem.Collections.Generic.List<Tile>();
            _previewGrid.CoordinateTilePairs =
                new Il2CppSystem.Collections.Generic.List<CoordinateTilePair>();
            _previewGrid._coordinateToTile =
                new Il2CppSystem.Collections.Generic.Dictionary<Coordinate, Tile>();
            _previewGrid._cosmeticTemperatureEmitters =
                new Il2CppSystem.Collections.Generic.List<TemperatureEmitter>();
            _previewGrid._temperatureEmitters =
                new Il2CppSystem.Collections.Generic.List<TemperatureEmitter>();
            _previewGrid.SetGUID(new Il2CppSystem.Guid(gridGuid));

            try
            {
                if (property.Grids != null && !property.Grids.Contains(_previewGrid))
                    property.Grids.Add(_previewGrid);
            }
            catch { }

            int tileLayer = LayerMask.NameToLayer("Tile");
            if (tileLayer < 0)
                tileLayer = 0;

            for (int x = -GridRadius; x <= GridRadius; x++)
            {
                for (int y = -GridRadius; y <= GridRadius; y++)
                    CreateTile(x, y, tileLayer);
            }

            _previewRoot.SetActive(true);
            Physics.SyncTransforms();
            DebugLogService.Instance.Verbose(
                "Created place-anywhere grid at " + origin + " property=" + property.PropertyCode);
        }

        private void CreateTile(int x, int y, int layer)
        {
            var tileObject = new GameObject("NugzzTile_" + x + "_" + y);
            tileObject.layer = layer;
            tileObject.transform.SetParent(_previewRoot.transform, false);
            tileObject.transform.localPosition = new Vector3(x * 0.5f, 0f, y * 0.5f);
            tileObject.transform.localRotation = Quaternion.identity;

            var collider = tileObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.5f, 0.1f, 0.5f);
            collider.center = Vector3.zero;
            collider.isTrigger = true;

            Tile tile = tileObject.AddComponent<Tile>();
            tile.InitializePropertyTile(x, y, 1000f, _previewGrid);
            tile.BuildableOccupants =
                new Il2CppSystem.Collections.Generic.List<GridItem>();
            tile.OccupantTiles =
                new Il2CppSystem.Collections.Generic.List<FootprintTile>();
            var lightNode = tileObject.AddComponent<LightExposureNode>();
            lightNode.ambientExposure = 1f;
            tile.LightExposureNode = lightNode;
            IgnoreTileForPlayers(collider);

            var coordinate = new Coordinate(x, y);
            var pair = new CoordinateTilePair
            {
                coord = coordinate,
                tile = tile
            };
            _previewGrid.Tiles.Add(tile);
            _previewGrid.CoordinateTilePairs.Add(pair);
            if (!_previewGrid._coordinateToTile.ContainsKey(coordinate))
                _previewGrid._coordinateToTile.Add(coordinate, tile);
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
                if (_previewRoot != null)
                    UnityEngine.Object.Destroy(_previewRoot);
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
