using System;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Trash;
using Il2CppScheduleOne.Variables;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class CompatibilityService
    {
        private static readonly CompatibilityService _instance = new CompatibilityService();
        public static CompatibilityService Instance => _instance;

        private bool _unityLogFilterPatched;

        private CompatibilityService() { }

        public void ApplyRuntimeCompatibilityFixes(HarmonyLib.Harmony harmony)
        {
            ApplyUnityLogFilter(harmony);
        }

        private void ApplyUnityLogFilter(HarmonyLib.Harmony harmony)
        {
            if (_unityLogFilterPatched || harmony == null)
                return;

            try
            {
                MethodInfo prefix = typeof(CompatibilityService).GetMethod(
                    nameof(UnityLoggerLogPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (prefix != null)
                {
                    MethodInfo[] methods = typeof(UnityEngine.Logger).GetMethods(
                        BindingFlags.Instance | BindingFlags.Public);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (method == null || method.Name != "Log")
                            continue;

                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length < 2 || parameters[0].ParameterType != typeof(LogType))
                            continue;

                        harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                    }
                }

                _unityLogFilterPatched = true;
            }
            catch
            {
                _unityLogFilterPatched = true;
            }
        }

        private static bool UnityLoggerLogPrefix(object[] __args)
        {
            if (__args == null)
                return true;

            for (int i = 0; i < __args.Length; i++)
            {
                if (ShouldSuppressUnityLog(__args[i]))
                    return false;
            }

            return true;
        }

        private static bool ShouldSuppressUnityLog(object message)
        {
            return ShouldSuppressMissingVariableLog(message) ||
                ShouldSuppressNegativeBoxColliderLog(message);
        }

        private static bool ShouldSuppressMissingVariableLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(text))
                return false;

            if (text.IndexOf("Failed to find variable with name:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ContainsIgnoredMissingVariableName(text) ||
                    ContainsMissingInventoryVariable(text);
            }

            return text.StartsWith("Variable with name inventory", StringComparison.OrdinalIgnoreCase) &&
                text.IndexOf("does not exist in the database", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsIgnoredMissingVariableName(string variableName)
        {
            return string.Equals(variableName, "cash_balance", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(variableName, "total_money", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(variableName, "player_in_vehicle", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldSuppressNegativeBoxColliderLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf("BoxCollider does not support negative scale or size", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsIgnoredMissingVariableName(string text)
        {
            return text.IndexOf("cash_balance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("total_money", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("player_in_vehicle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsMissingInventoryVariable(string text)
        {
            int marker = text.IndexOf("name:", StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
                return false;

            string variableName = text.Substring(marker + 5).Trim();
            return variableName.StartsWith("inventory", StringComparison.OrdinalIgnoreCase);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetVariableValue), new[] { typeof(string), typeof(string), typeof(bool) })]
    internal static class PlayerMissingVariableSetSpamPatch
    {
        private static bool _reported;

        private static bool Prefix(string variableName)
        {
            if (!CompatibilityService.IsIgnoredMissingVariableName(variableName))
                return true;

            if (!_reported)
            {
                _reported = true;
                DebugLogService.Instance.VerboseWarning(
                    "Suppressed Player.SetVariableValue missing-variable warning spam");
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetVariable), new[] { typeof(string) })]
    internal static class PlayerGetMissingVariableSpamPatch
    {
        private static bool _reported;

        private static bool Prefix(string variableName, ref BaseVariable __result)
        {
            if (!CompatibilityService.IsIgnoredMissingVariableName(variableName))
                return true;

            __result = null;
            if (!_reported)
            {
                _reported = true;
                DebugLogService.Instance.VerboseWarning(
                    "Suppressed Player.GetVariable missing-variable warning spam");
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(VariableDatabase), nameof(VariableDatabase.GetVariable), new[] { typeof(string) })]
    internal static class VariableDatabaseGetMissingVariableSpamPatch
    {
        private static bool _reported;

        private static bool Prefix(string variableName, ref BaseVariable __result)
        {
            if (!CompatibilityService.IsIgnoredMissingVariableName(variableName))
                return true;

            __result = null;
            if (!_reported)
            {
                _reported = true;
                DebugLogService.Instance.VerboseWarning(
                    "Suppressed VariableDatabase.GetVariable missing-variable warning spam");
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(VariableDatabase), nameof(VariableDatabase.SetVariableValue), new[] { typeof(string), typeof(string), typeof(bool) })]
    internal static class VariableDatabaseSetMissingVariableSpamPatch
    {
        private static bool _reported;

        private static bool Prefix(VariableDatabase __instance, string variableName)
        {
            if (!CompatibilityService.IsIgnoredMissingVariableName(variableName))
                return true;

            if (!_reported)
            {
                _reported = true;
                DebugLogService.Instance.VerboseWarning(
                    "Suppressed VariableDatabase.SetVariableValue missing-variable warning spam");
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(TrashItem), "MinPass")]
    internal static class TrashItemMinPassSafetyPatch
    {
        private static int _suppressedCount;
        private static int _skippedVehicleTrashCount;
        private static float _nextVerboseLogTime;
        private static bool _reportedVehicleTrashSkip;
        private static bool _reportedSuppressedException;

        private static bool Prefix(TrashItem __instance)
        {
            if (!IsAttachedToVehicle(__instance))
                return true;

            _skippedVehicleTrashCount++;
            if (!_reportedVehicleTrashSkip)
            {
                _reportedVehicleTrashSkip = true;
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Skipping vehicle-attached TrashItem.MinPass to prevent police/NPC vehicle crash spam");
            }

            if (DebugLogService.Instance.VerboseEnabled &&
                Time.unscaledTime >= _nextVerboseLogTime)
            {
                _nextVerboseLogTime = Time.unscaledTime + 30f;
                DebugLogService.Instance.VerboseWarning(
                    "Skipped vehicle-attached TrashItem.MinPass x" +
                    _skippedVehicleTrashCount + " on " + SafeObjectName(__instance));
            }

            return false;
        }

        private static Exception Finalizer(TrashItem __instance, Exception __exception)
        {
            if (__exception == null)
                return null;

            if (!ShouldSuppressTrashMinPassException(__exception))
                return __exception;

            _suppressedCount++;
            if (!_reportedSuppressedException)
            {
                _reportedSuppressedException = true;
                UnityEngine.Debug.LogWarning(
                    "[Nugzz] Suppressed TrashItem.MinPass null-ref spam from an invalid trash item");
            }

            if (DebugLogService.Instance.VerboseEnabled &&
                Time.unscaledTime >= _nextVerboseLogTime)
            {
                _nextVerboseLogTime = Time.unscaledTime + 30f;
                DebugLogService.Instance.VerboseWarning(
                    "Suppressed TrashItem.MinPass exception x" + _suppressedCount +
                    " on " + SafeObjectName(__instance) + ": " + __exception.Message);
            }

            return null;
        }

        private static bool ShouldSuppressTrashMinPassException(Exception exception)
        {
            if (exception == null)
                return false;

            if (exception is NullReferenceException)
                return true;

            string text = exception.ToString();
            return text.IndexOf("TrashItem.MinPass", StringComparison.OrdinalIgnoreCase) >= 0 &&
                text.IndexOf("NullReferenceException", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAttachedToVehicle(TrashItem item)
        {
            if (item == null)
                return false;

            try
            {
                return item.GetComponentInParent<LandVehicle>(true) != null;
            }
            catch
            {
                return false;
            }
        }

        private static string SafeObjectName(TrashItem item)
        {
            try
            {
                return item != null ? item.name : "null";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}
