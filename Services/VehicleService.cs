using System;
using System.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;
using static UnityEngine.Object;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Service for vehicle-related operations
    /// </summary>
    public sealed class VehicleService
    {
        private static readonly VehicleService _instance = new VehicleService();
        public static VehicleService Instance => _instance;
        private VehicleService() { }
        // Cached vehicle data
        private string[] _vehicleCodes = new string[0];
        private string[] _vehicleNames = new string[0];
        private bool[] _vehicleRisky = new bool[0];
        private int _vehicleCount = 0;
        private bool _isCached = false;

        // Selected vehicle index
        private int _selectedIndex = 0;
        private readonly List<VisualRepairRequest> _visualRepairs = new List<VisualRepairRequest>();

        private struct VisualRepairRequest
        {
            public LandVehicle Vehicle;
            public int FramesRemaining;
        }

        /// <summary>
        /// Initializes the vehicle cache by loading all vehicles from the manager
        /// </summary>
        public void InitializeCache()
        {
            if (_isCached)
                return;

            try
            {
                var vehicleManager = FindObjectOfType<VehicleManager>();
                if (vehicleManager == null || vehicleManager.VehiclePrefabs == null || vehicleManager.VehiclePrefabs.Count == 0)
                {
                    UnityEngine.Debug.LogError("[Nugzz] VehicleManager not found or empty");
                    return;
                }

                var codes = new List<string>();
                var names = new List<string>();
                var risky = new List<bool>();

                for (int i = 0; i < vehicleManager.VehiclePrefabs.Count; i++)
                {
                    try
                    {
                        AddVehicleCandidate(vehicleManager, vehicleManager.VehiclePrefabs[i], codes, names, risky);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[Nugzz] Error loading vehicle prefab {i}: {ex.Message}");
                    }
                }

                try
                {
                    var loadedVehicles = Resources.FindObjectsOfTypeAll<LandVehicle>();
                    if (loadedVehicles != null)
                    {
                        for (int i = 0; i < loadedVehicles.Length; i++)
                        {
                            AddVehicleCandidate(vehicleManager, loadedVehicles[i], codes, names, risky);
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] Extra vehicle scan failed: " + ex.Message);
                }

                _vehicleCodes = codes.ToArray();
                _vehicleNames = names.ToArray();
                _vehicleRisky = risky.ToArray();
                _vehicleCount = _vehicleCodes.Length;

                // Sort vehicles by name
                SortVehicles();

                _isCached = true;
                UnityEngine.Debug.Log($"[Nugzz] Loaded {_vehicleCount} vehicles into cache");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] Failed to initialize vehicle cache: {ex.Message}");
            }
        }

        public void Update()
        {
            for (int i = _visualRepairs.Count - 1; i >= 0; i--)
            {
                VisualRepairRequest request = _visualRepairs[i];
                if (request.Vehicle == null || request.Vehicle.gameObject == null)
                {
                    _visualRepairs.RemoveAt(i);
                    continue;
                }

                EnsureLocalRiskyVehicleVisible(request.Vehicle, request.Vehicle.transform.position, request.Vehicle.transform.rotation, false);
                request.FramesRemaining--;

                if (request.FramesRemaining <= 0)
                    _visualRepairs.RemoveAt(i);
                else
                    _visualRepairs[i] = request;
            }
        }

        private void AddVehicleCandidate(VehicleManager vehicleManager, LandVehicle landVehicle, List<string> codes, List<string> names, List<bool> risky)
        {
            if (vehicleManager == null || landVehicle == null || string.IsNullOrEmpty(landVehicle.VehicleCode))
                return;

            string vehicleCode = landVehicle.VehicleCode;
            for (int i = 0; i < codes.Count; i++)
            {
                if (string.Equals(codes[i], vehicleCode, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            string cleanName = CleanVehicleName(landVehicle.name, vehicleCode);

            bool isRisky = IsRiskyVehicle(landVehicle.name) || IsRiskyVehicle(vehicleCode) || IsRiskyVehicle(cleanName);

            if (isRisky)
            {
                DebugLogService.Instance.Verbose("Skipped police/NPC vehicle: " + cleanName + " (" + vehicleCode + ")");
                return;
            }

            if (!CanManagerSpawn(vehicleManager, vehicleCode))
            {
                UnityEngine.Debug.Log("[Nugzz] Skipped vehicle not registered for spawning: " + landVehicle.name + " (" + vehicleCode + ")");
                return;
            }

            codes.Add(vehicleCode);
            names.Add(cleanName);
            risky.Add(isRisky);

            if (isRisky)
                UnityEngine.Debug.Log("[Nugzz] Added warning vehicle: " + cleanName + " (" + vehicleCode + ")");
        }

        /// <summary>
        /// Clears the vehicle cache
        /// </>
        public void ClearCache()
        {
            _vehicleCodes = new string[0];
            _vehicleNames = new string[0];
            _vehicleRisky = new bool[0];
            _vehicleCount = 0;
            _isCached = false;
            _selectedIndex = 0;
        }

        /// <summary>
        /// Gets the cached vehicle codes
        /// </summary>
        public string[] GetVehicleCodes()
        {
            if (!_isCached)
                InitializeCache();

            // Return only the valid entries
            var codes = new string[_vehicleCount];
            Array.Copy(_vehicleCodes, codes, _vehicleCount);
            return codes;
        }

        /// <summary>
        /// Gets the cached vehicle names
        /// </summary>
        public string[] GetVehicleNames()
        {
            if (!_isCached)
                InitializeCache();

            // Return only the valid entries
            var names = new string[_vehicleCount];
            Array.Copy(_vehicleNames, names, _vehicleCount);
            return names;
        }

        /// <summary>
        /// Gets the number of cached vehicles
        /// </summary>
        public int GetVehicleCount()
        {
            if (!_isCached)
                InitializeCache();

            return _vehicleCount;
        }

        /// <summary>
        /// Gets the vehicle code at the specified index
        /// </summary>
        public string GetVehicleCodeAt(int index)
        {
            if (index < 0 || index >= _vehicleCount)
                return null;

            return _vehicleCodes[index];
        }

        /// <summary>
        /// Gets the vehicle name at the specified index
        /// </summary>
        public string GetVehicleNameAt(int index)
        {
            if (index < 0 || index >= _vehicleCount)
                return null;

            return _vehicleNames[index];
        }

        public bool IsVehicleRiskyAt(int index)
        {
            if (index < 0 || index >= _vehicleCount || index >= _vehicleRisky.Length)
                return false;

            return _vehicleRisky[index];
        }

        /// <summary>
        /// Sets the selected vehicle index
        /// </summary>
        public void SetSelectedIndex(int index)
        {
            if (index < 0)
                index = 0;
            if (index >= _vehicleCount)
                index = 0;

            _selectedIndex = index;
        }

        /// <summary>
        /// Gets the currently selected vehicle index
        /// </summary>
        public int GetSelectedIndex()
        {
            return _selectedIndex;
        }

        /// <summary>
        /// Gets the currently selected vehicle code
        /// </summary>
        public string GetSelectedVehicleCode()
        {
            return GetVehicleCodeAt(_selectedIndex);
        }

        /// <summary>
        /// Gets the currently selected vehicle name
        /// </summary>
        public string GetSelectedVehicleName()
        {
            return GetVehicleNameAt(_selectedIndex);
        }

        public bool IsSelectedVehicleRisky()
        {
            return IsVehicleRiskyAt(_selectedIndex);
        }

        public string GetSelectedVehicleRiskWarning()
        {
            if (!IsSelectedVehicleRisky())
                return "";

            return "Warning: NPC/police vehicles can have broken ownership, seats, or AI setup.";
        }

        /// <summary>
        /// Attempts to spawn the currently selected vehicle
        /// </summary>
        /// <returns>The name of the spawned vehicle on success, null on failure</returns>
        public string SpawnSelectedVehicle()
        {
            if (_vehicleCount == 0)
            {
                UnityEngine.Debug.LogError("[Nugzz] No vehicles cached");
                return null;
            }

            string vehicleCode = GetSelectedVehicleCode();
            string vehicleName = GetSelectedVehicleName();
            bool riskyVehicle = IsSelectedVehicleRisky();

            if (string.IsNullOrEmpty(vehicleCode))
            {
                UnityEngine.Debug.LogError("[Nugzz] Selected vehicle code is null or empty");
                return null;
            }

            try
            {
                if (riskyVehicle)
                {
                    string warning = GetSelectedVehicleRiskWarning();
                    NotificationService.Instance.Warning(warning);
                    UnityEngine.Debug.LogWarning("[Nugzz] Risky vehicle spawn requested: " + vehicleName + " (" + vehicleCode + ") - " + warning);
                }

                var player = GetLocalPlayer();
                if (player == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] No local player found");
                    return null;
                }

                // Derive spawn position from PLAYER forward only, NEVER camera look direction.
                // Camera look can point into walls/ceiling and would spawn the vehicle on top of
                // the player causing a physics explosion / crash.
                var playerTransform = player.transform;
                Vector3 fwdXZ = Vector3.Normalize(new Vector3(
                    playerTransform.forward.x,
                    0f,
                    playerTransform.forward.z));

                float spawnDist = 4f;           // closer in front of player for easier access
                float clearanceHeight = 2.5f;   // height above ground so collider is not embedded
                Vector3 spawnPosition = playerTransform.position + fwdXZ * spawnDist + new Vector3(0f, clearanceHeight, 0f);

                int allLayers = ~0;

                // Spawn through VehicleManager whenever possible so police variants keep the same
                // vehicle controller/seat setup as normal drivable vehicles. AI is sanitized after spawn.
                bool spawnAsPlayerOwned = true;
                try
                {
                    var lobbyService = LobbyService.Instance;
                    if (lobbyService.IsInLobby())
                    {
                            // Only host spawns networked vehicles that persist for all players.
                            spawnAsPlayerOwned = lobbyService.IsHost();
                    }
                }
                catch { /* fallback to local assumption */ }

                // Snap to ground
                if (Physics.Raycast(
                    spawnPosition + Vector3.up * 25f,
                    Vector3.down,
                    out RaycastHit groundHit,
                    50f,
                    allLayers))
                {
                    spawnPosition = groundHit.point + new Vector3(0f, 0.3f, 0f);
                }
                else
                {
                    // No ground below – fall back to player height + 1 unit
                    spawnPosition.y = playerTransform.position.y + 1f;
                }

                // If the spawn box is already occupied, push further out
                if (Physics.CheckBox(
                    spawnPosition,
                    new Vector3(1.5f, 0.8f, 2.5f),
                    Quaternion.identity,
                    allLayers))
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] Spawn area occupied, pushing back 2 m");
                    Vector3 altPos = playerTransform.position + fwdXZ * (spawnDist + 2f) + new Vector3(0f, clearanceHeight, 0f);

                    if (Physics.Raycast(
                        altPos + Vector3.up * 25f,
                        Vector3.down,
                        out RaycastHit groundHit2,
                        50f,
                        allLayers))
                    {
                        spawnPosition = groundHit2.point + new Vector3(0f, 0.3f, 0f);
                    }
                    else
                    {
                        spawnPosition = altPos;
                    }
                }

                string cleanName = CleanVehicleName(vehicleName, vehicleCode);

                // Validate prefab exists before touching the network layer
                LandVehicle prefab = GetVehiclePrefab(vehicleCode);
                if (prefab == null)
                {
                    UnityEngine.Debug.LogError($"[Nugzz] Vehicle prefab not found for code: {vehicleCode}");
                    return null;
                }

                Quaternion rotation = Quaternion.Euler(0f, playerTransform.eulerAngles.y, 0f);

                UnityEngine.Debug.Log($"[Nugzz] Spawning '{cleanName}' at {spawnPosition}  fwd={fwdXZ}  owned={spawnAsPlayerOwned} risky={riskyVehicle}");

                LandVehicle spawned = SpawnAndReturnVehicle(vehicleCode, spawnPosition, rotation, spawnAsPlayerOwned);
                if (spawned != null)
                {
                    if (riskyVehicle)
                    {
                        SanitizeRiskyVehicle(spawned);
                        try { spawned.SetIsPlayerOwned(null, true); } catch { }
                    }

                    VehicleCollisionService.Instance.ApplyVehicle(spawned);

                    // Mark vehicle as drivable by ensuring it's properly set up
                    try
                    {
                        if (spawnAsPlayerOwned)
                        {
                            // Ensure the vehicle is marked as player-owned for drivability
                            spawned.SetIsPlayerOwned(null, true);
                        }
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning($"[Nugzz] Failed to mark vehicle as drivable: {e.Message}");
                    }
                    
                    UnityEngine.Debug.Log($"[Nugzz] Successfully spawned {cleanName}");
                    return cleanName;
                }

                UnityEngine.Debug.LogWarning($"[Nugzz] SpawnAndReturnVehicle returned null, trying fire-and-forget");
                SpawnVehicle(vehicleCode, spawnPosition, rotation, spawnAsPlayerOwned);
                UnityEngine.Debug.Log($"[Nugzz] Fire-and-forget spawn issued for {cleanName}");
                return cleanName;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to spawn vehicle: " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Cleans a vehicle name by removing unwanted elements and trimming
        /// </summary>
        private string CleanVehicleName(string rawName, string code)
        {
            try
            {
                string text = rawName;
                if (string.IsNullOrEmpty(text))
                    text = code;

                text = text.Replace("(Clone)", "").Replace("_", " ").Trim();

                if (string.IsNullOrEmpty(text))
                    text = code;

                return text;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] Error cleaning vehicle name: {ex.Message}");
                return code ?? "Unknown";
            }
        }

        private bool IsRiskyVehicle(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            string lower = value.ToLowerInvariant();
            return lower.Contains("police") ||
                lower.Contains("sheriff") ||
                lower.Contains("patrol") ||
                lower.Contains("pursuit") ||
                lower.Contains("swat") ||
                lower.Contains("law") ||
                lower.Contains("roadblock") ||
                lower.Contains("npc");
        }

        private void SanitizeRiskyVehicle(LandVehicle vehicle)
        {
            if (vehicle == null)
                return;

            try { vehicle.SpawnAsPlayerOwned = false; } catch { }

            try
            {
                if (vehicle.Agent != null)
                {
                    vehicle.Agent.StopNavigating();
                    vehicle.Agent.enabled = false;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Failed to stop risky vehicle agent: " + ex.Message);
            }

            try
            {
                var seats = vehicle.Seats;
                if (seats != null)
                {
                    for (int i = 0; i < seats.Length; i++)
                    {
                        try
                        {
                            if (seats[i] != null)
                                seats[i].Occupant = null;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                var behaviours = vehicle.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours == null)
                    return;

                for (int i = 0; i < behaviours.Length; i++)
                {
                    var behaviour = behaviours[i];
                    if (behaviour == null)
                        continue;

                    string typeName = "";
                    try { typeName = behaviour.GetIl2CppType()?.FullName ?? behaviour.GetType().FullName ?? ""; }
                    catch { typeName = behaviour.GetType().FullName ?? ""; }

                    if (!ShouldDisableRiskyVehicleBehaviour(typeName))
                        continue;

                    try { behaviour.enabled = false; } catch { }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Failed to sanitize risky vehicle behaviours: " + ex.Message);
            }
        }

        private bool ShouldDisableRiskyVehicleBehaviour(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            string lower = typeName.ToLowerInvariant();
            return lower.Contains("vehicleagent") ||
                lower.Contains("vehiclepatrol") ||
                lower.Contains("deliveryvehicle") ||
                lower.Contains("policelight") ||
                lower.Contains("obstructiondetector");
        }

        private LandVehicle SpawnRiskyVehicleLocal(LandVehicle prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null || prefab.gameObject == null)
                return null;

            GameObject prefabObject = prefab.gameObject;
            bool restoreActive = prefabObject.activeSelf;

            try
            {
                if (restoreActive)
                    prefabObject.SetActive(false);

                GameObject clone = Instantiate(prefabObject, position, rotation);
                if (clone == null)
                    return null;

                clone.name = CleanVehicleName(prefab.name, prefab.VehicleCode) + " (Nugzz Local)";

                LandVehicle vehicle = clone.GetComponent<LandVehicle>();
                if (vehicle == null)
                {
                    Destroy(clone);
                    return null;
                }

                SanitizeRiskyVehicle(vehicle);
                clone.transform.SetPositionAndRotation(position, rotation);
                clone.SetActive(true);
                EnsureLocalRiskyVehicleVisible(vehicle, position, rotation);
                QueueLocalRiskyVehicleVisualRepair(vehicle);
                RegisterLocalVehicle(vehicle);
                return vehicle;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed local risky vehicle spawn: " + ex);
                return null;
            }
            finally
            {
                try
                {
                    if (restoreActive)
                        prefabObject.SetActive(true);
                }
                catch { }
            }
        }

        private void EnsureLocalRiskyVehicleVisible(LandVehicle vehicle, Vector3 position, Quaternion rotation, bool resetTransform = true)
        {
            if (vehicle == null || vehicle.gameObject == null)
                return;

            if (resetTransform)
            {
                try { vehicle.transform.SetPositionAndRotation(position, rotation); } catch { }
            }

            try { vehicle.gameObject.SetActive(true); } catch { }
            try { vehicle.SpawnAsPlayerOwned = false; } catch { }
            try { vehicle.SetVisible(true); } catch { }

            try
            {
                if (vehicle.vehicleModel != null)
                    vehicle.vehicleModel.SetActive(true);
            }
            catch { }

            try
            {
                if (vehicle.Color != null)
                {
                    try { vehicle.Color.gameObject.SetActive(true); } catch { }
                    try { vehicle.Color.enabled = true; } catch { }
                    try { vehicle.Color.Start(); } catch { }
                    try { vehicle.ApplyColor(vehicle.Color.DefaultColor); } catch { }
                    try { vehicle.Color.ApplyColor(vehicle.Color.DefaultColor); } catch { }
                }

                var vehicleColors = vehicle.GetComponentsInChildren<VehicleColor>(true);
                if (vehicleColors != null)
                {
                    for (int i = 0; i < vehicleColors.Length; i++)
                    {
                        var color = vehicleColors[i];
                        if (color == null)
                            continue;

                        try { color.gameObject.SetActive(true); } catch { }
                        try { color.enabled = true; } catch { }
                        try { color.Start(); } catch { }
                        try { color.ApplyColor(color.DefaultColor); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Failed to restore risky vehicle color materials: " + ex.Message);
            }

            try
            {
                var wheels = vehicle.GetComponentsInChildren<Wheel>(true);
                if (wheels != null)
                {
                    for (int i = 0; i < wheels.Length; i++)
                    {
                        var wheel = wheels[i];
                        if (wheel == null)
                            continue;

                        try { wheel.gameObject.SetActive(true); } catch { }
                        try { wheel.enabled = true; } catch { }
                        try { wheel.modelContainer?.gameObject.SetActive(true); } catch { }
                        try { wheel.wheelModel?.gameObject.SetActive(true); } catch { }
                        try { wheel.wheelCollider?.gameObject.SetActive(true); } catch { }
                        try { if (wheel.wheelCollider != null) wheel.wheelCollider.enabled = true; } catch { }
                        try { if (wheel.staticCollider != null) wheel.staticCollider.enabled = true; } catch { }
                        try { wheel.SetPhysicsEnabled(true); } catch { }
                        try { wheel.ApplyDefaultWheelModelPosition(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Failed to restore risky vehicle wheels: " + ex.Message);
            }

            try
            {
                var renderers = vehicle.GetComponentsInChildren<Renderer>(true);
                if (renderers != null)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        try
                        {
                            if (renderers[i] != null)
                            {
                                EnsureHierarchyActive(renderers[i].transform, vehicle.transform);
                                renderers[i].enabled = true;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                var colliders = vehicle.GetComponentsInChildren<Collider>(true);
                if (colliders != null)
                {
                    for (int i = 0; i < colliders.Length; i++)
                    {
                        try
                        {
                            if (colliders[i] != null)
                                colliders[i].enabled = true;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                var rigidbody = vehicle.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.isKinematic = false;
                    rigidbody.detectCollisions = true;
                    rigidbody.WakeUp();
                }
            }
            catch { }
        }

        private void QueueLocalRiskyVehicleVisualRepair(LandVehicle vehicle)
        {
            if (vehicle == null)
                return;

            _visualRepairs.Add(new VisualRepairRequest
            {
                Vehicle = vehicle,
                FramesRemaining = 45
            });
        }

        private void EnsureHierarchyActive(Transform child, Transform stopAt)
        {
            try
            {
                Transform current = child;
                while (current != null)
                {
                    current.gameObject.SetActive(true);
                    if (current == stopAt)
                        break;
                    current = current.parent;
                }
            }
            catch { }
        }

        private void RegisterLocalVehicle(LandVehicle vehicle)
        {
            try
            {
                var manager = FindObjectOfType<VehicleManager>();
                if (manager?.AllVehicles == null || vehicle == null)
                    return;

                if (!manager.AllVehicles.Contains(vehicle))
                    manager.AllVehicles.Add(vehicle);
            }
            catch { }
        }

        private bool CanManagerSpawn(VehicleManager vehicleManager, string vehicleCode)
        {
            try
            {
                return vehicleManager != null && !string.IsNullOrEmpty(vehicleCode) && vehicleManager.GetVehiclePrefab(vehicleCode) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sorts the vehicle cache by name
        /// </summary>
        private void SortVehicles()
        {
            for (int i = 1; i < _vehicleCount; i++)
            {
                string name = _vehicleNames[i];
                string code = _vehicleCodes[i];
                bool risky = _vehicleRisky[i];
                int j = i - 1;

                while (j >= 0 && string.Compare(_vehicleNames[j], name, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    _vehicleNames[j + 1] = _vehicleNames[j];
                    _vehicleCodes[j + 1] = _vehicleCodes[j];
                    _vehicleRisky[j + 1] = _vehicleRisky[j];
                    j--;
                }

                _vehicleNames[j + 1] = name;
                _vehicleCodes[j + 1] = code;
                _vehicleRisky[j + 1] = risky;
            }
        }

        /// <summary>
        /// Gets the local player
        /// </summary>
        private Player GetLocalPlayer()
        {
            try
            {
                return Player.Local;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] Failed to get local player: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a vehicle prefab by its code
        /// </summary>
        private LandVehicle GetVehiclePrefab(string vehicleCode)
        {
            var vehicleManager = FindObjectOfType<VehicleManager>();
            if (vehicleManager == null)
                return null;

            return vehicleManager.GetVehiclePrefab(vehicleCode);
        }

        /// <summary>
        /// Spawns and returns a vehicle
        /// </summary>
        private LandVehicle SpawnAndReturnVehicle(string vehicleCode, Vector3 position, Quaternion rotation, bool playerOwned)
        {
            try
            {
                var vehicleManager = FindObjectOfType<VehicleManager>();
                if (vehicleManager == null)
                    return null;

                return vehicleManager.SpawnAndReturnVehicle(vehicleCode, position, rotation, playerOwned);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] Failed to spawn and return vehicle: {ex.ToString()}");
                return null;
            }
        }

        /// <summary>
        /// Spawns a vehicle (fire-and-forget)
        /// </summary>
        private void SpawnVehicle(string vehicleCode, Vector3 position, Quaternion rotation, bool playerOwned)
        {
            try
            {
                var vehicleManager = FindObjectOfType<VehicleManager>();
                if (vehicleManager == null)
                    return;

                vehicleManager.SpawnVehicle(vehicleCode, position, rotation, playerOwned);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] Failed to spawn vehicle: {ex.ToString()}");
            }
        }
    }
}
