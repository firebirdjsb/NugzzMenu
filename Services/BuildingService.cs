using System;
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
        private bool _placeAnywhere;
        private GameObject _previewRoot;
        private Grid _previewGrid;
        private Vector3 _previewOrigin;

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

                Vector3 origin = ghost.transform.position;
                if (!IsFinite(origin) && buildable.OriginFootprint != null)
                    origin = buildable.OriginFootprint.transform.position;
                if (!IsFinite(origin))
                    origin = ghost.transform.position;
                if (!IsFinite(origin))
                {
                    DebugLogService.Instance.VerboseWarning("Place-anywhere ignored an invalid placement position");
                    DestroyUnusedPreview();
                    return;
                }
                origin.x = Mathf.Round(origin.x * 2f) * 0.5f;
                origin.z = Mathf.Round(origin.z * 2f) * 0.5f;

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

            DebugLogService.Instance.Verbose("Committed place-anywhere grid " + _previewGrid.GUID);
            _previewRoot = null;
            _previewGrid = null;
            _previewOrigin = Vector3.zero;
        }

        public void ForceGridValid(BuildUpdate_Grid buildUpdate)
        {
            if (!_placeAnywhere || buildUpdate == null)
                return;
            try { buildUpdate._validPosition = true; } catch { }
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
            _previewRoot = new GameObject("Nugzz_PlaceAnywhereGrid_" + Guid.NewGuid().ToString("N"));
            _previewRoot.SetActive(false);
            Transform parent = property.Container != null
                ? property.Container.transform
                : property.transform;
            _previewRoot.transform.SetParent(parent, false);
            _previewRoot.transform.position = origin;
            _previewRoot.transform.rotation = Quaternion.identity;

            _previewGrid = _previewRoot.AddComponent<Grid>();
            string gridGuid = Guid.NewGuid().ToString();
            _previewGrid._guid = gridGuid;
            _previewGrid._parentProperty = property;
            _previewGrid.SetGUID(new Il2CppSystem.Guid(gridGuid));
            _previewGrid.Tiles = new Il2CppSystem.Collections.Generic.List<Tile>();
            _previewGrid.CoordinateTilePairs =
                new Il2CppSystem.Collections.Generic.List<CoordinateTilePair>();
            _previewGrid._coordinateToTile =
                new Il2CppSystem.Collections.Generic.Dictionary<Coordinate, Tile>();
            _previewGrid._cosmeticTemperatureEmitters =
                new Il2CppSystem.Collections.Generic.List<TemperatureEmitter>();
            _previewGrid._temperatureEmitters =
                new Il2CppSystem.Collections.Generic.List<TemperatureEmitter>();

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
        }
    }
}
