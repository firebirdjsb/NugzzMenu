using System;
using System.Reflection;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class PerformanceService
    {
        private static readonly PerformanceService _instance = new PerformanceService();
        public static PerformanceService Instance => _instance;

        private PerformanceService() { }

        public int TargetFps => Application.targetFrameRate <= 0 ? 0 : Application.targetFrameRate;
        public bool VSyncEnabled => QualitySettings.vSyncCount > 0;

        public string GetSummary()
        {
            float fps = Time.smoothDeltaTime > 0.0001f ? 1f / Time.smoothDeltaTime : 0f;
            string cap = TargetFps <= 0 ? "Unlimited" : TargetFps.ToString();
            return "FPS " + fps.ToString("0") + " | Cap " + cap + " | VSync " + (VSyncEnabled ? "ON" : "OFF");
        }

        public void SetTargetFps(int fps)
        {
            int target = fps <= 0 ? -1 : Mathf.Clamp(fps, 30, 240);
            Application.targetFrameRate = target;
            if (fps > 0)
                SetVSync(false);

            TrySetGameSetting("DisplaySettings", "TargetFPS", target);
            NotificationService.Instance.Status(fps <= 0 ? "FPS cap: Unlimited" : "FPS cap: " + fps);
        }

        public void SetVSync(bool enabled)
        {
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            TrySetGameSetting("DisplaySettings", "VSync", enabled);
            NotificationService.Instance.Status(enabled ? "VSync ON" : "VSync OFF");
        }

        public void ApplySmoothVisualsPreset()
        {
            SetVSync(false);
            SetTargetFps(120);
            TrySetGameSetting("GraphicsSettings", "SSAO", false);
            TrySetGameSetting("GraphicsSettings", "GodRays", false);
            TrySetEnumSetting("GraphicsSettings", "AntiAliasingMode", "FXAA", 1);
            NotificationService.Instance.Notify("Smooth visuals preset applied");
        }

        public void ApplyLowImpactMenuPreset()
        {
            KeybindOverlayService.Instance.SetEnabled(false);
            SetVSync(false);
            SetTargetFps(120);
            NotificationService.Instance.Notify("Low-impact menu mode applied");
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

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            try { return type.GetProperty("Instance", flags)?.GetValue(null, null); }
            catch { }
            try { return type.GetField("Instance", flags)?.GetValue(null); }
            catch { }
            return null;
        }

        private static MemberInfo FindMember(Type type, string memberName)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            return (MemberInfo)type.GetProperty(memberName, flags) ?? type.GetField(memberName, flags);
        }

        private static Type GetMemberType(MemberInfo member)
        {
            PropertyInfo property = member as PropertyInfo;
            if (property != null)
                return property.PropertyType;
            FieldInfo field = member as FieldInfo;
            return field?.FieldType;
        }

        private static bool TrySetMember(Type type, object instance, string memberName, object value)
        {
            if (type == null)
                return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
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
            if (targetType == null || value == null)
                return value;
            if (targetType.IsInstanceOfType(value))
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
