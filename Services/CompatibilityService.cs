using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Trash;
using Il2CppScheduleOne.Variables;
using Il2CppScheduleOne.Vehicles;
using MelonLoader;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class CompatibilityService
    {
        private static readonly CompatibilityService _instance = new CompatibilityService();
        public static CompatibilityService Instance => _instance;

        private bool _unityLogFilterPatched;
        private bool _actionListStaggeredPatched;

        private CompatibilityService() { }

        public void ApplyRuntimeCompatibilityFixes(HarmonyLib.Harmony harmony)
        {
            ApplyUnityLogFilter(harmony);
            ApplyActionListStaggeredPatch(harmony);
        }

        private void ApplyUnityLogFilter(HarmonyLib.Harmony harmony)
        {
            if (_unityLogFilterPatched || harmony == null)
                return;

            try
            {
                MethodInfo firstArgumentPrefix = typeof(CompatibilityService).GetMethod(
                    nameof(UnityLogFirstArgumentPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo secondArgumentPrefix = typeof(CompatibilityService).GetMethod(
                    nameof(UnityLogSecondArgumentPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);

                PatchUnityLogMethods(harmony, typeof(UnityEngine.Logger), firstArgumentPrefix, secondArgumentPrefix);
                PatchUnityLogMethods(harmony, typeof(UnityEngine.Debug), firstArgumentPrefix, secondArgumentPrefix);

                _unityLogFilterPatched = true;
            }
            catch
            {
                _unityLogFilterPatched = true;
            }
        }

        private static void PatchUnityLogMethods(
            HarmonyLib.Harmony harmony,
            Type type,
            MethodInfo firstArgumentPrefix,
            MethodInfo secondArgumentPrefix)
        {
            if (harmony == null || type == null)
                return;

            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (!ShouldPatchUnityLogMethod(method))
                    continue;

                MethodInfo prefix = SelectUnityLogPrefix(
                    method,
                    firstArgumentPrefix,
                    secondArgumentPrefix);
                if (prefix == null)
                    continue;

                try
                {
                    harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                }
                catch { }
            }
        }

        private static bool ShouldPatchUnityLogMethod(MethodInfo method)
        {
            if (method == null)
                return false;

            return method.Name == "Log" ||
                method.Name == "LogWarning" ||
                method.Name == "LogError";
        }

        private static MethodInfo SelectUnityLogPrefix(
            MethodInfo method,
            MethodInfo firstArgumentPrefix,
            MethodInfo secondArgumentPrefix)
        {
            ParameterInfo[] parameters = method?.GetParameters();
            if (parameters == null || parameters.Length == 0)
                return null;

            for (int i = 0; i < parameters.Length; i++)
            {
                string name = parameters[i]?.Name ?? string.Empty;
                if (string.Equals(name, "message", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "msg", StringComparison.OrdinalIgnoreCase))
                {
                    return i == 0 ? firstArgumentPrefix :
                        i == 1 ? secondArgumentPrefix : null;
                }
            }

            if (parameters[0].ParameterType == typeof(LogType) ||
                parameters[0].ParameterType == typeof(string))
            {
                return parameters.Length > 1 ? secondArgumentPrefix : null;
            }

            return firstArgumentPrefix;
        }

        private static bool UnityLogFirstArgumentPrefix(object __0)
        {
            return !ShouldSuppressUnityLog(__0);
        }

        private static bool UnityLogSecondArgumentPrefix(object __1)
        {
            return !ShouldSuppressUnityLog(__1);
        }

        internal static bool ShouldSuppressUnityLog(object message)
        {
            return ShouldSuppressMissingVariableLog(message) ||
                ShouldSuppressNegativeBoxColliderLog(message) ||
                ShouldSuppressActionListStaggeredLog(message) ||
                ShouldSuppressNavMeshAgentLog(message);
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
                string.Equals(variableName, "player_in_vehicle", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(variableName, "inputhintstutorialdone", StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(variableName) &&
                 variableName.StartsWith("inventory", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ShouldSuppressNegativeBoxColliderLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf("BoxCollider does not support negative scale or size", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldSuppressActionListStaggeredLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf("Error invoking StaggeredInvoke", StringComparison.OrdinalIgnoreCase) >= 0 &&
                text.IndexOf("Index was out of range", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldSuppressNavMeshAgentLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf("Failed to create agent because it is not close enough to the NavMesh", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ApplyActionListStaggeredPatch(HarmonyLib.Harmony harmony)
        {
            if (_actionListStaggeredPatched || harmony == null)
                return;

            try
            {
                Type actionListType = FindGameType("ActionList");
                MethodInfo target = actionListType?.GetMethod(
                    "InvokeAllStaggered",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(float) },
                    null);
                MethodInfo prefix = typeof(CompatibilityService).GetMethod(
                    nameof(ActionListInvokeAllStaggeredPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (target != null && prefix != null)
                    harmony.Patch(target, prefix: new HarmonyMethod(prefix));
            }
            catch { }

            _actionListStaggeredPatched = true;
        }

        private static Type FindGameType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            try
            {
                Type type = typeof(Player).Assembly.GetType(typeName, false);
                if (type != null)
                    return type;
            }
            catch { }

            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Assembly assembly = assemblies[i];
                    string name = assembly?.GetName()?.Name ?? string.Empty;
                    if (!string.Equals(name, "Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                        continue;

                    Type type = assembly.GetType(typeName, false);
                    if (type != null)
                        return type;
                }
            }
            catch { }

            return null;
        }

        private static bool ActionListInvokeAllStaggeredPrefix(object __instance, float __0)
        {
            try
            {
                object[] callbacks = CollectActionListCallbacks(__instance);
                if (callbacks == null)
                    return true;
                if (callbacks.Length == 0)
                    return false;

                MelonCoroutines.Start(InvokeActionListSnapshot(callbacks, __0));
                return false;
            }
            catch
            {
                return true;
            }
        }

        private static object[] CollectActionListCallbacks(object actionList)
        {
            if (actionList == null)
                return null;

            MethodInfo getter = actionList.GetType().GetMethod(
                "GetInvocationList",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object rawList = getter?.Invoke(actionList, null);
            IEnumerable enumerable = rawList as IEnumerable;
            if (enumerable == null)
                return null;

            var callbacks = new List<object>();
            foreach (object callback in enumerable)
            {
                if (callback != null)
                    callbacks.Add(callback);
            }

            return callbacks.ToArray();
        }

        private static IEnumerator InvokeActionListSnapshot(object[] callbacks, float staggerTime)
        {
            if (callbacks == null || callbacks.Length == 0)
                yield break;

            float delay = callbacks.Length > 1
                ? Mathf.Max(0f, staggerTime) / callbacks.Length
                : 0f;

            for (int i = 0; i < callbacks.Length; i++)
            {
                try
                {
                    InvokeActionListCallback(callbacks[i]);
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning(
                        "Suppressed ActionList staggered callback error: " + ex.Message);
                }

                if (delay > 0f && i + 1 < callbacks.Length)
                    yield return new WaitForSeconds(delay);
                else
                    yield return null;
            }
        }

        private static void InvokeActionListCallback(object callback)
        {
            if (callback == null)
                return;

            Action action = callback as Action;
            if (action != null)
            {
                action();
                return;
            }

            Delegate del = callback as Delegate;
            if (del != null)
            {
                del.DynamicInvoke();
                return;
            }

            MethodInfo invoke = callback.GetType().GetMethod(
                "Invoke",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            invoke?.Invoke(callback, null);
        }

        private static bool ContainsIgnoredMissingVariableName(string text)
        {
            return text.IndexOf("cash_balance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("total_money", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("player_in_vehicle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("inputhintstutorialdone", StringComparison.OrdinalIgnoreCase) >= 0;
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
