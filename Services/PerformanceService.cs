using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Lighting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NugzzMenu.Services
{
    public sealed class PerformanceService
    {
        private sealed class LightState
        {
            public OptimizedLight Light;
            public float OriginalDistance;
        }

        private static readonly PerformanceService _instance = new PerformanceService();
        public static PerformanceService Instance => _instance;

        private readonly Dictionary<int, LightState> _lightStates =
            new Dictionary<int, LightState>();
        private readonly Dictionary<int, float> _lastProbeUpdates =
            new Dictionary<int, float>();
        private readonly float _defaultLodBias;
        private readonly float _defaultShadowDistance;
        private readonly FieldInfo _lightDistanceSquaredField;

        private float _lightRangeScale = 1f;
        private float _reflectionInterval;
        private float _nextLightRefresh;
        private int _lightSceneHandle = -1;
        private string _diagnostics = "Scene diagnostics have not been scanned.";
        private long _nugzzUpdateStarted;
        private double _nugzzUpdateTotalMs;
        private int _nugzzUpdateSamples;
        private float _nextTimingPublish;
        private float _nugzzUpdateAverageMs;
        private float _nextSummaryRefresh;
        private string _cachedSummary = "Collecting frame timing...";

        private PerformanceService()
        {
            _defaultLodBias = QualitySettings.lodBias;
            _defaultShadowDistance = QualitySettings.shadowDistance;
            _lightDistanceSquaredField = typeof(OptimizedLight).GetField(
                "maxDistanceSquared",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public int TargetFps => Application.targetFrameRate <= 0 ? 0 : Application.targetFrameRate;
        public bool VSyncEnabled => QualitySettings.vSyncCount > 0;
        public int GameTargetFps => TryReadDisplaySettings(out DisplaySettings display)
            ? (display.TargetFPS <= 0 ? 0 : display.TargetFPS)
            : TargetFps;
        public bool GameVSyncEnabled => TryReadDisplaySettings(out DisplaySettings display)
            ? display.VSync
            : VSyncEnabled;
        public float NugzzUpdateAverageMs => _nugzzUpdateAverageMs;
        public float LightRangeScale => _lightRangeScale;
        public float ReflectionInterval => _reflectionInterval;
        public float LodBias => QualitySettings.lodBias;
        public float ShadowDistance => QualitySettings.shadowDistance;
        public string Diagnostics => _diagnostics;

        public void BeginNugzzUpdate()
        {
            _nugzzUpdateStarted = System.Diagnostics.Stopwatch.GetTimestamp();
        }

        public void EndNugzzUpdate()
        {
            if (_nugzzUpdateStarted == 0)
                return;

            long elapsed = System.Diagnostics.Stopwatch.GetTimestamp() - _nugzzUpdateStarted;
            _nugzzUpdateStarted = 0;
            _nugzzUpdateTotalMs += elapsed * 1000d /
                System.Diagnostics.Stopwatch.Frequency;
            _nugzzUpdateSamples++;

            if (Time.unscaledTime < _nextTimingPublish)
                return;

            if (_nugzzUpdateSamples > 0)
                _nugzzUpdateAverageMs = (float)(_nugzzUpdateTotalMs / _nugzzUpdateSamples);
            _nugzzUpdateTotalMs = 0d;
            _nugzzUpdateSamples = 0;
            _nextTimingPublish = Time.unscaledTime + 1f;
        }

        public void Update()
        {
            if (_lightRangeScale >= 0.999f)
                return;

            int sceneHandle = SceneManager.GetActiveScene().handle;
            if (sceneHandle == _lightSceneHandle && Time.unscaledTime < _nextLightRefresh)
                return;

            if (sceneHandle != _lightSceneHandle)
            {
                _lightStates.Clear();
                _lastProbeUpdates.Clear();
                _lightSceneHandle = sceneHandle;
            }

            _nextLightRefresh = Time.unscaledTime + 20f;
            ApplyLightRangeBudget();
        }

        public string GetSummary()
        {
            if (Time.unscaledTime < _nextSummaryRefresh)
                return _cachedSummary;

            _nextSummaryRefresh = Time.unscaledTime + 0.25f;
            float fps = Time.smoothDeltaTime > 0.0001f ? 1f / Time.smoothDeltaTime : 0f;
            string cap = TargetFps <= 0 ? "Unlimited" : TargetFps.ToString();
            string gameCap = GameTargetFps <= 0 ? "Unlimited" : GameTargetFps.ToString();
            int refreshRate = 0;
            try { refreshRate = Screen.currentResolution.refreshRate; } catch { }
            _cachedSummary = "FPS " + fps.ToString("0") + " | Cap E/G " + cap + "/" + gameCap +
                " | Sync E/G " + (VSyncEnabled ? "ON" : "OFF") + "/" +
                (GameVSyncEnabled ? "ON" : "OFF") + " | " + refreshRate + " Hz | Nugzz " +
                _nugzzUpdateAverageMs.ToString("0.00") + " ms";
            return _cachedSummary;
        }

        public void SetTargetFps(int fps)
        {
            int target = fps <= 0 ? -1 : Mathf.Clamp(fps, 30, 500);
            ApplyNativeDisplaySettings(target, false);
            Application.targetFrameRate = target;
            QualitySettings.vSyncCount = 0;
            NotificationService.Instance.Status(fps <= 0 ? "FPS cap: Unlimited" : "FPS cap: " + fps);
        }

        public void SetVSync(bool enabled)
        {
            ApplyNativeDisplaySettings(null, enabled);
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            NotificationService.Instance.Status(enabled ? "VSync ON" : "VSync OFF");
        }

        public void SetLightRangeScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.35f, 1f);
            if (scale >= 0.999f)
            {
                RestoreLightRanges();
                NotificationService.Instance.Status("Decorative light ranges restored");
                return;
            }

            _lightRangeScale = scale;
            ApplyLightRangeBudget();
            NotificationService.Instance.Status(
                "Decorative light range: " + Mathf.RoundToInt(scale * 100f) + "%");
        }

        public void SetReflectionInterval(float seconds)
        {
            _reflectionInterval = Mathf.Clamp(seconds, 0f, 2f);
            _lastProbeUpdates.Clear();
            bool installed = ReflectionProbeUpdateThrottlePatch.SetInstalled(
                _reflectionInterval > 0f);
            if (_reflectionInterval > 0f && !installed)
            {
                _reflectionInterval = 0f;
                NotificationService.Instance.Warning("Reflection limiter unavailable; native mode kept");
                return;
            }
            NotificationService.Instance.Status(_reflectionInterval <= 0f
                ? "Reflection updates: Native"
                : "Reflection updates limited to every " + _reflectionInterval.ToString("0.0") + "s");
        }

        public bool ShouldUpdateReflectionProbe(ReflectionProbeUpdater updater)
        {
            if (_reflectionInterval <= 0f || updater == null)
                return true;

            int id;
            try { id = updater.GetInstanceID(); }
            catch { return true; }

            float now = Time.unscaledTime;
            if (_lastProbeUpdates.TryGetValue(id, out float last) &&
                now - last < _reflectionInterval)
            {
                return false;
            }

            _lastProbeUpdates[id] = now;
            return true;
        }

        public void SetLodBias(float value)
        {
            QualitySettings.lodBias = Mathf.Clamp(value, 0.5f, 1.5f);
            NotificationService.Instance.Status("LOD detail: " + QualitySettings.lodBias.ToString("0.00") + "x");
        }

        public void RestoreLodBias()
        {
            QualitySettings.lodBias = _defaultLodBias;
            NotificationService.Instance.Status("LOD detail restored");
        }

        public void SetShadowDistance(float distance)
        {
            QualitySettings.shadowDistance = Mathf.Clamp(distance, 20f, 140f);
            NotificationService.Instance.Status(
                "Shadow distance: " + QualitySettings.shadowDistance.ToString("0") + "m");
        }

        public void RestoreShadowDistance()
        {
            QualitySettings.shadowDistance = _defaultShadowDistance;
            NotificationService.Instance.Status("Shadow distance restored");
        }

        public void ScanDiagnostics()
        {
            try
            {
                int npcCount = ManagerCacheService.Instance.NPCRegistry?.Count ?? 0;
                var manager = ManagerCacheService.Instance.VehicleManager;
                int vehicleCount = manager?.AllVehicles?.Count ?? 0;
                OptimizedLight[] lights = UnityEngine.Object.FindObjectsOfType<OptimizedLight>(true);
                ReflectionProbeUpdater[] probes =
                    UnityEngine.Object.FindObjectsOfType<ReflectionProbeUpdater>(true);
                int activeLights = 0;
                for (int i = 0; i < lights.Length; i++)
                {
                    try
                    {
                        if (lights[i] != null && lights[i].Enabled && !lights[i].DisabledForOptimization)
                            activeLights++;
                    }
                    catch { }
                }

                _diagnostics = "NPCs " + npcCount + " | Vehicles " + vehicleCount +
                    " | Optimized lights " + activeLights + "/" + lights.Length +
                    " | Reflection updaters " + probes.Length;
            }
            catch (Exception ex)
            {
                _diagnostics = "Scene scan unavailable: " + ex.GetType().Name;
            }
        }

        public void ApplySmoothVisualsPreset()
        {
            SetVSync(false);
            SetTargetFps(120);
            SetLightRangeScale(0.75f);
            SetReflectionInterval(0.5f);
            SetLodBias(Mathf.Min(_defaultLodBias, 0.9f));
            TrySetGameSetting("GraphicsSettings", "SSAO", false);
            TrySetGameSetting("GraphicsSettings", "GodRays", false);
            TrySetEnumSetting("GraphicsSettings", "AntiAliasingMode", "FXAA", 1);
            NotificationService.Instance.Notify("Balanced performance preset applied");
        }

        public void ApplyLowImpactMenuPreset()
        {
            KeybindOverlayService.Instance.SetEnabled(false);
            SetVSync(false);
            SetTargetFps(120);
            SetLightRangeScale(0.75f);
            SetReflectionInterval(1f);
            NotificationService.Instance.Notify("Low-impact mode applied");
        }

        public void RestoreRuntimeDefaults()
        {
            RestoreLightRanges();
            SetReflectionInterval(0f);
            RestoreLodBias();
            RestoreShadowDistance();
            NotificationService.Instance.Notify("Runtime performance controls restored");
        }

        private void ApplyLightRangeBudget()
        {
            OptimizedLight[] lights;
            try { lights = UnityEngine.Object.FindObjectsOfType<OptimizedLight>(true); }
            catch { return; }

            for (int i = 0; i < lights.Length; i++)
            {
                OptimizedLight light = lights[i];
                if (light == null)
                    continue;

                try
                {
                    int id = light.GetInstanceID();
                    if (!_lightStates.TryGetValue(id, out LightState state))
                    {
                        state = new LightState
                        {
                            Light = light,
                            OriginalDistance = Mathf.Max(0f, light.MaxDistance)
                        };
                        _lightStates[id] = state;
                    }

                    float adjusted = state.OriginalDistance * _lightRangeScale;
                    light.MaxDistance = adjusted;
                    _lightDistanceSquaredField?.SetValue(light, adjusted * adjusted);
                }
                catch { }
            }
        }

        private void RestoreLightRanges()
        {
            foreach (LightState state in _lightStates.Values)
            {
                try
                {
                    if (state.Light == null)
                        continue;
                    state.Light.MaxDistance = state.OriginalDistance;
                    _lightDistanceSquaredField?.SetValue(
                        state.Light,
                        state.OriginalDistance * state.OriginalDistance);
                }
                catch { }
            }

            _lightStates.Clear();
            _lightRangeScale = 1f;
            _lightSceneHandle = -1;
        }

        private static bool TrySetGameSetting(string typeShortName, string memberName, object value)
        {
            Type type = FindGameType(typeShortName);
            if (type == null)
                return false;

            object instance = GetSingletonInstance(type);
            return TrySetMember(type, instance, memberName, value) ||
                TrySetMember(type, null, memberName, value);
        }

        private static bool TryReadDisplaySettings(out DisplaySettings display)
        {
            display = default;
            try
            {
                Settings settings = Settings.Instance;
                if (settings == null)
                    return false;
                display = settings.DisplaySettings;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ApplyNativeDisplaySettings(int? targetFps, bool? vSync)
        {
            try
            {
                Settings settings = Settings.Instance;
                if (settings == null)
                    return false;

                DisplaySettings display = settings.DisplaySettings;
                if (targetFps.HasValue)
                    display.TargetFPS = targetFps.Value;
                if (vSync.HasValue)
                    display.VSync = vSync.Value;

                settings.UnappliedDisplaySettings = display;
                settings.ApplyDisplaySettings(display);
                settings.WriteDisplaySettings(display);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Native display setting apply failed: " + ex.Message);
                return false;
            }
        }

        private static bool TrySetEnumSetting(string typeShortName, string memberName, string enumName, int fallbackValue)
        {
            Type type = FindGameType(typeShortName);
            if (type == null)
                return false;

            object instance = GetSingletonInstance(type);
            MemberInfo member = FindMember(type, memberName);
            Type memberType = GetMemberType(member);
            object value = fallbackValue;
            if (memberType != null && memberType.IsEnum)
            {
                try { value = Enum.Parse(memberType, enumName, true); }
                catch { value = Enum.ToObject(memberType, fallbackValue); }
            }

            return TrySetMember(type, instance, memberName, value) ||
                TrySetMember(type, null, memberName, value);
        }

        private static Type FindGameType(string shortName)
        {
            string[] names =
            {
                "Il2CppScheduleOne.DevUtilities." + shortName,
                "ScheduleOne.DevUtilities." + shortName,
                shortName
            };

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Assembly assembly = assemblies[a];
                if (assembly == null)
                    continue;

                for (int i = 0; i < names.Length; i++)
                {
                    try
                    {
                        Type type = assembly.GetType(names[i], false);
                        if (type != null)
                            return type;
                    }
                    catch { }
                }
            }

            return null;
        }

        private static object GetSingletonInstance(Type type)
        {
            if (type == null)
                return null;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.FlattenHierarchy;
            try { return type.GetProperty("Instance", flags)?.GetValue(null, null); }
            catch { }
            try { return type.GetField("Instance", flags)?.GetValue(null); }
            catch { }
            return null;
        }

        private static MemberInfo FindMember(Type type, string memberName)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            return (MemberInfo)type.GetProperty(memberName, flags) ?? type.GetField(memberName, flags);
        }

        private static Type GetMemberType(MemberInfo member)
        {
            if (member is PropertyInfo property)
                return property.PropertyType;
            return (member as FieldInfo)?.FieldType;
        }

        private static bool TrySetMember(Type type, object instance, string memberName, object value)
        {
            if (type == null)
                return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            try
            {
                PropertyInfo property = type.GetProperty(memberName, flags);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(instance, ConvertValue(value, property.PropertyType), null);
                    return true;
                }
            }
            catch { }

            try
            {
                FieldInfo field = type.GetField(memberName, flags);
                if (field != null)
                {
                    field.SetValue(instance, ConvertValue(value, field.FieldType));
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (targetType == null || value == null || targetType.IsInstanceOfType(value))
                return value;
            if (targetType.IsEnum)
                return Enum.ToObject(targetType, Convert.ToInt32(value));
            if (targetType == typeof(bool))
                return value is bool b ? b : Convert.ToInt32(value) != 0;
            if (targetType == typeof(int))
                return Convert.ToInt32(value);
            if (targetType == typeof(float))
                return Convert.ToSingle(value);
            return value;
        }
    }
}
