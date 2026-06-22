using System;
using System.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Lighting;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles.Modification;
using Il2Cpp;
using Il2CppFishNet.Object;
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
        private static readonly EVehicleColor[] TunableBodyColors =
        {
            EVehicleColor.Black,
            EVehicleColor.DarkGrey,
            EVehicleColor.LightGrey,
            EVehicleColor.White,
            EVehicleColor.Yellow,
            EVehicleColor.Orange,
            EVehicleColor.Red,
            EVehicleColor.DullRed,
            EVehicleColor.Pink,
            EVehicleColor.Purple,
            EVehicleColor.Navy,
            EVehicleColor.DarkBlue,
            EVehicleColor.LightBlue,
            EVehicleColor.Cyan,
            EVehicleColor.LightGreen,
            EVehicleColor.DarkGreen
        };
        private VehicleService() { }
        // Cached vehicle data
        private string[] _vehicleCodes = new string[0];
        private string[] _vehicleNames = new string[0];
        private bool[] _vehicleRisky = new bool[0];
        private string[] _vehicleSources = new string[0];
        private string[] _vehicleCategories = new string[0];
        private LandVehicle[] _vehiclePrefabs = new LandVehicle[0];
        private int _vehicleCount = 0;
        private bool _isCached = false;

        // Selected vehicle index
        private int _selectedIndex = 0;
        private readonly List<VisualRepairRequest> _visualRepairs = new List<VisualRepairRequest>();
        private bool _benzieManorAccessEnabled;
        private float _nextVehicleCacheRetryTime;
        private bool _reportedMissingVehicleManager;
        private bool _reportedPoliceSirenFailure;
        private bool _reportedPoliceSirenMissingLightbar;
        private bool _policeSirenOn;
        private LandVehicle _lastPoliceSirenVehicle;
        private int _spawnAttemptId;
        private readonly Dictionary<string, VehicleTuneSettings> _vehicleTunes = new Dictionary<string, VehicleTuneSettings>();
        private readonly Dictionary<string, VehicleTuneBaseline> _vehicleTuneBaselines = new Dictionary<string, VehicleTuneBaseline>();
        private LandVehicle _cachedDrivenVehicle;
        private float _nextDrivenVehicleLookupTime;
        private float _nextVehicleTuneMaintenanceTime;

        public bool BenzieManorAccessEnabled => _benzieManorAccessEnabled;

        public sealed class VehicleTuneSettings
        {
            public float TractionMultiplier = 1f;
            public float SteeringMultiplier = 1f;
            public float SpeedMultiplier = 1f;
            public float BrakeMultiplier = 1f;
            public float BrakeHardnessMultiplier = 1f;
            public float HeadlightBrightnessMultiplier = 1f;
            public float HeadlightRed = 1f;
            public float HeadlightGreen = 1f;
            public float HeadlightBlue = 1f;
            public bool BodyColorEnabled;
            public EVehicleColor BodyColor = EVehicleColor.Black;
            internal bool HasAppliedBodyColor;
            internal EVehicleColor LastAppliedBodyColor = EVehicleColor.Black;

            public void Reset()
            {
                TractionMultiplier = 1f;
                SteeringMultiplier = 1f;
                SpeedMultiplier = 1f;
                BrakeMultiplier = 1f;
                BrakeHardnessMultiplier = 1f;
                HeadlightBrightnessMultiplier = 1f;
                HeadlightRed = 1f;
                HeadlightGreen = 1f;
                HeadlightBlue = 1f;
                BodyColorEnabled = false;
                BodyColor = EVehicleColor.Black;
                HasAppliedBodyColor = false;
                LastAppliedBodyColor = EVehicleColor.Black;
            }
        }

        private struct VisualRepairRequest
        {
            public LandVehicle Vehicle;
            public int FramesRemaining;
        }

        private sealed class VehicleTuneBaseline
        {
            public float TopSpeed;
            public float MaxSteeringAngle;
            public float SteerRate;
            public float BrakeForceMultiplier;
            public float HandBrakeForce;
            public float DiffGearing;
            public float Downforce;
            public float ReverseMultiplier;
            public AnimationCurve MotorTorque;
            public AnimationCurve BrakeForce;
            public readonly Dictionary<int, WheelFrictionBaseline> WheelBaselines = new Dictionary<int, WheelFrictionBaseline>();
            public readonly Dictionary<int, LightBaseline> LightBaselines = new Dictionary<int, LightBaseline>();
            public bool HasBodyColor;
            public EVehicleColor BodyColor;
        }

        private struct WheelFrictionBaseline
        {
            public float ForwardStiffness;
            public float SidewaysStiffness;
        }

        private struct LightBaseline
        {
            public Color Color;
            public float Intensity;
            public float Range;
        }

        private sealed class VehicleCacheStats
        {
            public int ManagerPrefabsSeen;
            public int ResourceVehiclesSeen;
            public int Added;
            public int SpecialAdded;
            public int DuplicateSkipped;
            public int DuplicateReplaced;
            public int ExtraResourceAdded;
            public int MissingCodeSkipped;
            public int UnregisteredSkipped;
            public int NullSkipped;
            public readonly List<string> SpecialExamples = new List<string>();
            public readonly List<string> UnregisteredExamples = new List<string>();
            public readonly List<string> ExtraResourceExamples = new List<string>();
        }

        /// <summary>
        /// Initializes the vehicle cache by loading all vehicles from the manager
        /// </summary>
        public void InitializeCache()
        {
            if (_isCached)
                return;

            if (_reportedMissingVehicleManager && Time.unscaledTime < _nextVehicleCacheRetryTime)
                return;

            try
            {
                var vehicleManager = FindObjectOfType<VehicleManager>();
                if (vehicleManager == null || vehicleManager.VehiclePrefabs == null || vehicleManager.VehiclePrefabs.Count == 0)
                {
                    if (Time.unscaledTime < _nextVehicleCacheRetryTime)
                        return;

                    _nextVehicleCacheRetryTime = Time.unscaledTime + 3f;
                    if (!_reportedMissingVehicleManager)
                    {
                        _reportedMissingVehicleManager = true;
                        UnityEngine.Debug.LogWarning("[Nugzz] VehicleManager not found or empty; vehicle list will retry quietly");
                    }
                    return;
                }

                var codes = new List<string>();
                var names = new List<string>();
                var risky = new List<bool>();
                var sources = new List<string>();
                var categories = new List<string>();
                var prefabs = new List<LandVehicle>();
                var stats = new VehicleCacheStats();

                for (int i = 0; i < vehicleManager.VehiclePrefabs.Count; i++)
                {
                    try
                    {
                        stats.ManagerPrefabsSeen++;
                        AddVehicleCandidate(vehicleManager, vehicleManager.VehiclePrefabs[i], "VehicleManager",
                            codes, names, risky, sources, categories, prefabs, stats);
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
                            stats.ResourceVehiclesSeen++;
                            AddVehicleCandidate(vehicleManager, loadedVehicles[i], "Resources",
                                codes, names, risky, sources, categories, prefabs, stats);
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
                _vehicleSources = sources.ToArray();
                _vehicleCategories = categories.ToArray();
                _vehiclePrefabs = prefabs.ToArray();
                _vehicleCount = _vehicleCodes.Length;

                // Sort vehicles by name
                SortVehicles();

                _isCached = true;
                _reportedMissingVehicleManager = false;
                LogVehicleCacheSummary(stats);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] Failed to initialize vehicle cache: {ex.Message}");
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                TogglePoliceSirenForCurrentVehicle();

            MaintainPoliceSirenState();
            if (_vehicleTunes.Count > 0 && Time.unscaledTime >= _nextVehicleTuneMaintenanceTime)
            {
                _nextVehicleTuneMaintenanceTime = Time.unscaledTime + 1.5f;
                ApplyVehicleTunes();
            }

            if (_benzieManorAccessEnabled)
                EnsureBenzieManorAccess(false);

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

        private void AddVehicleCandidate(
            VehicleManager vehicleManager,
            LandVehicle landVehicle,
            string source,
            List<string> codes,
            List<string> names,
            List<bool> risky,
            List<string> sources,
            List<string> categories,
            List<LandVehicle> prefabs,
            VehicleCacheStats stats)
        {
            if (vehicleManager == null || landVehicle == null)
            {
                stats.NullSkipped++;
                return;
            }

            if (string.IsNullOrEmpty(landVehicle.VehicleCode))
            {
                stats.MissingCodeSkipped++;
                return;
            }

            string vehicleCode = landVehicle.VehicleCode;
            string cleanName = CleanVehicleName(landVehicle.name, vehicleCode);
            string category = GetVehicleCategory(landVehicle.name, vehicleCode, cleanName);

            bool isRisky = IsRiskyVehicle(landVehicle.name) || IsRiskyVehicle(vehicleCode) || IsRiskyVehicle(cleanName);
            bool managerSpawnable = CanManagerSpawn(vehicleManager, vehicleCode);
            bool extraResourceVehicle = false;

            if (!managerSpawnable)
            {
                if (!ShouldExposeExtraVehicle(landVehicle, source, cleanName, vehicleCode, category))
                {
                    stats.UnregisteredSkipped++;
                    AddLimitedExample(stats.UnregisteredExamples, cleanName + " (" + vehicleCode + ") from " + source);
                    return;
                }

                isRisky = true;
                extraResourceVehicle = true;
            }

            int existingIndex = FindVehicleCodeIndex(codes, vehicleCode);
            if (existingIndex >= 0)
            {
                LandVehicle existingPrefab = existingIndex < prefabs.Count ? prefabs[existingIndex] : null;
                string existingSource = existingIndex < sources.Count ? sources[existingIndex] : "";
                bool existingManagerSpawnable = string.Equals(existingSource, "VehicleManager", StringComparison.OrdinalIgnoreCase);

                if (!ShouldReplaceVehicleCandidate(existingPrefab, existingSource, existingManagerSpawnable, landVehicle, source, managerSpawnable))
                {
                    stats.DuplicateSkipped++;
                    return;
                }

                names[existingIndex] = cleanName;
                risky[existingIndex] = isRisky;
                sources[existingIndex] = source;
                categories[existingIndex] = category;
                prefabs[existingIndex] = landVehicle;
                stats.DuplicateReplaced++;

                if (extraResourceVehicle)
                {
                    stats.ExtraResourceAdded++;
                    AddLimitedExample(stats.ExtraResourceExamples, cleanName + " (" + vehicleCode + ")");
                }

                return;
            }

            codes.Add(vehicleCode);
            names.Add(cleanName);
            risky.Add(isRisky);
            sources.Add(source);
            categories.Add(category);
            prefabs.Add(landVehicle);
            stats.Added++;

            if (isRisky)
            {
                stats.SpecialAdded++;
                AddLimitedExample(stats.SpecialExamples, cleanName + " (" + vehicleCode + ")");
            }

            if (extraResourceVehicle)
            {
                stats.ExtraResourceAdded++;
                AddLimitedExample(stats.ExtraResourceExamples, cleanName + " (" + vehicleCode + ")");
            }
        }

        private void LogVehicleCacheSummary(VehicleCacheStats stats)
        {
            UnityEngine.Debug.Log(
                "[Nugzz] Loaded " + _vehicleCount +
                " vehicles into cache (manager=" + stats.ManagerPrefabsSeen +
                ", resources=" + stats.ResourceVehiclesSeen +
                ", special=" + stats.SpecialAdded +
                ", extra=" + stats.ExtraResourceAdded +
                ", duplicates=" + stats.DuplicateSkipped +
                ", replaced=" + stats.DuplicateReplaced +
                ", unregistered=" + stats.UnregisteredSkipped +
                ", missingCode=" + stats.MissingCodeSkipped +
                ").");

            if (!DebugLogService.Instance.VerboseEnabled)
                return;

            if (stats.SpecialExamples.Count > 0)
                DebugLogService.Instance.Verbose("Vehicle special entries: " + string.Join(", ", stats.SpecialExamples));

            if (stats.ExtraResourceExamples.Count > 0)
                DebugLogService.Instance.Verbose("Vehicle extra resource entries: " + string.Join(", ", stats.ExtraResourceExamples));

            if (stats.UnregisteredExamples.Count > 0)
                DebugLogService.Instance.Verbose("Vehicle entries skipped because VehicleManager cannot spawn them: " + string.Join(", ", stats.UnregisteredExamples));
        }

        private static void AddLimitedExample(List<string> examples, string value)
        {
            if (examples == null || string.IsNullOrEmpty(value) || examples.Count >= 12)
                return;

            examples.Add(value);
        }

        private bool ShouldExposeExtraVehicle(
            LandVehicle vehicle,
            string source,
            string cleanName,
            string code,
            string category)
        {
            if (vehicle == null || vehicle.gameObject == null)
                return false;

            if (!string.Equals(source, "Resources", StringComparison.OrdinalIgnoreCase))
                return false;

            string text = ((cleanName ?? "") + " " + (code ?? "") + " " + (category ?? "")).ToLowerInvariant();
            if (text.Contains("wreck") || text.Contains("destroyed") || text.Contains("debris") ||
                text.Contains("burnt") || text.Contains("burned") || text.Contains("scrap"))
                return false;

            if (LooksLikeVehicleFragment(text))
                return false;

            try
            {
                if (vehicle.NetworkObject == null)
                    return false;
            }
            catch
            {
                return false;
            }

            bool hasSeat = false;
            try { hasSeat = vehicle.Seats != null && vehicle.Seats.Length > 0; } catch { }

            bool hasWheel = false;
            try { hasWheel = vehicle.wheels != null && vehicle.wheels.Count > 0; } catch { }
            if (!hasWheel)
            {
                try { hasWheel = vehicle.GetComponentInChildren<WheelCollider>(true) != null; } catch { }
            }

            if (!hasSeat && !hasWheel)
                return false;

            bool isPolice = IsPoliceVehicle(cleanName, code, category);
            if (!isPolice)
            {
                if (HasVehicleAgent(vehicle))
                    return false;

                if (HasMissingBehaviourSlots(vehicle))
                    return false;

                if (!HasRequiredOwnedVehicleReferences(vehicle))
                    return false;
            }

            return true;
        }

        private static int FindVehicleCodeIndex(List<string> codes, string vehicleCode)
        {
            if (codes == null || string.IsNullOrEmpty(vehicleCode))
                return -1;

            for (int i = 0; i < codes.Count; i++)
            {
                if (string.Equals(codes[i], vehicleCode, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private bool ShouldReplaceVehicleCandidate(
            LandVehicle existing,
            string existingSource,
            bool existingManagerSpawnable,
            LandVehicle candidate,
            string candidateSource,
            bool candidateManagerSpawnable)
        {
            int existingScore = GetVehicleCandidateScore(existing, existingSource, existingManagerSpawnable);
            int candidateScore = GetVehicleCandidateScore(candidate, candidateSource, candidateManagerSpawnable);
            return candidateScore > existingScore + 10;
        }

        private int GetVehicleCandidateScore(LandVehicle vehicle, string source, bool managerSpawnable)
        {
            if (vehicle == null || vehicle.gameObject == null)
                return int.MinValue;

            int score = 0;

            if (managerSpawnable || string.Equals(source, "VehicleManager", StringComparison.OrdinalIgnoreCase))
                score += 1000;

            try
            {
                if (!vehicle.gameObject.scene.IsValid())
                    score += 120;
                else
                    score -= 80;
            }
            catch { }

            if (HasRequiredOwnedVehicleReferences(vehicle))
                score += 140;

            if (HasVehicleAgent(vehicle))
                score -= 90;

            if (HasMissingBehaviourSlots(vehicle))
                score -= 160;

            score += Math.Min(CountUsableRenderers(vehicle), 24) * 4;
            return score;
        }

        private static bool LooksLikeVehicleFragment(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            return text.Contains("collider") ||
                text.Contains("bounds") ||
                text.Contains("trigger") ||
                text.Contains("preview") ||
                text.Contains("proxy") ||
                text.Contains("lod") ||
                text.Contains("blockout") ||
                text.Contains("blocky") ||
                text.Contains("bodyonly") ||
                text.Contains("body only") ||
                text.Contains("decorative");
        }

        private static bool HasVehicleAgent(LandVehicle vehicle)
        {
            try
            {
                return vehicle != null && vehicle.Agent != null;
            }
            catch
            {
                return true;
            }
        }

        private static bool HasRequiredOwnedVehicleReferences(LandVehicle vehicle)
        {
            if (vehicle == null || vehicle.gameObject == null)
                return false;

            try { if (vehicle.Rb == null) return false; } catch { return false; }
            try { if (vehicle.boundingBox == null) return false; } catch { return false; }

            try
            {
                if (vehicle.vehicleModel == null)
                    return false;
            }
            catch
            {
                return false;
            }

            if (CountUsableRenderers(vehicle) <= 0)
                return false;

            return true;
        }

        private static bool HasMissingBehaviourSlots(LandVehicle vehicle)
        {
            if (vehicle == null || vehicle.gameObject == null)
                return true;

            try
            {
                MonoBehaviour[] behaviours = vehicle.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours == null)
                    return false;

                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] == null)
                        return true;
                }
            }
            catch
            {
                return true;
            }

            return false;
        }

        private static int CountUsableRenderers(LandVehicle vehicle)
        {
            if (vehicle == null || vehicle.gameObject == null)
                return 0;

            int count = 0;

            try
            {
                Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
                if (renderers == null)
                    return 0;

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null)
                        continue;

                    Material[] materials = null;
                    try { materials = renderer.sharedMaterials; } catch { }

                    if (materials == null || materials.Length == 0)
                        continue;

                    for (int j = 0; j < materials.Length; j++)
                    {
                        Material material = materials[j];
                        if (material == null)
                            continue;

                        string shaderName = "";
                        try { shaderName = material.shader != null ? material.shader.name ?? "" : ""; } catch { }
                        if (shaderName.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            shaderName.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            continue;
                        }

                        count++;
                        break;
                    }
                }
            }
            catch
            {
                return 0;
            }

            return count;
        }

        private string GetVehicleCategory(string rawName, string code, string cleanName)
        {
            string text = ((rawName ?? "") + " " + (code ?? "") + " " + (cleanName ?? "")).ToLowerInvariant();

            if (text.Contains("police") || text.Contains("sheriff") || text.Contains("patrol") ||
                text.Contains("pursuit") || text.Contains("swat") || text.Contains("law") ||
                text.Contains("roadblock"))
                return "Police";

            if (text.Contains("tractor") || text.Contains("forklift") || text.Contains("farm") ||
                text.Contains("utility") || text.Contains("work"))
                return "Work";

            if (text.Contains("delivery") || text.Contains("truck") || text.Contains("box") ||
                text.Contains("van") || text.Contains("trailer"))
                return "Utility";

            if (text.Contains("npc") || text.Contains("ai"))
                return "NPC";

            return "Standard";
        }

        /// <summary>
        /// Clears the vehicle cache
        /// </>
        public void ClearCache()
        {
            _vehicleCodes = new string[0];
            _vehicleNames = new string[0];
            _vehicleRisky = new bool[0];
            _vehicleSources = new string[0];
            _vehicleCategories = new string[0];
            _vehiclePrefabs = new LandVehicle[0];
            _vehicleCount = 0;
            _isCached = false;
            _selectedIndex = 0;
            _nextVehicleCacheRetryTime = 0f;
            _reportedMissingVehicleManager = false;
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

        public bool IsVehiclePoliceAt(int index)
        {
            if (index < 0 || index >= _vehicleCount)
                return false;

            return IsPoliceVehicle(
                GetVehicleNameAt(index),
                GetVehicleCodeAt(index),
                GetVehicleCategoryAt(index));
        }

        public bool ShouldWarnForVehicleAt(int index)
        {
            return IsVehicleRiskyAt(index) && !IsVehiclePoliceAt(index);
        }

        public string GetVehicleSourceAt(int index)
        {
            if (index < 0 || index >= _vehicleCount || index >= _vehicleSources.Length)
                return "";

            return _vehicleSources[index] ?? "";
        }

        public string GetVehicleCategoryAt(int index)
        {
            if (index < 0 || index >= _vehicleCount || index >= _vehicleCategories.Length)
                return "";

            return _vehicleCategories[index] ?? "";
        }

        private LandVehicle GetVehiclePrefabAt(int index)
        {
            if (index < 0 || index >= _vehicleCount || index >= _vehiclePrefabs.Length)
                return null;

            return _vehiclePrefabs[index];
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

        public string GetSelectedVehicleSource()
        {
            return GetVehicleSourceAt(_selectedIndex);
        }

        public string GetSelectedVehicleCategory()
        {
            return GetVehicleCategoryAt(_selectedIndex);
        }

        public bool IsSelectedVehicleRisky()
        {
            return IsVehicleRiskyAt(_selectedIndex);
        }

        public bool IsSelectedVehiclePolice()
        {
            return IsVehiclePoliceAt(_selectedIndex);
        }

        public bool ShouldWarnForSelectedVehicle()
        {
            return ShouldWarnForVehicleAt(_selectedIndex);
        }

        public bool CanSpawnVehicles()
        {
            var lobbyService = LobbyService.Instance;
            return !lobbyService.IsInLobby() || lobbyService.IsHost();
        }

        public bool HasDrivenTunableVehicle()
        {
            return GetDrivenTunableVehicle() != null;
        }

        public VehicleTuneSettings GetDrivenVehicleTune()
        {
            LandVehicle vehicle = GetDrivenTunableVehicle();
            if (vehicle == null)
                return null;

            return GetOrCreateVehicleTune(vehicle);
        }

        public string GetDrivenVehicleTuneLabel()
        {
            LandVehicle vehicle = GetDrivenTunableVehicle();
            if (vehicle == null)
                return "No driven vehicle";

            string name = CleanVehicleName(SafeVehicleName(vehicle), SafeVehicleCode(vehicle));
            string key = GetVehicleTuneKey(vehicle);
            if (string.IsNullOrEmpty(key))
                return name;

            int marker = key.LastIndexOf(':');
            string shortKey = marker >= 0 && marker < key.Length - 1 ? key.Substring(marker + 1) : key;
            if (shortKey.Length > 8)
                shortKey = shortKey.Substring(0, 8);

            return name + "  #" + shortKey;
        }

        public string GetDrivenVehicleBodyColorLabel()
        {
            VehicleTuneSettings tune = GetDrivenVehicleTune();
            if (tune == null)
                return "None";

            return tune.BodyColor.ToString();
        }

        public void CycleDrivenVehicleBodyColor(int direction)
        {
            VehicleTuneSettings tune = GetDrivenVehicleTune();
            if (tune == null)
                return;

            if (TunableBodyColors.Length == 0)
                return;

            int current = Array.IndexOf(TunableBodyColors, tune.BodyColor);
            if (current < 0)
                current = 0;

            int next = (current + direction) % TunableBodyColors.Length;
            if (next < 0)
                next += TunableBodyColors.Length;

            tune.BodyColor = TunableBodyColors[next];
            tune.BodyColorEnabled = true;
            tune.HasAppliedBodyColor = false;
            ApplyDrivenVehicleTune();
        }

        public void ApplyDrivenVehicleTune()
        {
            LandVehicle vehicle = GetDrivenTunableVehicle();
            if (vehicle == null)
                return;

            ApplyVehicleTune(vehicle, GetOrCreateVehicleTune(vehicle));
        }

        public void ResetDrivenVehicleTune()
        {
            LandVehicle vehicle = GetDrivenTunableVehicle();
            if (vehicle == null)
                return;

            VehicleTuneSettings tune = GetOrCreateVehicleTune(vehicle);
            tune.Reset();
            string key = GetVehicleTuneKey(vehicle);
            VehicleTuneBaseline baseline = string.IsNullOrEmpty(key) ? null : GetOrCreateVehicleBaseline(vehicle, key);
            if (baseline != null)
            {
                ApplyBaselineColorsToTune(tune, baseline);
                if (baseline.HasBodyColor)
                {
                    tune.BodyColorEnabled = true;
                    tune.HasAppliedBodyColor = false;
                }
            }

            ApplyVehicleTune(vehicle, tune);
            tune.BodyColorEnabled = false;
            NotificationService.Instance.Status("Vehicle tuning reset");
        }

        public string BlowUpRV()
        {
            if (!CanSpawnVehicles())
            {
                NotificationService.Instance.Warning("RV controls are host-only in multiplayer");
                return null;
            }

            try
            {
                RV storyRV = FindStoryRV();
                if (storyRV != null)
                {
                    PrepareStoryRVForBlowUp(storyRV);
                    storyRV.BlowUp();
                    NotificationService.Instance.Notify("Story RV blown up.");
                    return "Story RV blown up";
                }

                NotificationService.Instance.Warning("Story RV not found in the current scene");
                return null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to destroy RV: " + ex);
                NotificationService.Instance.Warning("Failed to destroy RV");
                return null;
            }
        }

        public string FixOrRespawnRV()
        {
            if (!CanSpawnVehicles())
            {
                NotificationService.Instance.Warning("RV controls are host-only in multiplayer");
                return null;
            }

            try
            {
                RV storyRV = FindStoryRV();
                if (storyRV != null)
                {
                    RepairStoryRV(storyRV);
                    NotificationService.Instance.Notify("Story RV repaired.");
                    return "Story RV repaired";
                }

                NotificationService.Instance.Warning("Story RV not found in the current scene");
                return null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to fix/respawn RV: " + ex);
                NotificationService.Instance.Warning("Failed to fix/respawn RV");
                return null;
            }
        }

        public void SetBenzieManorAccess(bool enabled)
        {
            if (enabled && !CanSpawnVehicles())
            {
                NotificationService.Instance.Warning("Benzie Manor access is host-only in multiplayer");
                return;
            }

            _benzieManorAccessEnabled = enabled;
            if (!enabled)
            {
                NotificationService.Instance.Status("Benzie Manor access: Off");
                return;
            }

            if (EnsureBenzieManorAccess(true))
                NotificationService.Instance.Status("Benzie Manor access: On");
        }

        public string GetSelectedVehicleRiskWarning()
        {
            if (!ShouldWarnForSelectedVehicle())
                return "";

            return "Warning: special vehicle. Nugzz will spawn it as a custom owned vehicle and strip AI-only behavior.";
        }

        /// <summary>
        /// Attempts to spawn the currently selected vehicle
        /// </summary>
        /// <returns>The name of the spawned vehicle on success, null on failure</returns>
        public string SpawnSelectedVehicle()
        {
            if (!CanSpawnVehicles())
            {
                NotificationService.Instance.Warning("Vehicle spawning is host-only in multiplayer");
                return null;
            }

            if (_vehicleCount == 0)
            {
                UnityEngine.Debug.LogError("[Nugzz] No vehicles cached");
                return null;
            }

            string vehicleCode = GetSelectedVehicleCode();
            string vehicleName = GetSelectedVehicleName();
            bool riskyVehicle = IsSelectedVehicleRisky();
            string vehicleSource = GetSelectedVehicleSource();
            string vehicleCategory = GetSelectedVehicleCategory();

            if (string.IsNullOrEmpty(vehicleCode))
            {
                UnityEngine.Debug.LogError("[Nugzz] Selected vehicle code is null or empty");
                return null;
            }

            int attemptId = ++_spawnAttemptId;

            try
            {
                bool shouldWarn = riskyVehicle && !IsPoliceVehicle(vehicleName, vehicleCode, vehicleCategory);
                if (shouldWarn)
                {
                    string warning = GetSelectedVehicleRiskWarning();
                    NotificationService.Instance.Warning(warning);
                    UnityEngine.Debug.LogWarning("[Nugzz] Vehicle spawn #" + attemptId + " special vehicle requested: " + vehicleName + " (" + vehicleCode + ") - " + warning);
                }

                var player = GetLocalPlayer();
                if (player == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] Vehicle spawn #" + attemptId + " failed: no local player found");
                    return null;
                }

                // Use player orientation so camera pitch cannot affect vehicle placement.
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
                bool inLobby = false;
                bool isHost = true;
                try
                {
                    var lobbyService = LobbyService.Instance;
                    inLobby = lobbyService.IsInLobby();
                    isHost = !inLobby || lobbyService.IsHost();
                    if (inLobby)
                    {
                        // Only host spawns networked vehicles that persist for all players.
                        spawnAsPlayerOwned = isHost;
                    }
                }
                catch { /* fallback to local assumption */ }

                DebugLogService.Instance.Verbose(
                    "Vehicle spawn #" + attemptId +
                    " begin name='" + vehicleName +
                    "' code='" + vehicleCode +
                    "' source=" + vehicleSource +
                    " category=" + vehicleCategory +
                    " lobby=" + inLobby +
                    " host=" + isHost +
                    " owned=" + spawnAsPlayerOwned +
                    " playerPos=" + FormatVector(playerTransform.position) +
                    " fwd=" + FormatVector(fwdXZ));

                bool groundSnapped = false;
                bool spawnAreaOccupied = false;

                // Snap to ground
                if (Physics.Raycast(
                    spawnPosition + Vector3.up * 25f,
                    Vector3.down,
                    out RaycastHit groundHit,
                    50f,
                    allLayers))
                {
                    spawnPosition = groundHit.point + new Vector3(0f, 0.3f, 0f);
                    groundSnapped = true;
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
                    spawnAreaOccupied = true;
                    UnityEngine.Debug.LogWarning("[Nugzz] Vehicle spawn #" + attemptId + " area occupied, pushing back 2 m");
                    Vector3 altPos = playerTransform.position + fwdXZ * (spawnDist + 2f) + new Vector3(0f, clearanceHeight, 0f);

                    if (Physics.Raycast(
                        altPos + Vector3.up * 25f,
                        Vector3.down,
                        out RaycastHit groundHit2,
                        50f,
                        allLayers))
                    {
                        spawnPosition = groundHit2.point + new Vector3(0f, 0.3f, 0f);
                        groundSnapped = true;
                    }
                    else
                    {
                        spawnPosition = altPos;
                    }
                }

                string cleanName = CleanVehicleName(vehicleName, vehicleCode);

                // Validate prefab exists before touching the network layer.
                // Extra resource vehicles are not registered with VehicleManager, so keep
                // their discovered prefab and route them through the custom spawn path.
                LandVehicle managerPrefab = GetVehiclePrefab(vehicleCode);
                LandVehicle cachedPrefab = GetVehiclePrefabAt(_selectedIndex);
                LandVehicle prefab = managerPrefab != null ? managerPrefab : cachedPrefab;
                if (prefab == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] Vehicle spawn #" + attemptId + " failed: prefab not found for code " + vehicleCode);
                    return null;
                }

                if (managerPrefab == null)
                    riskyVehicle = true;

                Quaternion rotation = Quaternion.Euler(0f, playerTransform.eulerAngles.y, 0f);

                UnityEngine.Debug.Log(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    ": '" + cleanName +
                    "' code='" + vehicleCode +
                    "' source=" + vehicleSource +
                    " category=" + vehicleCategory +
                    " pos=" + FormatVector(spawnPosition) +
                    " owned=" + spawnAsPlayerOwned +
                    " special=" + riskyVehicle);

                DebugLogService.Instance.Verbose(
                    "Vehicle spawn #" + attemptId +
                    " placement groundSnapped=" + groundSnapped +
                    " areaOccupied=" + spawnAreaOccupied +
                    " rotY=" + rotation.eulerAngles.y +
                    " prefab=" + DescribeVehicle(prefab));

                if (riskyVehicle)
                {
                    if (IsPoliceVehicle(vehicleName, vehicleCode, vehicleCategory))
                    {
                        string policeResult = SpawnPoliceStationVehicle(
                            attemptId,
                            cleanName,
                            vehicleName,
                            vehicleCode,
                            spawnPosition,
                            rotation,
                            spawnAsPlayerOwned);

                        if (policeResult != null)
                            return policeResult;

                        NotificationService.Instance.Warning("Police vehicle spawn failed safely; check Nugzz log");
                        return null;
                    }

                    string result = SpawnCustomSpecialVehicle(
                        attemptId,
                        prefab,
                        cleanName,
                        vehicleName,
                        vehicleCode,
                        vehicleCategory,
                        spawnPosition,
                        rotation,
                        spawnAsPlayerOwned);

                    if (result != null)
                        return result;

                    return null;
                }

                LandVehicle spawned = SpawnAndReturnVehicle(vehicleCode, spawnPosition, rotation, spawnAsPlayerOwned);
                if (spawned != null)
                {
                    VehicleCollisionService.Instance.ApplyVehicle(spawned);

                    try
                    {
                        if (spawnAsPlayerOwned)
                            spawned.SetIsPlayerOwned(null, true);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning($"[Nugzz] Failed to mark vehicle as drivable: {e.Message}");
                    }

                    UnityEngine.Debug.Log("[Nugzz] Vehicle spawn #" + attemptId + " success: " + DescribeVehicle(spawned));
                    return cleanName;
                }

                UnityEngine.Debug.LogWarning("[Nugzz] Vehicle spawn #" + attemptId + " SpawnAndReturnVehicle returned null, trying fire-and-forget");
                SpawnVehicle(vehicleCode, spawnPosition, rotation, spawnAsPlayerOwned);
                UnityEngine.Debug.Log("[Nugzz] Vehicle spawn #" + attemptId + " fire-and-forget spawn issued for " + cleanName);
                return cleanName;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Vehicle spawn #" + attemptId + " failed: " + ex);
                return null;
            }
        }

        private bool TryGetForwardSpawn(out Vector3 spawnPosition, out Quaternion rotation)
        {
            spawnPosition = Vector3.zero;
            rotation = Quaternion.identity;

            var player = GetLocalPlayer();
            if (player == null)
            {
                UnityEngine.Debug.LogError("[Nugzz] No local player found");
                NotificationService.Instance.Warning("No local player found");
                return false;
            }

            var playerTransform = player.transform;
            Vector3 fwdXZ = new Vector3(playerTransform.forward.x, 0f, playerTransform.forward.z);
            if (fwdXZ.sqrMagnitude < 0.001f)
                fwdXZ = Vector3.forward;
            else
                fwdXZ.Normalize();

            rotation = Quaternion.Euler(0f, playerTransform.eulerAngles.y, 0f);
            spawnPosition = playerTransform.position + fwdXZ * 5f + Vector3.up * 2.5f;

            if (Physics.Raycast(spawnPosition + Vector3.up * 25f, Vector3.down, out RaycastHit groundHit, 60f, ~0))
                spawnPosition = groundHit.point + Vector3.up * 0.35f;
            else
                spawnPosition.y = playerTransform.position.y + 1f;

            return true;
        }

        private string SpawnPoliceStationVehicle(
            int attemptId,
            string cleanName,
            string vehicleName,
            string vehicleCode,
            Vector3 position,
            Quaternion rotation,
            bool playerOwned)
        {
            PoliceStation station = FindBestPoliceStation(position);
            if (station == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " no PoliceStation found; falling back to custom prefab spawn");
                return null;
            }

            LandVehicle[] originalPrefabs = null;
            bool narrowedPrefabs = false;
            try
            {
                LandVehicle stationPrefab = SelectStationPolicePrefab(station, vehicleName, vehicleCode);
                int stationPrefabCount = 0;
                try { stationPrefabCount = station.PoliceVehiclePrefabs?.Length ?? 0; } catch { }

                if (stationPrefab != null)
                {
                    originalPrefabs = station.PoliceVehiclePrefabs;
                    station.PoliceVehiclePrefabs = new[] { stationPrefab };
                    narrowedPrefabs = true;
                    UnityEngine.Debug.Log(
                        "[Nugzz] Vehicle spawn #" + attemptId +
                        " police station prefab pick: selected='" + SafeVehicleName(stationPrefab) +
                        "' code='" + SafeVehicleCode(stationPrefab) +
                        "' stationPrefabs=" + stationPrefabCount);
                }
                else
                {
                    UnityEngine.Debug.Log(
                        "[Nugzz] Vehicle spawn #" + attemptId +
                        " police station prefab pick: using vanilla default selection, stationPrefabs=" +
                        stationPrefabCount);
                }
            }
            catch { }

            try
            {
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " spawning police vehicle through PoliceStation.CreateVehicle: '" +
                    cleanName + "' code='" + vehicleCode + "'");

                LandVehicle vehicle = station.CreateVehicle();
                if (vehicle == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "[Nugzz] Vehicle spawn #" + attemptId +
                        " PoliceStation.CreateVehicle returned null; falling back to custom prefab spawn");
                    return null;
                }

                try { vehicle.transform.SetPositionAndRotation(position, rotation); } catch { }
                try { vehicle.SetTransform(position, rotation); } catch { }
                try { vehicle.SetTransform_Server(position, rotation); } catch { }

                LogPoliceVehicleVisualSummary(vehicle, attemptId, "spawned");
                ConvertAiPoliceVehicleToPlayerVehicle(vehicle, playerOwned, attemptId);
                RestorePoliceStationVehicleVisuals(vehicle, attemptId);
                LogPoliceVehicleVisualSummary(vehicle, attemptId, "after conversion");
                VehicleCollisionService.Instance.ApplyVehicle(vehicle);

                UnityEngine.Debug.Log(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " police station spawn success: " + DescribeVehicle(vehicle));
                return cleanName;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " police station spawn failed: " + ex);
                return null;
            }
            finally
            {
                if (narrowedPrefabs)
                {
                    try { station.PoliceVehiclePrefabs = originalPrefabs; } catch { }
                }
            }
        }

        private LandVehicle SelectStationPolicePrefab(PoliceStation station, string requestedName, string requestedCode)
        {
            try
            {
                LandVehicle[] prefabs = station?.PoliceVehiclePrefabs;
                if (prefabs == null || prefabs.Length == 0)
                    return null;

                string wanted = ((requestedName ?? "") + " " + (requestedCode ?? "")).ToLowerInvariant();
                bool wantsSuv = wanted.Contains("suv") || wanted.Contains("truck") || wanted.Contains("van");
                bool wantsCar = wanted.Contains("car") || wanted.Contains("sedan") || wanted.Contains("cruiser");

                LandVehicle best = null;
                int bestScore = int.MinValue;
                for (int i = 0; i < prefabs.Length; i++)
                {
                    LandVehicle prefab = prefabs[i];
                    if (prefab == null)
                        continue;

                    string code = SafeVehicleCode(prefab);
                    string name = CleanVehicleName(SafeVehicleName(prefab), code);
                    string text = (name + " " + code).ToLowerInvariant();

                    int score = 0;
                    if (!string.IsNullOrEmpty(requestedCode) &&
                        string.Equals(code, requestedCode, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 1000;
                    }

                    if (!string.IsNullOrEmpty(requestedName) &&
                        text.Contains(requestedName.ToLowerInvariant()))
                    {
                        score += 200;
                    }

                    if (wantsSuv && (text.Contains("suv") || text.Contains("truck") || text.Contains("van")))
                        score += 100;
                    if (wantsCar && (text.Contains("car") || text.Contains("sedan") || text.Contains("cruiser")))
                        score += 80;
                    if (text.Contains("police") || text.Contains("sheriff") || text.Contains("patrol"))
                        score += 20;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = prefab;
                    }
                }

                return bestScore > 0 ? best : null;
            }
            catch
            {
                return null;
            }
        }

        private PoliceStation FindBestPoliceStation(Vector3 position)
        {
            try
            {
                PoliceStation closest = PoliceStation.GetClosestPoliceStation(position);
                if (closest != null)
                    return closest;
            }
            catch { }

            try
            {
                var stations = PoliceStation.PoliceStations;
                if (stations != null && stations.Count > 0)
                    return stations[0];
            }
            catch { }

            try
            {
                var stations = FindObjectsOfType<PoliceStation>(true);
                if (stations != null && stations.Length > 0)
                    return stations[0];
            }
            catch { }

            return null;
        }

        private void ConvertAiPoliceVehicleToPlayerVehicle(LandVehicle vehicle, bool playerOwned, int attemptId)
        {
            if (vehicle == null)
                return;

            try { vehicle.SpawnAsPlayerOwned = playerOwned; } catch { }
            try { vehicle.IsOccupied = false; } catch { }
            try { vehicle.SetVisible(true); } catch { }
            try { vehicle.SetIsPlayerOwned(null, playerOwned); } catch { }

            try
            {
                if (vehicle.Agent != null)
                {
                    vehicle.Agent.StopNavigating();
                    vehicle.Agent.PursuitModeEnabled = false;
                    vehicle.Agent.enabled = false;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " failed to disable police vehicle AI: " + ex.Message);
            }

            try
            {
                var manager = FindObjectOfType<VehicleManager>();
                if (manager?.PlayerOwnedVehicles != null && playerOwned && !manager.PlayerOwnedVehicles.Contains(vehicle))
                    manager.PlayerOwnedVehicles.Add(vehicle);
            }
            catch { }

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
        }

        private void RestorePoliceStationVehicleVisuals(LandVehicle vehicle, int attemptId)
        {
            if (vehicle == null)
                return;

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
                    vehicle.Color.gameObject.SetActive(true);
                    vehicle.Color.enabled = true;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Vehicle spawn #" + attemptId +
                    " police visual restore warning: " + ex.Message);
            }
        }

        private void LogPoliceVehicleVisualSummary(LandVehicle vehicle, int attemptId, string stage)
        {
            if (vehicle == null)
                return;

            try
            {
                Renderer[] renderers = vehicle.GetComponentsInChildren<Renderer>(true);
                int rendererCount = renderers?.Length ?? 0;
                int enabledCount = 0;
                int activeCount = 0;
                int materialCount = 0;
                int nullMaterials = 0;
                int errorShaders = 0;
                var examples = new List<string>();

                if (renderers != null)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        Renderer renderer = renderers[i];
                        if (renderer == null)
                            continue;

                        try { if (renderer.enabled) enabledCount++; } catch { }
                        try { if (renderer.gameObject != null && renderer.gameObject.activeInHierarchy) activeCount++; } catch { }

                        try
                        {
                            Material[] materials = renderer.sharedMaterials;
                            if (materials == null || materials.Length == 0)
                            {
                                nullMaterials++;
                                continue;
                            }

                            for (int j = 0; j < materials.Length; j++)
                            {
                                materialCount++;
                                Material material = materials[j];
                                if (material == null)
                                {
                                    nullMaterials++;
                                    continue;
                                }

                                string shaderName = "";
                                try { shaderName = material.shader != null ? material.shader.name ?? "" : ""; } catch { }
                                if (shaderName.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    shaderName.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    errorShaders++;
                                }

                                if (examples.Count < 5)
                                {
                                    string rendererName = "renderer";
                                    try { rendererName = renderer.name ?? rendererName; } catch { }
                                    examples.Add(rendererName + ":" + SafeMaterialName(material) + "/" + shaderName);
                                }
                            }
                        }
                        catch { }
                    }
                }

                UnityEngine.Debug.Log(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " police visuals " + stage +
                    ": vehicle='" + SafeVehicleName(vehicle) +
                    "' renderers=" + rendererCount +
                    " enabled=" + enabledCount +
                    " active=" + activeCount +
                    " materials=" + materialCount +
                    " nullMats=" + nullMaterials +
                    " errorShaders=" + errorShaders +
                    " examples=" + string.Join(", ", examples));
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Vehicle spawn #" + attemptId +
                    " police visual summary failed: " + ex.Message);
            }
        }

        private void TogglePoliceSirenForCurrentVehicle()
        {
            LandVehicle vehicle = GetLocalDrivenVehicle();
            if (vehicle == null)
                return;

            try
            {
                List<PoliceLight> policeLights = FindPoliceLights(vehicle);
                if (policeLights.Count == 0)
                {
                    ReportMissingPoliceLightbar(vehicle);
                    return;
                }

                _policeSirenOn = !IsAnyPoliceLightOn(policeLights);
                _lastPoliceSirenVehicle = vehicle;

                int changed = 0;
                for (int i = 0; i < policeLights.Count; i++)
                {
                    PoliceLight policeLight = policeLights[i];
                    if (policeLight == null)
                        continue;

                    try
                    {
                        EnsureHierarchyActive(policeLight.transform, vehicle.transform);
                        policeLight.enabled = true;
                        if (policeLight.IsOn != _policeSirenOn)
                        {
                            policeLight.SetIsOn(_policeSirenOn);
                            changed++;
                        }

                        ApplyPoliceSirenAudio(policeLight, _policeSirenOn);
                    }
                    catch (Exception ex)
                    {
                        LogPoliceSirenFailureOnce("Police siren toggle failed on " +
                            SafeVehicleName(vehicle) + ": " + ex.Message);
                    }
                }

                SuppressPoliceHeadlights(vehicle);

                if (changed > 0)
                {
                    NotificationService.Instance.Status("Police siren: " + (_policeSirenOn ? "On" : "Off"));
                    DebugLogService.Instance.Verbose(
                        "Police siren " + (_policeSirenOn ? "enabled" : "disabled") +
                        " on " + SafeVehicleName(vehicle) +
                        " lights=" + policeLights.Count);
                }
            }
            catch (Exception ex)
            {
                LogPoliceSirenFailureOnce("Police siren toggle failed: " + ex.Message);
            }
        }

        private void MaintainPoliceSirenState()
        {
            if (_lastPoliceSirenVehicle == null)
                return;

            try
            {
                List<PoliceLight> policeLights = FindPoliceLights(_lastPoliceSirenVehicle);
                if (policeLights.Count == 0)
                    return;

                for (int i = 0; i < policeLights.Count; i++)
                {
                    PoliceLight policeLight = policeLights[i];
                    if (policeLight == null)
                        continue;

                    try
                    {
                        if (policeLight.IsOn != _policeSirenOn)
                            policeLight.SetIsOn(_policeSirenOn);
                        ApplyPoliceSirenAudio(policeLight, _policeSirenOn);
                    }
                    catch { }
                }

                SuppressPoliceHeadlights(_lastPoliceSirenVehicle);
            }
            catch { }
        }

        private LandVehicle GetLocalDrivenVehicle()
        {
            try
            {
                var manager = ManagerCacheService.Instance.VehicleManager ?? FindObjectOfType<VehicleManager>();
                var vehicles = manager?.AllVehicles;
                if (vehicles != null)
                {
                    for (int i = 0; i < vehicles.Count; i++)
                    {
                        LandVehicle vehicle = vehicles[i];
                        if (vehicle == null)
                            continue;

                        try
                        {
                            if (vehicle.LocalPlayerIsDriver)
                                return vehicle;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                LandVehicle[] vehicles = FindObjectsOfType<LandVehicle>(true);
                if (vehicles != null)
                {
                    for (int i = 0; i < vehicles.Length; i++)
                    {
                        LandVehicle vehicle = vehicles[i];
                        if (vehicle == null)
                            continue;

                        try
                        {
                            if (vehicle.LocalPlayerIsDriver)
                                return vehicle;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                var seat = player?.CurrentVehicleSeat;
                if (seat == null || !seat.isDriverSeat)
                    return null;

                return seat.GetComponentInParent<LandVehicle>(true);
            }
            catch (Exception ex)
            {
                LogPoliceSirenFailureOnce("Failed to read current vehicle for police siren: " + ex.Message);
                return null;
            }
        }

        private LandVehicle GetDrivenTunableVehicle()
        {
            if (Time.unscaledTime < _nextDrivenVehicleLookupTime)
            {
                if (_cachedDrivenVehicle == null || IsDrivenTunableVehicleValid(_cachedDrivenVehicle))
                    return _cachedDrivenVehicle;
            }

            _cachedDrivenVehicle = GetLocalDrivenVehicle();
            if (!IsDrivenTunableVehicleValid(_cachedDrivenVehicle))
                _cachedDrivenVehicle = null;

            _nextDrivenVehicleLookupTime = Time.unscaledTime + 0.15f;
            return _cachedDrivenVehicle;
        }

        private static bool IsDrivenTunableVehicleValid(LandVehicle vehicle)
        {
            if (vehicle == null)
                return false;

            try
            {
                if (vehicle.gameObject == null || !vehicle.gameObject.activeInHierarchy)
                    return false;
            }
            catch { }

            try
            {
                if (vehicle.LocalPlayerIsDriver)
                    return true;
            }
            catch { }

            try
            {
                Player player = ManagerCacheService.Instance.LocalPlayer;
                VehicleSeat seat = player?.CurrentVehicleSeat;
                return seat != null && seat.isDriverSeat && seat.GetComponentInParent<LandVehicle>(true) == vehicle;
            }
            catch
            {
                return false;
            }
        }

        private VehicleTuneSettings GetOrCreateVehicleTune(LandVehicle vehicle)
        {
            string key = GetVehicleTuneKey(vehicle);
            if (string.IsNullOrEmpty(key))
                key = "vehicle:" + _vehicleTunes.Count;

            VehicleTuneSettings tune;
            if (_vehicleTunes.TryGetValue(key, out tune))
                return tune;

            tune = new VehicleTuneSettings();
            VehicleTuneBaseline baseline = GetOrCreateVehicleBaseline(vehicle, key);
            ApplyBaselineColorsToTune(tune, baseline);

            if (!baseline.HasBodyColor)
            {
                try
                {
                    tune.BodyColor = vehicle.OwnedColor;
                    tune.LastAppliedBodyColor = tune.BodyColor;
                }
                catch { }
            }

            _vehicleTunes[key] = tune;
            return tune;
        }

        private static void ApplyBaselineColorsToTune(VehicleTuneSettings tune, VehicleTuneBaseline baseline)
        {
            if (tune == null || baseline == null)
                return;

            if (baseline.HasBodyColor)
            {
                tune.BodyColor = baseline.BodyColor;
                tune.LastAppliedBodyColor = baseline.BodyColor;
            }

            foreach (LightBaseline lightBaseline in baseline.LightBaselines.Values)
            {
                tune.HeadlightRed = Mathf.Clamp01(lightBaseline.Color.r);
                tune.HeadlightGreen = Mathf.Clamp01(lightBaseline.Color.g);
                tune.HeadlightBlue = Mathf.Clamp01(lightBaseline.Color.b);
                return;
            }
        }

        private void ApplyVehicleTunes()
        {
            if (_vehicleTunes.Count == 0)
                return;

            LandVehicle driven = GetDrivenTunableVehicle();
            if (driven != null)
                ApplyVehicleTune(driven, GetOrCreateVehicleTune(driven));

            try
            {
                var manager = ManagerCacheService.Instance.VehicleManager ?? FindObjectOfType<VehicleManager>();
                var vehicles = manager?.AllVehicles;
                if (vehicles == null)
                    return;

                for (int i = 0; i < vehicles.Count; i++)
                {
                    LandVehicle vehicle = vehicles[i];
                    if (vehicle == null || vehicle == driven)
                        continue;

                    string key = GetVehicleTuneKey(vehicle);
                    VehicleTuneSettings tune;
                    if (!string.IsNullOrEmpty(key) && _vehicleTunes.TryGetValue(key, out tune))
                        ApplyVehicleTune(vehicle, tune);
                }
            }
            catch { }
        }

        private void ApplyVehicleTune(LandVehicle vehicle, VehicleTuneSettings tune)
        {
            if (vehicle == null || tune == null)
                return;

            try
            {
                string key = GetVehicleTuneKey(vehicle);
                if (string.IsNullOrEmpty(key))
                    return;

                VehicleTuneBaseline baseline = GetOrCreateVehicleBaseline(vehicle, key);
                float speed = Mathf.Clamp(tune.SpeedMultiplier, 0.1f, 10f);
                float steering = Mathf.Clamp(tune.SteeringMultiplier, 0.1f, 5f);
                float brake = Mathf.Clamp(tune.BrakeMultiplier, 0.1f, 8f);
                float brakeHardness = Mathf.Clamp(tune.BrakeHardnessMultiplier, 0.1f, 8f);
                float traction = Mathf.Clamp(tune.TractionMultiplier, 0.1f, 6f);

                if (baseline.TopSpeed > 0f)
                    vehicle.TopSpeed = baseline.TopSpeed * speed;

                if (baseline.MotorTorque != null)
                    SetPrivateObject(vehicle, "motorTorque", ScaleAnimationCurve(baseline.MotorTorque, Mathf.Lerp(0.25f, speed, 0.9f)));

                if (baseline.DiffGearing > 0f)
                    SetPrivateFloat(vehicle, "diffGearing", baseline.DiffGearing * Mathf.Lerp(0.5f, speed, 0.75f));

                if (baseline.ReverseMultiplier > 0f)
                    SetPrivateFloat(vehicle, "reverseMultiplier", baseline.ReverseMultiplier * Mathf.Clamp(speed, 0.25f, 4f));

                if (baseline.Downforce > 0f)
                    SetPrivateFloat(vehicle, "downforce", baseline.Downforce * Mathf.Clamp(speed, 0.5f, 3f));

                float steerAngle = baseline.MaxSteeringAngle > 0f ? baseline.MaxSteeringAngle * steering : 0f;
                if (steerAngle > 0f)
                    vehicle.OverrideMaxSteerAngle(steerAngle);

                if (baseline.SteerRate > 0f)
                    SetPrivateFloat(vehicle, "steerRate", baseline.SteerRate * steering);

                if (baseline.BrakeForceMultiplier > 0f)
                    SetPrivateFloat(vehicle, "BrakeForceMultiplier", baseline.BrakeForceMultiplier * brake);

                if (baseline.BrakeForce != null)
                    SetPrivateObject(vehicle, "brakeForce", ScaleAnimationCurve(baseline.BrakeForce, brake));

                if (baseline.HandBrakeForce > 0f)
                    SetPrivateFloat(vehicle, "handBrakeForce", baseline.HandBrakeForce * brakeHardness);

                ApplyWheelTraction(vehicle, baseline, traction);
                ApplyHeadlightTune(vehicle, baseline, tune);
                ApplyBodyColorTune(vehicle, baseline, tune);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Vehicle tuning failed: " + ex.Message);
            }
        }

        private VehicleTuneBaseline GetOrCreateVehicleBaseline(LandVehicle vehicle, string key)
        {
            VehicleTuneBaseline baseline;
            if (_vehicleTuneBaselines.TryGetValue(key, out baseline))
                return baseline;

            baseline = new VehicleTuneBaseline();
            try { baseline.TopSpeed = vehicle.TopSpeed; } catch { baseline.TopSpeed = 0f; }
            baseline.MaxSteeringAngle = GetPrivateFloat(vehicle, "maxSteeringAngle", 35f);
            baseline.SteerRate = GetPrivateFloat(vehicle, "steerRate", 1f);
            baseline.BrakeForceMultiplier = GetPrivateFloat(vehicle, "BrakeForceMultiplier", 1f);
            baseline.HandBrakeForce = GetPrivateFloat(vehicle, "handBrakeForce", 1f);
            baseline.DiffGearing = GetPrivateFloat(vehicle, "diffGearing", 1f);
            baseline.Downforce = GetPrivateFloat(vehicle, "downforce", 1f);
            baseline.ReverseMultiplier = GetPrivateFloat(vehicle, "reverseMultiplier", 1f);
            baseline.MotorTorque = CloneAnimationCurve(GetPrivateObject<AnimationCurve>(vehicle, "motorTorque"));
            baseline.BrakeForce = CloneAnimationCurve(GetPrivateObject<AnimationCurve>(vehicle, "brakeForce"));

            if (baseline.MaxSteeringAngle <= 0f)
                baseline.MaxSteeringAngle = 35f;
            if (baseline.SteerRate <= 0f)
                baseline.SteerRate = 1f;
            if (baseline.BrakeForceMultiplier <= 0f)
                baseline.BrakeForceMultiplier = 1f;
            if (baseline.HandBrakeForce <= 0f)
                baseline.HandBrakeForce = 1f;
            if (baseline.DiffGearing <= 0f)
                baseline.DiffGearing = 1f;
            if (baseline.Downforce <= 0f)
                baseline.Downforce = 1f;
            if (baseline.ReverseMultiplier <= 0f)
                baseline.ReverseMultiplier = 1f;

            try
            {
                baseline.BodyColor = vehicle.OwnedColor;
                baseline.HasBodyColor = true;
            }
            catch { }

            CaptureWheelBaselines(vehicle, baseline);
            CaptureHeadlightBaselines(vehicle, baseline);
            _vehicleTuneBaselines[key] = baseline;
            return baseline;
        }

        private string GetVehicleTuneKey(LandVehicle vehicle)
        {
            if (vehicle == null)
                return "";

            try
            {
                string guid = vehicle.GUID.ToString();
                if (!string.IsNullOrEmpty(guid) && guid != "00000000-0000-0000-0000-000000000000")
                    return "guid:" + guid.Replace("-", "");
            }
            catch { }

            try
            {
                return "instance:" + SafeVehicleCode(vehicle) + ":" + vehicle.GetInstanceID();
            }
            catch
            {
                return "name:" + SafeVehicleName(vehicle);
            }
        }

        private void CaptureWheelBaselines(LandVehicle vehicle, VehicleTuneBaseline baseline)
        {
            List<WheelCollider> wheels = GetWheelColliders(vehicle);
            for (int i = 0; i < wheels.Count; i++)
            {
                WheelCollider wheel = wheels[i];
                if (wheel == null)
                    continue;

                try
                {
                    int id = wheel.GetInstanceID();
                    if (baseline.WheelBaselines.ContainsKey(id))
                        continue;

                    WheelFrictionCurve forward = wheel.forwardFriction;
                    WheelFrictionCurve sideways = wheel.sidewaysFriction;
                    baseline.WheelBaselines[id] = new WheelFrictionBaseline
                    {
                        ForwardStiffness = forward.stiffness,
                        SidewaysStiffness = sideways.stiffness
                    };
                }
                catch { }
            }
        }

        private void ApplyWheelTraction(LandVehicle vehicle, VehicleTuneBaseline baseline, float traction)
        {
            List<WheelCollider> wheels = GetWheelColliders(vehicle);
            for (int i = 0; i < wheels.Count; i++)
            {
                WheelCollider wheel = wheels[i];
                if (wheel == null)
                    continue;

                try
                {
                    int id = wheel.GetInstanceID();
                    WheelFrictionBaseline wheelBaseline;
                    if (!baseline.WheelBaselines.TryGetValue(id, out wheelBaseline))
                    {
                        WheelFrictionCurve capturedForward = wheel.forwardFriction;
                        WheelFrictionCurve capturedSideways = wheel.sidewaysFriction;
                        wheelBaseline = new WheelFrictionBaseline
                        {
                            ForwardStiffness = capturedForward.stiffness,
                            SidewaysStiffness = capturedSideways.stiffness
                        };
                        baseline.WheelBaselines[id] = wheelBaseline;
                    }

                    WheelFrictionCurve forward = wheel.forwardFriction;
                    WheelFrictionCurve sideways = wheel.sidewaysFriction;
                    forward.stiffness = Mathf.Clamp(wheelBaseline.ForwardStiffness * traction, 0.05f, 10f);
                    sideways.stiffness = Mathf.Clamp(wheelBaseline.SidewaysStiffness * traction, 0.05f, 10f);
                    wheel.forwardFriction = forward;
                    wheel.sidewaysFriction = sideways;
                }
                catch { }
            }
        }

        private List<WheelCollider> GetWheelColliders(LandVehicle vehicle)
        {
            var result = new List<WheelCollider>();
            if (vehicle == null)
                return result;

            try
            {
                var wheelList = vehicle.wheels;
                if (wheelList != null)
                {
                    for (int i = 0; i < wheelList.Count; i++)
                    {
                        Wheel wheel = wheelList[i];
                        AddWheelColliderIfUnique(result, wheel?.wheelCollider);
                    }
                }
            }
            catch { }

            try
            {
                WheelCollider[] childWheels = vehicle.GetComponentsInChildren<WheelCollider>(true);
                if (childWheels != null)
                {
                    for (int i = 0; i < childWheels.Length; i++)
                        AddWheelColliderIfUnique(result, childWheels[i]);
                }
            }
            catch { }

            return result;
        }

        private static void AddWheelColliderIfUnique(List<WheelCollider> wheels, WheelCollider wheel)
        {
            if (wheels == null || wheel == null)
                return;

            int id;
            try { id = wheel.GetInstanceID(); }
            catch { id = 0; }

            for (int i = 0; i < wheels.Count; i++)
            {
                try
                {
                    if (wheels[i] != null && wheels[i].GetInstanceID() == id)
                        return;
                }
                catch { }
            }

            wheels.Add(wheel);
        }

        private void CaptureHeadlightBaselines(LandVehicle vehicle, VehicleTuneBaseline baseline)
        {
            try
            {
                VehicleLights lights = vehicle.GetComponentInChildren<VehicleLights>(true);
                var sources = lights?.headLightSources;
                if (sources == null)
                    return;

                for (int i = 0; i < sources.Length; i++)
                {
                    try
                    {
                        Light light = sources[i]?._Light;
                        if (light == null)
                            continue;

                        int id = light.GetInstanceID();
                        if (baseline.LightBaselines.ContainsKey(id))
                            continue;

                        baseline.LightBaselines[id] = new LightBaseline
                        {
                            Color = light.color,
                            Intensity = light.intensity,
                            Range = light.range
                        };
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ApplyHeadlightTune(LandVehicle vehicle, VehicleTuneBaseline baseline, VehicleTuneSettings tune)
        {
            try
            {
                VehicleLights lights = vehicle.GetComponentInChildren<VehicleLights>(true);
                var sources = lights?.headLightSources;
                if (sources != null)
                {
                    Color color = new Color(
                        Mathf.Clamp01(tune.HeadlightRed),
                        Mathf.Clamp01(tune.HeadlightGreen),
                        Mathf.Clamp01(tune.HeadlightBlue),
                        1f);
                    float brightness = Mathf.Clamp(tune.HeadlightBrightnessMultiplier, 0.1f, 10f);

                    for (int i = 0; i < sources.Length; i++)
                    {
                        try
                        {
                            Light light = sources[i]?._Light;
                            if (light == null)
                                continue;

                            int id = light.GetInstanceID();
                            LightBaseline lightBaseline;
                            if (!baseline.LightBaselines.TryGetValue(id, out lightBaseline))
                            {
                                lightBaseline = new LightBaseline
                                {
                                    Color = light.color,
                                    Intensity = light.intensity,
                                    Range = light.range
                                };
                                baseline.LightBaselines[id] = lightBaseline;
                            }

                            light.color = color;
                            light.intensity = Mathf.Max(0.05f, lightBaseline.Intensity * brightness);
                            light.range = Mathf.Max(0.5f, lightBaseline.Range * Mathf.Lerp(0.75f, 2f, brightness / 10f));
                        }
                        catch { }
                    }
                }

                ApplyHeadlightMeshColor(lights, tune);
            }
            catch { }
        }

        private static void ApplyHeadlightMeshColor(VehicleLights lights, VehicleTuneSettings tune)
        {
            if (lights == null)
                return;

            try
            {
                var meshes = lights.headLightMeshes;
                if (meshes == null)
                    return;

                Color color = new Color(
                    Mathf.Clamp01(tune.HeadlightRed),
                    Mathf.Clamp01(tune.HeadlightGreen),
                    Mathf.Clamp01(tune.HeadlightBlue),
                    1f);
                Color emission = color * Mathf.Clamp(tune.HeadlightBrightnessMultiplier, 0.1f, 10f);

                for (int i = 0; i < meshes.Length; i++)
                {
                    MeshRenderer mesh = meshes[i];
                    if (mesh == null)
                        continue;

                    try
                    {
                        Material material = mesh.material;
                        if (material == null)
                            continue;

                        if (material.HasProperty("_Color"))
                            material.color = color;
                        if (material.HasProperty("_EmissionColor"))
                            material.SetColor("_EmissionColor", emission);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ApplyBodyColorTune(LandVehicle vehicle, VehicleTuneBaseline baseline, VehicleTuneSettings tune)
        {
            if (!tune.BodyColorEnabled)
                return;

            try
            {
                if (!tune.HasAppliedBodyColor || tune.LastAppliedBodyColor != tune.BodyColor)
                {
                    try { vehicle.SendOwnedColor(tune.BodyColor); }
                    catch { vehicle.ApplyColor(tune.BodyColor); }

                    tune.LastAppliedBodyColor = tune.BodyColor;
                    tune.HasAppliedBodyColor = true;
                }
                else
                {
                    try { vehicle.ApplyColor(tune.BodyColor); } catch { }
                }
            }
            catch
            {
                try
                {
                    if (baseline.HasBodyColor)
                        vehicle.ApplyColor(baseline.BodyColor);
                }
                catch { }
            }
        }

        private List<PoliceLight> FindPoliceLights(LandVehicle vehicle)
        {
            var result = new List<PoliceLight>();
            if (vehicle == null)
                return result;

            try
            {
                PoliceLight[] direct = vehicle.GetComponentsInChildren<PoliceLight>(true);
                if (direct != null)
                {
                    for (int i = 0; i < direct.Length; i++)
                        AddPoliceLightIfUnique(result, direct[i]);
                }
            }
            catch { }

            try
            {
                MonoBehaviour[] behaviours = vehicle.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours != null)
                {
                    for (int i = 0; i < behaviours.Length; i++)
                    {
                        MonoBehaviour behaviour = behaviours[i];
                        if (behaviour == null)
                            continue;

                        string typeName = GetBehaviourTypeName(behaviour);
                        if (typeName.IndexOf("PoliceLight", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        try { AddPoliceLightIfUnique(result, behaviour.TryCast<PoliceLight>()); }
                        catch { }
                    }
                }
            }
            catch { }

            return result;
        }

        private static void AddPoliceLightIfUnique(List<PoliceLight> list, PoliceLight policeLight)
        {
            if (list == null || policeLight == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    if (list[i] != null && list[i].Pointer == policeLight.Pointer)
                        return;
                }
                catch
                {
                    if (list[i] == policeLight)
                        return;
                }
            }

            list.Add(policeLight);
        }

        private static bool IsAnyPoliceLightOn(List<PoliceLight> policeLights)
        {
            if (policeLights == null)
                return false;

            for (int i = 0; i < policeLights.Count; i++)
            {
                try
                {
                    if (policeLights[i] != null && policeLights[i].IsOn)
                        return true;
                }
                catch { }
            }

            return false;
        }

        private void ApplyPoliceSirenAudio(PoliceLight policeLight, bool on)
        {
            if (policeLight == null)
                return;

            try
            {
                var siren = policeLight.Siren;
                if (siren == null)
                    return;

                siren.SetLoop(true);
                siren.VolumeMultiplier = 1f;

                if (on)
                {
                    if (!siren.IsPlaying)
                        siren.Play();
                }
                else
                {
                    if (siren.IsPlaying)
                        siren.Stop();
                }
            }
            catch (Exception ex)
            {
                LogPoliceSirenFailureOnce("Police siren audio toggle failed: " + ex.Message);
            }
        }

        private void ReportMissingPoliceLightbar(LandVehicle vehicle)
        {
            if (_reportedPoliceSirenMissingLightbar)
                return;

            _reportedPoliceSirenMissingLightbar = true;
            UnityEngine.Debug.LogWarning(
                "[Nugzz] Police siren requested but no PoliceLight component was found under " +
                SafeVehicleName(vehicle) + ". This vehicle may use a different lightbar component.");
            NotificationService.Instance.Warning("No police lightbar component found on this vehicle");
        }

        private void SuppressPoliceHeadlights(LandVehicle vehicle)
        {
            if (vehicle == null)
                return;

            try
            {
                VehicleLights vehicleLights = vehicle.GetComponentInChildren<VehicleLights>(true);
                SuppressPoliceHeadlights(vehicleLights);
            }
            catch (Exception ex)
            {
                LogPoliceSirenFailureOnce("Failed to suppress police headlights: " + ex.Message);
            }
        }

        private static void SuppressPoliceHeadlights(VehicleLights vehicleLights)
        {
            if (vehicleLights == null)
                return;

            try { vehicleLights.HeadlightsOn = false; } catch { }

            try
            {
                var sources = vehicleLights.headLightSources;
                if (sources != null)
                {
                    for (int i = 0; i < sources.Length; i++)
                    {
                        try { sources[i]?.SetEnabled(false); } catch { }
                    }
                }
            }
            catch { }

            try
            {
                var meshes = vehicleLights.headLightMeshes;
                Material offMaterial = null;
                try { offMaterial = vehicleLights.headLightMat_Off; } catch { }

                if (meshes != null && offMaterial != null)
                {
                    for (int i = 0; i < meshes.Length; i++)
                    {
                        try
                        {
                            if (meshes[i] != null)
                                meshes[i].sharedMaterial = offMaterial;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void LogPoliceSirenFailureOnce(string message)
        {
            if (_reportedPoliceSirenFailure)
                return;

            _reportedPoliceSirenFailure = true;
            DebugLogService.Instance.VerboseWarning(message);
        }

        private static string SafeMaterialName(Material material)
        {
            try
            {
                return material != null ? material.name ?? "unnamed" : "null";
            }
            catch
            {
                return "unknown";
            }
        }

        private bool IsPoliceVehicle(string name, string code, string category)
        {
            if (string.Equals(category, "Police", StringComparison.OrdinalIgnoreCase))
                return true;

            string text = ((name ?? "") + " " + (code ?? "")).ToLowerInvariant();
            return text.Contains("police") ||
                text.Contains("sheriff") ||
                text.Contains("patrol") ||
                text.Contains("pursuit") ||
                text.Contains("swat") ||
                text.Contains("law") ||
                text.Contains("roadblock");
        }

        private string SpawnCustomSpecialVehicle(
            int attemptId,
            LandVehicle prefab,
            string cleanName,
            string specialVehicleName,
            string specialVehicleCode,
            string category,
            Vector3 position,
            Quaternion rotation,
            bool playerOwned)
        {
            if (prefab == null || prefab.gameObject == null)
            {
                UnityEngine.Debug.LogError(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " failed: custom special prefab missing for " + specialVehicleCode);
                return null;
            }

            VehicleManager manager = FindObjectOfType<VehicleManager>();
            if (manager == null)
            {
                UnityEngine.Debug.LogError(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " failed: VehicleManager missing for custom special spawn");
                return null;
            }

            GameObject clone = null;
            try
            {
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " custom special spawn: '" + cleanName +
                    "' code='" + specialVehicleCode +
                    "' category=" + category +
                    " ownerForced=" + playerOwned);

                clone = Instantiate(prefab.gameObject, position, rotation);
                if (clone == null)
                    return null;

                clone.name = cleanName + " (Nugzz Custom)";
                clone.transform.SetPositionAndRotation(position, rotation);

                LandVehicle vehicle = clone.GetComponent<LandVehicle>();
                if (vehicle == null)
                {
                    Destroy(clone);
                    UnityEngine.Debug.LogError(
                        "[Nugzz] Vehicle spawn #" + attemptId +
                        " failed: custom clone has no LandVehicle component");
                    return null;
                }

                AssignUniqueVehicleGuid(vehicle, attemptId);
                PrepareCustomVehicleBeforeNetworkSpawn(vehicle, playerOwned);
                RegisterCustomVehicle(manager, vehicle, attemptId);

                if (!ValidateCustomVehicleNetworkHierarchy(vehicle, attemptId))
                {
                    Destroy(clone);
                    return null;
                }

                if (!SpawnCustomVehicleNetworked(manager, vehicle, attemptId))
                {
                    Destroy(clone);
                    return null;
                }

                SanitizeRiskyVehicle(vehicle);
                ForceVehicleOwned(vehicle, playerOwned, attemptId);
                EnsureLocalRiskyVehicleVisible(vehicle, position, rotation);
                VehicleCollisionService.Instance.ApplyVehicle(vehicle);
                QueueLocalRiskyVehicleVisualRepair(vehicle);

                UnityEngine.Debug.Log(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " custom special success: " + DescribeVehicle(vehicle));
                return cleanName;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " custom special spawn failed: " + ex);

                try
                {
                    if (clone != null)
                        Destroy(clone);
                }
                catch { }

                return null;
            }
        }

        private void AssignUniqueVehicleGuid(LandVehicle vehicle, int attemptId)
        {
            if (vehicle == null)
                return;

            try
            {
                var guid = GUIDManager.GenerateUniqueGUID();
                vehicle.SetGUID(guid);
                DebugLogService.Instance.Verbose(
                    "Vehicle spawn #" + attemptId +
                    " assigned custom GUID " + guid + " to " + SafeVehicleName(vehicle));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " failed to assign custom vehicle GUID: " + ex.Message);
            }
        }

        private void RegisterCustomVehicle(VehicleManager manager, LandVehicle vehicle, int attemptId)
        {
            if (manager == null || vehicle == null)
                return;

            try
            {
                if (manager.AllVehicles != null && !manager.AllVehicles.Contains(vehicle))
                    manager.AllVehicles.Add(vehicle);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " failed to register custom vehicle lists: " + ex.Message);
            }
        }

        private void PrepareCustomVehicleBeforeNetworkSpawn(LandVehicle vehicle, bool playerOwned)
        {
            if (vehicle == null)
                return;

            try { vehicle.SpawnAsPlayerOwned = playerOwned; } catch { }
            try { vehicle.IsOccupied = false; } catch { }
            try { vehicle.transform.SetParent(null, true); } catch { }
        }

        private bool ValidateCustomVehicleNetworkHierarchy(LandVehicle vehicle, int attemptId)
        {
            if (vehicle == null || vehicle.gameObject == null)
                return false;

            try
            {
                if (vehicle.NetworkObject == null)
                {
                    UnityEngine.Debug.LogError(
                        "[Nugzz] Vehicle spawn #" + attemptId +
                        " aborted: custom vehicle has no FishNet NetworkObject");
                    return false;
                }

                var networkObjects = vehicle.GetComponentsInChildren<NetworkObject>(true);
                var networkBehaviours = vehicle.GetComponentsInChildren<NetworkBehaviour>(true);

                DebugLogService.Instance.Verbose(
                    "Vehicle spawn #" + attemptId +
                    " network preflight ok objects=" + (networkObjects?.Length ?? 0) +
                    " behaviours=" + (networkBehaviours?.Length ?? 0));
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " aborted: custom vehicle network hierarchy is invalid: " + ex);
                return false;
            }
        }

        private bool SpawnCustomVehicleNetworked(VehicleManager manager, LandVehicle vehicle, int attemptId)
        {
            if (manager == null || vehicle == null || vehicle.gameObject == null)
                return false;

            try
            {
                ((NetworkBehaviour)manager).Spawn(vehicle.gameObject);
                DebugLogService.Instance.Verbose(
                    "Vehicle spawn #" + attemptId +
                    " FishNet spawned custom special vehicle " + SafeVehicleName(vehicle));
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " failed to network-spawn custom vehicle: " + ex);
                return false;
            }
        }

        private void ForceVehicleOwned(LandVehicle vehicle, bool playerOwned, int attemptId)
        {
            if (vehicle == null)
                return;

            try { vehicle.SpawnAsPlayerOwned = playerOwned; } catch { }
            try { vehicle.IsOccupied = false; } catch { }
            try { vehicle.SetVisible(true); } catch { }

            try
            {
                if (playerOwned)
                    vehicle.SetIsPlayerOwned(null, true);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Vehicle spawn #" + attemptId +
                    " failed to force custom vehicle ownership: " + ex.Message);
            }

            try
            {
                if (playerOwned)
                {
                    var manager = FindObjectOfType<VehicleManager>();
                    if (manager?.PlayerOwnedVehicles != null && !manager.PlayerOwnedVehicles.Contains(vehicle))
                        manager.PlayerOwnedVehicles.Add(vehicle);
                }
            }
            catch { }
        }

        private static string FormatVector(Vector3 value)
        {
            return "(" + value.x.ToString("0.00") + ", " + value.y.ToString("0.00") + ", " + value.z.ToString("0.00") + ")";
        }

        private static string DescribeVehicle(LandVehicle vehicle)
        {
            if (vehicle == null)
                return "null";

            string name = "unknown";
            string code = "";
            bool active = false;
            bool sceneValid = false;
            bool networked = false;

            try { name = vehicle.name ?? "unknown"; } catch { }
            try { code = vehicle.VehicleCode ?? ""; } catch { }
            try { active = vehicle.gameObject != null && vehicle.gameObject.activeInHierarchy; } catch { }
            try { sceneValid = vehicle.gameObject != null && vehicle.gameObject.scene.IsValid(); } catch { }
            try { networked = vehicle.NetworkObject != null; } catch { }

            return "name='" + name + "' code='" + code + "' active=" + active + " scene=" + sceneValid + " networked=" + networked;
        }

        private static string SafeVehicleName(LandVehicle vehicle)
        {
            try
            {
                return vehicle != null ? vehicle.name : "null";
            }
            catch
            {
                return "unknown";
            }
        }

        private static string SafeVehicleCode(LandVehicle vehicle)
        {
            try
            {
                return vehicle != null ? vehicle.VehicleCode ?? "" : "";
            }
            catch
            {
                return "";
            }
        }

        private LandVehicle FindActiveRV()
        {
            try
            {
                var vehicleManager = FindObjectOfType<VehicleManager>();
                if (vehicleManager?.AllVehicles != null)
                {
                    for (int i = 0; i < vehicleManager.AllVehicles.Count; i++)
                    {
                        LandVehicle vehicle = vehicleManager.AllVehicles[i];
                        if (IsRVVehicle(vehicle))
                            return vehicle;
                    }
                }
            }
            catch { }

            try
            {
                var vehicles = FindObjectsOfType<LandVehicle>(true);
                if (vehicles != null)
                {
                    for (int i = 0; i < vehicles.Length; i++)
                    {
                        LandVehicle vehicle = vehicles[i];
                        if (IsRVVehicle(vehicle) && vehicle.gameObject != null && vehicle.gameObject.scene.IsValid())
                            return vehicle;
                    }
                }
            }
            catch { }

            return null;
        }

        private RV FindStoryRV()
        {
            RV fallback = null;

            try
            {
                var rvs = FindObjectsOfType<RV>(true);
                RV best = FindBestStoryRV(rvs, ref fallback);
                if (best != null)
                    return best;
            }
            catch { }

            try
            {
                var properties = Property.Properties;
                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        RV rv = properties[i]?.TryCast<RV>();
                        if (rv == null)
                            continue;

                        if (fallback == null)
                            fallback = rv;

                        if (IsUsableStoryRV(rv))
                            return rv;
                    }
                }
            }
            catch { }

            try
            {
                var rvs = Resources.FindObjectsOfTypeAll<RV>();
                RV best = FindBestStoryRV(rvs, ref fallback);
                if (best != null)
                    return best;
            }
            catch { }

            return fallback;
        }

        private static RV FindBestStoryRV(RV[] rvs, ref RV fallback)
        {
            if (rvs == null)
                return null;

            RV activeDestroyed = null;
            RV inactiveUsable = null;
            for (int i = 0; i < rvs.Length; i++)
            {
                RV rv = rvs[i];
                if (rv == null)
                    continue;

                if (fallback == null)
                    fallback = rv;

                bool activeInScene = false;
                try
                {
                    activeInScene = rv.gameObject != null &&
                        rv.gameObject.scene.IsValid() &&
                        rv.gameObject.activeInHierarchy;
                }
                catch { }

                if (activeInScene && IsUsableStoryRV(rv))
                    return rv;

                if (activeInScene && activeDestroyed == null)
                    activeDestroyed = rv;
                else if (inactiveUsable == null && IsUsableStoryRV(rv))
                    inactiveUsable = rv;
            }

            return inactiveUsable ?? activeDestroyed;
        }

        private static bool IsUsableStoryRV(RV rv)
        {
            if (rv == null)
                return false;

            try
            {
                if (rv.IsDestroyed)
                    return false;
            }
            catch { }

            try
            {
                return rv.gameObject != null && rv.gameObject.scene.IsValid();
            }
            catch
            {
                return true;
            }
        }

        private void RepairStoryRV(RV rv)
        {
            if (rv == null)
                return;

            try { rv.gameObject.SetActive(true); } catch { }
            try { rv.SetContentCulled(false); } catch { }
            try { if (!rv.IsOwned) rv.SetOwned(); } catch { }

            TrySetPrivateBool(rv, "IsDestroyed", false);
            TrySetPrivateBool(rv, "<IsDestroyed>k__BackingField", false);
            TrySetPrivateBool(rv, "_exploded", false);

            try { rv.ModelContainer?.gameObject.SetActive(true); } catch { }
            try { rv.FXContainer?.gameObject.SetActive(false); } catch { }
            DisableDestroyedRVVisuals(rv);
        }

        private void PrepareStoryRVForBlowUp(RV rv)
        {
            if (rv == null)
                return;

            try { rv.gameObject.SetActive(true); } catch { }
            try { rv.SetContentCulled(false); } catch { }
            try { rv.ModelContainer?.gameObject.SetActive(true); } catch { }

            TrySetPrivateBool(rv, "IsDestroyed", false);
            TrySetPrivateBool(rv, "<IsDestroyed>k__BackingField", false);
            TrySetPrivateBool(rv, "_exploded", false);

            try
            {
                if (rv.FXContainer != null)
                    SetHierarchyActive(rv.FXContainer, true);
            }
            catch { }
        }

        private void DisableDestroyedRVVisuals(RV rv)
        {
            if (rv == null)
                return;

            try
            {
                Transform[] children = rv.GetComponentsInChildren<Transform>(true);
                if (children == null)
                    return;

                for (int i = 0; i < children.Length; i++)
                {
                    Transform child = children[i];
                    if (child == null || child == rv.transform)
                        continue;
                    if (rv.ModelContainer != null &&
                        (child == rv.ModelContainer || child.IsChildOf(rv.ModelContainer)))
                    {
                        continue;
                    }
                    if (rv.FXContainer != null &&
                        (child == rv.FXContainer || child.IsChildOf(rv.FXContainer)))
                    {
                        continue;
                    }

                    string name = child.name ?? string.Empty;
                    if (!LooksLikeDestroyedRVVisual(name))
                        continue;

                    try { child.gameObject.SetActive(false); } catch { }
                }
            }
            catch { }
        }

        private static void SetHierarchyActive(Transform root, bool active)
        {
            if (root == null)
                return;

            try
            {
                Transform[] children = root.GetComponentsInChildren<Transform>(true);
                if (children == null)
                {
                    root.gameObject.SetActive(active);
                    return;
                }

                for (int i = 0; i < children.Length; i++)
                {
                    Transform child = children[i];
                    if (child != null)
                        child.gameObject.SetActive(active);
                }
            }
            catch
            {
                try { root.gameObject.SetActive(active); } catch { }
            }
        }

        private static bool LooksLikeDestroyedRVVisual(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            string lower = name.ToLowerInvariant();
            return lower.Contains("destroy") ||
                lower.Contains("explod") ||
                lower.Contains("wreck") ||
                lower.Contains("debris") ||
                lower.Contains("rubble") ||
                lower.Contains("broken") ||
                lower.Contains("burn") ||
                lower.Contains("fire") ||
                lower.Contains("smoke") ||
                lower.Contains("fx");
        }

        private static void TrySetPrivateBool(object target, string fieldName, bool value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
                return;

            try
            {
                var property = target.GetType().GetProperty(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                var setter = property?.GetSetMethod(true);
                if (setter != null)
                {
                    setter.Invoke(target, new object[] { value });
                    return;
                }
            }
            catch { }

            try
            {
                var field = FindField(target.GetType(), fieldName);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }
            catch { }

            try
            {
                var field = FindField(typeof(RV), fieldName);
                if (field != null)
                    field.SetValue(target, value);
            }
            catch { }
        }

        private static float GetPrivateFloat(object target, string fieldName, float fallback)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
                return fallback;

            try
            {
                var property = target.GetType().GetProperty(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                var getter = property?.GetGetMethod(true);
                if (getter != null)
                    return Convert.ToSingle(getter.Invoke(target, null));
            }
            catch { }

            try
            {
                var field = FindField(target.GetType(), fieldName);
                if (field != null)
                    return Convert.ToSingle(field.GetValue(target));
            }
            catch { }

            return fallback;
        }

        private static void SetPrivateFloat(object target, string fieldName, float value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
                return;

            try
            {
                var property = target.GetType().GetProperty(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                var setter = property?.GetSetMethod(true);
                if (setter != null)
                {
                    setter.Invoke(target, new object[] { value });
                    return;
                }
            }
            catch { }

            try
            {
                var field = FindField(target.GetType(), fieldName);
                if (field != null)
                    field.SetValue(target, value);
            }
            catch { }
        }

        private static T GetPrivateObject<T>(object target, string fieldName) where T : class
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
                return null;

            try
            {
                var property = target.GetType().GetProperty(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                var getter = property?.GetGetMethod(true);
                if (getter != null)
                    return getter.Invoke(target, null) as T;
            }
            catch { }

            try
            {
                var field = FindField(target.GetType(), fieldName);
                if (field != null)
                    return field.GetValue(target) as T;
            }
            catch { }

            return null;
        }

        private static void SetPrivateObject(object target, string fieldName, object value)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
                return;

            try
            {
                var property = target.GetType().GetProperty(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                var setter = property?.GetSetMethod(true);
                if (setter != null)
                {
                    setter.Invoke(target, new[] { value });
                    return;
                }
            }
            catch { }

            try
            {
                var field = FindField(target.GetType(), fieldName);
                if (field != null)
                    field.SetValue(target, value);
            }
            catch { }
        }

        private static AnimationCurve CloneAnimationCurve(AnimationCurve source)
        {
            if (source == null)
                return null;

            try
            {
                return new AnimationCurve(source.keys);
            }
            catch
            {
                return null;
            }
        }

        private static AnimationCurve ScaleAnimationCurve(AnimationCurve source, float multiplier)
        {
            if (source == null)
                return null;

            try
            {
                Keyframe[] keys = source.keys;
                if (keys == null || keys.Length == 0)
                    return CloneAnimationCurve(source);

                float scale = Mathf.Clamp(multiplier, 0.05f, 12f);
                for (int i = 0; i < keys.Length; i++)
                {
                    Keyframe key = keys[i];
                    key.value *= scale;
                    key.inTangent *= scale;
                    key.outTangent *= scale;
                    keys[i] = key;
                }

                AnimationCurve curve = new AnimationCurve(keys);
                try
                {
                    curve.preWrapMode = source.preWrapMode;
                    curve.postWrapMode = source.postWrapMode;
                }
                catch { }
                return curve;
            }
            catch
            {
                return CloneAnimationCurve(source);
            }
        }

        private static System.Reflection.FieldInfo FindField(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(
                    fieldName,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        private LandVehicle FindRVPrefab()
        {
            try
            {
                var vehicleManager = FindObjectOfType<VehicleManager>();
                if (vehicleManager?.VehiclePrefabs != null)
                {
                    for (int i = 0; i < vehicleManager.VehiclePrefabs.Count; i++)
                    {
                        LandVehicle vehicle = vehicleManager.VehiclePrefabs[i];
                        if (IsRVVehicle(vehicle))
                            return vehicle;
                    }
                }
            }
            catch { }

            try
            {
                var vehicles = Resources.FindObjectsOfTypeAll<LandVehicle>();
                if (vehicles != null)
                {
                    for (int i = 0; i < vehicles.Length; i++)
                    {
                        LandVehicle vehicle = vehicles[i];
                        if (IsRVVehicle(vehicle))
                            return vehicle;
                    }
                }
            }
            catch { }

            return null;
        }

        private bool IsRVVehicle(LandVehicle vehicle)
        {
            if (vehicle == null)
                return false;

            try
            {
                if (IsRVVehicleText(vehicle.VehicleCode))
                    return true;
            }
            catch { }

            try
            {
                if (IsRVVehicleText(vehicle.name))
                    return true;
            }
            catch { }

            return false;
        }

        private bool IsRVVehicleText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            string lower = value.ToLowerInvariant();
            return lower == "rv" ||
                lower.Contains(" rv") ||
                lower.Contains("rv_") ||
                lower.Contains("_rv") ||
                lower.Contains("camper") ||
                lower.Contains("motorhome") ||
                lower.Contains("recreational") ||
                lower.Contains("winnebago");
        }

        private bool EnsureBenzieManorAccess(bool showErrors)
        {
            if (!CanSpawnVehicles())
            {
                if (showErrors)
                    NotificationService.Instance.Warning("Benzie Manor access is host-only in multiplayer");
                _benzieManorAccessEnabled = false;
                return false;
            }

            try
            {
                Manor manor = FindBenzieManor();
                if (manor == null)
                {
                    if (showErrors)
                        NotificationService.Instance.Warning("Benzie Manor not found in this scene");
                    return false;
                }

                try { manor.gameObject.SetActive(true); } catch { }
                try { manor.SetContentCulled(false); } catch { }

                try
                {
                    if (!manor.IsOwned)
                        manor.SetOwned();
                }
                catch { }

                try
                {
                    if (manor.ManorState != Manor.EManorState.Rebuilt)
                        manor.Rebuild();
                }
                catch { }

                try
                {
                    if (!manor.TunnelDug)
                        manor.DigTunnel();
                }
                catch { }

                TryRepairManorVisuals(manor);
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to unlock Benzie Manor: " + ex);
                if (showErrors)
                    NotificationService.Instance.Warning("Failed to unlock Benzie Manor");
                return false;
            }
        }

        private Manor FindBenzieManor()
        {
            try
            {
                Manor manor = FindObjectOfType<Manor>(true);
                if (manor != null)
                    return manor;
            }
            catch { }

            try
            {
                var properties = Property.Properties;
                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        Manor manor = properties[i]?.TryCast<Manor>();
                        if (manor != null)
                            return manor;
                    }
                }
            }
            catch { }

            try
            {
                var manors = Resources.FindObjectsOfTypeAll<Manor>();
                if (manors != null && manors.Length > 0)
                    return manors[0];
            }
            catch { }

            return null;
        }

        private void TryRepairManorVisuals(Manor manor)
        {
            if (manor == null)
                return;

            try { manor.OriginalContainer?.SetActive(false); } catch { }
            try { manor.DestroyedContainer?.SetActive(false); } catch { }
            try { manor.RebuiltContainer?.SetActive(true); } catch { }
            try { manor.DestructionFXContainer?.SetActive(false); } catch { }
            try { manor.TunnelBlocker?.SetActive(false); } catch { }
            try { manor.TunnelCollapse?.SetActive(false); } catch { }
            try { manor.ConstructionContainer?.SetActive(false); } catch { }

            try
            {
                if (manor.DisableOnRebuild == null)
                    return;

                for (int i = 0; i < manor.DisableOnRebuild.Length; i++)
                    manor.DisableOnRebuild[i]?.SetActive(false);
            }
            catch { }
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
                lower.Contains("npc") ||
                lower.Contains("ai") ||
                lower.Contains("tractor") ||
                lower.Contains("forklift") ||
                lower.Contains("farm") ||
                lower.Contains("utility") ||
                lower.Contains("delivery") ||
                lower.Contains("work") ||
                lower.Contains("trailer");
        }

        private void SanitizeRiskyVehicle(LandVehicle vehicle)
        {
            if (vehicle == null)
                return;

            try { vehicle.SpawnAsPlayerOwned = true; } catch { }
            RemoveUnsafeSpecialVehicleComponents(vehicle);

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

                    string typeName = GetBehaviourTypeName(behaviour);
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
                lower.Contains("vehicleai") ||
                lower.Contains("npcvehicle") ||
                lower.Contains("aivehicle") ||
                lower.Contains("pursuit") ||
                lower.Contains("patrol") ||
                lower.Contains("checkpoint") ||
                lower.Contains("deliveryvehicle") ||
                lower.Contains("obstructiondetector");
        }

        private bool ShouldRemoveUnsafeSpecialVehicleComponent(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            string lower = typeName.ToLowerInvariant();
            return lower.Contains("scheduleone.trash.") ||
                lower.EndsWith(".trashitem") ||
                lower.Contains("trashitem") ||
                lower.Contains("trashgenerator") ||
                lower.Contains("trashspawnvolume");
        }

        private static string GetBehaviourTypeName(MonoBehaviour behaviour)
        {
            if (behaviour == null)
                return string.Empty;

            try
            {
                return behaviour.GetIl2CppType()?.FullName ??
                    behaviour.GetType().FullName ??
                    string.Empty;
            }
            catch
            {
                try { return behaviour.GetType().FullName ?? string.Empty; }
                catch { return string.Empty; }
            }
        }

        private void RemoveUnsafeSpecialVehicleComponents(LandVehicle vehicle)
        {
            if (vehicle == null)
                return;

            try
            {
                var behaviours = vehicle.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours == null)
                    return;

                int disabled = 0;
                for (int i = 0; i < behaviours.Length; i++)
                {
                    var behaviour = behaviours[i];
                    if (behaviour == null)
                        continue;

                    string typeName = GetBehaviourTypeName(behaviour);
                    if (!ShouldRemoveUnsafeSpecialVehicleComponent(typeName))
                        continue;

                    try
                    {
                        behaviour.enabled = false;
                        disabled++;
                    }
                    catch { }
                }

                if (disabled > 0)
                {
                    DebugLogService.Instance.Verbose(
                        "Disabled unsafe special vehicle components on " + SafeVehicleName(vehicle) +
                        ": disabled=" + disabled);
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Unsafe special vehicle component cleanup failed: " + ex.Message);
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
            try { vehicle.SpawnAsPlayerOwned = true; } catch { }
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
                string source = _vehicleSources[i];
                string category = _vehicleCategories[i];
                LandVehicle prefab = _vehiclePrefabs[i];
                int j = i - 1;

                while (j >= 0 && string.Compare(_vehicleNames[j], name, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    _vehicleNames[j + 1] = _vehicleNames[j];
                    _vehicleCodes[j + 1] = _vehicleCodes[j];
                    _vehicleRisky[j + 1] = _vehicleRisky[j];
                    _vehicleSources[j + 1] = _vehicleSources[j];
                    _vehicleCategories[j + 1] = _vehicleCategories[j];
                    _vehiclePrefabs[j + 1] = _vehiclePrefabs[j];
                    j--;
                }

                _vehicleNames[j + 1] = name;
                _vehicleCodes[j + 1] = code;
                _vehicleRisky[j + 1] = risky;
                _vehicleSources[j + 1] = source;
                _vehicleCategories[j + 1] = category;
                _vehiclePrefabs[j + 1] = prefab;
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
