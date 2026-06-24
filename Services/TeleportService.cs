using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Property;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class TeleportService
    {
        private const int DefaultPageSize = 12;
        private const float NearDuplicateDistance = 4f;

        private static readonly TeleportService _instance = new TeleportService();
        public static TeleportService Instance => _instance;

        private readonly List<TeleportDestination> _destinations = new List<TeleportDestination>();
        private readonly HashSet<string> _destinationKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Vector3 _savedPosition = Vector3.zero;
        private bool _hasSavedPosition;
        private bool _catalogDirty = true;
        private float _nextAutoRefreshTime;
        private string _status = "Teleport catalog not loaded.";

        public struct TeleportDestination
        {
            public string Category;
            public string Label;
            public Vector3 Position;
        }

        private TeleportService() { }

        public string StatusMessage => _status;

        public int DestinationCount
        {
            get
            {
                EnsureCatalog();
                return _destinations.Count;
            }
        }

        public void MarkCatalogDirty()
        {
            _catalogDirty = true;
        }

        public void SavePosition()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player != null)
                {
                    _savedPosition = player.transform.position;
                    _hasSavedPosition = true;
                    NotificationService.Instance.Notify($"Position saved: {_savedPosition.x:F1}, {_savedPosition.y:F1}, {_savedPosition.z:F1}");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Save position failed: {ex.Message}");
            }
        }

        public void LoadPosition()
        {
            try
            {
                if (!_hasSavedPosition)
                {
                    NotificationService.Instance.Notify("No position saved");
                    return;
                }

                TeleportLocalPlayer(_savedPosition, "saved position");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Load position failed: {ex.Message}");
            }
        }

        public void RefreshCatalog()
        {
            _catalogDirty = false;
            _destinationKeys.Clear();
            _destinations.Clear();

            AddStaticFallbacks();
            AddProperties();
            AddBusinesses();
            AddDeliveryLocations();
            AddSupplierLocations();
            AddDeadDrops();
            AddPoliceStations();
            AddParkingLots();
            AddPOIs();
            AddNpcs();

            SortDestinations();
            _status = "Loaded teleport destinations: " + _destinations.Count;
        }

        public int GetPageCount(int pageSize = DefaultPageSize)
        {
            EnsureCatalog();
            pageSize = Mathf.Max(1, pageSize);
            if (_destinations.Count == 0)
                return 1;

            return (_destinations.Count + pageSize - 1) / pageSize;
        }

        public int GetPageItemCount(int pageIndex, int pageSize = DefaultPageSize)
        {
            EnsureCatalog();
            pageSize = Mathf.Max(1, pageSize);
            int start = Mathf.Max(0, pageIndex) * pageSize;
            if (start >= _destinations.Count)
                return 0;

            return Mathf.Min(pageSize, _destinations.Count - start);
        }

        public string GetDestinationButtonLabel(int index)
        {
            EnsureCatalog();
            if (index < 0 || index >= _destinations.Count)
                return "";

            TeleportDestination destination = _destinations[index];
            return destination.Category + ": " + destination.Label;
        }

        public string GetDestinationSummary(int index)
        {
            EnsureCatalog();
            if (index < 0 || index >= _destinations.Count)
                return "No destination selected.";

            TeleportDestination destination = _destinations[index];
            Vector3 pos = destination.Position;
            return destination.Category + " / " + destination.Label +
                $"  ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})";
        }

        public void TeleportToDestination(int index)
        {
            EnsureCatalog();
            if (index < 0 || index >= _destinations.Count)
            {
                NotificationService.Instance.Warning("No teleport destination selected");
                return;
            }

            TeleportDestination destination = _destinations[index];
            TeleportLocalPlayer(destination.Position, destination.Category + ": " + destination.Label);
        }

        private void EnsureCatalog()
        {
            if (_catalogDirty || Time.unscaledTime >= _nextAutoRefreshTime)
            {
                RefreshCatalog();
                _nextAutoRefreshTime = Time.unscaledTime + 10f;
            }
        }

        private void TeleportLocalPlayer(Vector3 position, string label)
        {
            try
            {
                Player player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null)
                {
                    NotificationService.Instance.Warning("No local player found");
                    return;
                }

                CharacterController controller = null;
                try { controller = player.GetComponent<CharacterController>(); } catch { }

                try { if (controller != null) controller.enabled = false; } catch { }
                player.transform.position = position + Vector3.up * 0.35f;
                try { if (controller != null) controller.enabled = true; } catch { }

                _status = "Teleported to " + label;
                NotificationService.Instance.Status(_status);
            }
            catch (Exception ex)
            {
                _status = "Teleport failed: " + ex.Message;
                NotificationService.Instance.Error(_status);
            }
        }

        private void AddStaticFallbacks()
        {
            Add("Location", "Current Room Origin", DebugTestRoomService.Instance.IsLoaded
                ? FindObjectPosition("Nugzz_DebugTestRoom", Vector3.zero)
                : Vector3.zero);
        }

        private void AddProperties()
        {
            try
            {
                var properties = Property.Properties;
                if (properties == null)
                    return;

                for (int i = 0; i < properties.Count; i++)
                {
                    Property property = null;
                    try { property = properties[i]; } catch { }
                    if (property == null)
                        continue;

                    string label = SafeString(property.PropertyName, property.PropertyCode, property.name);
                    Add("Property", label, GetBestPropertyPoint(property));
                }
            }
            catch { }
        }

        private void AddBusinesses()
        {
            try
            {
                var businesses = Business.Businesses;
                if (businesses == null)
                    return;

                for (int i = 0; i < businesses.Count; i++)
                {
                    Business business = null;
                    try { business = businesses[i]; } catch { }
                    if (business == null)
                        continue;

                    string label = SafeString(business.PropertyName, business.PropertyCode, business.name);
                    Add("Business", label, GetBestPropertyPoint(business));
                }
            }
            catch { }
        }

        private void AddPOIs()
        {
            try
            {
                POI[] pois = UnityEngine.Object.FindObjectsOfType<POI>(true);
                if (pois == null)
                    return;

                for (int i = 0; i < pois.Length; i++)
                {
                    POI poi = pois[i];
                    if (poi == null || poi.transform == null)
                        continue;

                    string label = SafeString(poi.MainText, poi.DefaultMainText, poi.name);
                    Add("POI", label, poi.transform.position);
                }
            }
            catch { }
        }

        private void AddDeliveryLocations()
        {
            try
            {
                DeliveryLocation[] locations = UnityEngine.Object.FindObjectsOfType<DeliveryLocation>(true);
                if (locations == null)
                    return;

                for (int i = 0; i < locations.Length; i++)
                {
                    DeliveryLocation location = locations[i];
                    if (location == null)
                        continue;

                    Transform point = location.TeleportPoint != null
                        ? location.TeleportPoint
                        : location.CustomerStandPoint != null
                            ? location.CustomerStandPoint
                            : location.transform;
                    Add("Delivery", SafeString(location.LocationName, location.name), point.position);
                }
            }
            catch { }
        }

        private void AddSupplierLocations()
        {
            try
            {
                SupplierLocation[] locations = UnityEngine.Object.FindObjectsOfType<SupplierLocation>(true);
                if (locations == null)
                    return;

                for (int i = 0; i < locations.Length; i++)
                {
                    SupplierLocation location = locations[i];
                    if (location == null)
                        continue;

                    Transform point = location.SupplierStandPoint != null
                        ? location.SupplierStandPoint
                        : location.transform;
                    Add("Supplier", SafeString(location.LocationName, location.name), point.position);
                }
            }
            catch { }
        }

        private void AddDeadDrops()
        {
            try
            {
                DeadDrop[] drops = UnityEngine.Object.FindObjectsOfType<DeadDrop>(true);
                if (drops == null)
                    return;

                for (int i = 0; i < drops.Length; i++)
                {
                    DeadDrop drop = drops[i];
                    if (drop == null || drop.transform == null)
                        continue;

                    Add("Dead Drop", SafeString(drop.DeadDropName, drop.name), drop.transform.position);
                }
            }
            catch { }
        }

        private void AddPoliceStations()
        {
            try
            {
                PoliceStation[] stations = UnityEngine.Object.FindObjectsOfType<PoliceStation>(true);
                if (stations == null)
                    return;

                for (int i = 0; i < stations.Length; i++)
                {
                    PoliceStation station = stations[i];
                    if (station == null)
                        continue;

                    Transform point = station.SpawnPoint != null ? station.SpawnPoint : station.transform;
                    Add("Police", SafeString(station.name, "Police Station"), point.position);
                }
            }
            catch { }
        }

        private void AddParkingLots()
        {
            try
            {
                ParkingLot[] lots = UnityEngine.Object.FindObjectsOfType<ParkingLot>(true);
                if (lots == null)
                    return;

                for (int i = 0; i < lots.Length; i++)
                {
                    ParkingLot lot = lots[i];
                    if (lot == null)
                        continue;

                    Transform point = lot.EntryPoint != null
                        ? lot.EntryPoint
                        : lot.HiddenVehicleAccessPoint != null
                            ? lot.HiddenVehicleAccessPoint
                            : lot.transform;
                    Add("Parking", SafeString(lot.name, "Parking Lot"), point.position);
                }
            }
            catch { }
        }

        private void AddNpcs()
        {
            try
            {
                NPC[] npcs = UnityEngine.Object.FindObjectsOfType<NPC>(true);
                if (npcs == null)
                    return;

                for (int i = 0; i < npcs.Length; i++)
                {
                    NPC npc = npcs[i];
                    if (npc == null || npc.transform == null || !npc.gameObject.activeInHierarchy)
                        continue;

                    Add("NPC", GetNpcName(npc), npc.transform.position);
                }
            }
            catch { }
        }

        private Vector3 GetBestPropertyPoint(Property property)
        {
            if (property == null)
                return Vector3.zero;

            try
            {
                if (property.InteriorSpawnPoint != null)
                    return property.InteriorSpawnPoint.position;
            }
            catch { }

            try
            {
                if (property.SpawnPoint != null)
                    return property.SpawnPoint.position;
            }
            catch { }

            try
            {
                if (property.NPCSpawnPoint != null)
                    return property.NPCSpawnPoint.position;
            }
            catch { }

            return property.transform.position;
        }

        private Vector3 FindObjectPosition(string name, Vector3 fallback)
        {
            try
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null)
                    return obj.transform.position;
            }
            catch { }

            return fallback;
        }

        private void Add(string category, string label, Vector3 position)
        {
            if (position == Vector3.zero)
                return;

            label = CleanLabel(label);
            category = string.IsNullOrEmpty(category) ? "Location" : category;

            if (ShouldSpatiallyDedupe(category) && HasNearbyDestination(position))
                return;

            string key = category + "|" + Normalize(label) + "|" +
                Mathf.RoundToInt(position.x) + "|" +
                Mathf.RoundToInt(position.y) + "|" +
                Mathf.RoundToInt(position.z);
            if (!_destinationKeys.Add(key))
                return;

            _destinations.Add(new TeleportDestination
            {
                Category = category,
                Label = label,
                Position = position
            });
        }

        private static bool ShouldSpatiallyDedupe(string category)
        {
            return !string.Equals(category, "NPC", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(category, "Location", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasNearbyDestination(Vector3 position)
        {
            float maxDistanceSq = NearDuplicateDistance * NearDuplicateDistance;
            for (int i = 0; i < _destinations.Count; i++)
            {
                TeleportDestination destination = _destinations[i];
                if (!ShouldSpatiallyDedupe(destination.Category))
                    continue;

                if ((destination.Position - position).sqrMagnitude <= maxDistanceSq)
                    return true;
            }

            return false;
        }

        private void SortDestinations()
        {
            _destinations.Sort((a, b) =>
            {
                int category = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
                if (category != 0)
                    return category;

                return string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static string SafeString(params string[] values)
        {
            if (values == null)
                return "Unknown";

            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                    return values[i];
            }

            return "Unknown";
        }

        private static string CleanLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            value = CollapseWhitespace(value.Replace("(Clone)", "").Replace("_", " "));
            return value.Length <= 64 ? value : value.Substring(0, 61) + "...";
        }

        private static string CollapseWhitespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            char[] chars = value.ToCharArray();
            char[] compact = new char[chars.Length];
            int count = 0;
            bool previousWasSpace = false;

            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (char.IsWhiteSpace(c))
                {
                    if (count > 0 && !previousWasSpace)
                    {
                        compact[count++] = ' ';
                        previousWasSpace = true;
                    }
                    continue;
                }

                compact[count++] = c;
                previousWasSpace = false;
            }

            if (count > 0 && compact[count - 1] == ' ')
                count--;

            return new string(compact, 0, count);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            char[] chars = new char[value.Length];
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = char.ToLowerInvariant(value[i]);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    chars[count++] = c;
            }

            return new string(chars, 0, count);
        }

        private static string GetNpcName(NPC npc)
        {
            if (npc == null)
                return "NPC";

            try
            {
                if (!string.IsNullOrEmpty(npc.fullName))
                    return npc.fullName;
            }
            catch { }

            try
            {
                string first = npc.FirstName ?? "";
                string last = npc.hasLastName ? npc.LastName ?? "" : "";
                string full = (first + " " + last).Trim();
                if (!string.IsNullOrEmpty(full))
                    return full;
            }
            catch { }

            try { return npc.name ?? "NPC"; }
            catch { return "NPC"; }
        }
    }
}
