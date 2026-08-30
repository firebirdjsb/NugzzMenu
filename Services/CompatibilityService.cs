using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NugzzMenu.Services
{
    public sealed class CompatibilityService
    {
        private static readonly CompatibilityService _instance = new CompatibilityService();
        public static CompatibilityService Instance => _instance;

        private bool _unityLogFilterPatched;
        private bool _actionListStaggeredPatched;
        private bool _temperatureDisplayPatched;
        private bool _cookingBurnerInputPatched;
        [ThreadStatic]
        private static bool _temperatureDisplayUpdateActive;
        private static Camera _temperatureCamera;
        private static int _temperatureCameraFrame = -1;

        private CompatibilityService() { }

        public void ApplyRuntimeCompatibilityFixes(HarmonyLib.Harmony harmony)
        {
            ApplyActionListStaggeredPatch(harmony);
            ApplyTemperatureDisplayPatch(harmony);
            ApplyCookingBurnerInputPatch(harmony);
        }

        private void ApplyUnityLogFilter(HarmonyLib.Harmony harmony)
        {
            if (_unityLogFilterPatched || harmony == null)
                return;

            try
            {
                MethodInfo il2CppContextPrefix = typeof(CompatibilityService).GetMethod(
                    nameof(UnityIl2CppLogSecondArgumentPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo loggerContextLog = typeof(UnityEngine.Logger).GetMethod(
                    "Log",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[]
                    {
                        typeof(LogType),
                        typeof(Il2CppSystem.Object),
                        typeof(UnityEngine.Object)
                    },
                    null);

                if (loggerContextLog != null && il2CppContextPrefix != null)
                {
                    try
                    {
                        harmony.Patch(
                            loggerContextLog,
                            prefix: new HarmonyMethod(il2CppContextPrefix));
                    }
                    catch { }
                }

                _unityLogFilterPatched = true;
            }
            catch
            {
                _unityLogFilterPatched = true;
            }
        }

        private static bool UnityIl2CppLogSecondArgumentPrefix(Il2CppSystem.Object __1)
        {
            try
            {
                return !ShouldSuppressUnityLog(UnityEngine.Logger.GetString(__1));
            }
            catch
            {
                return !ShouldSuppressUnityLog(__1);
            }
        }

        internal static bool ShouldSuppressUnityLog(object message)
        {
            return ShouldSuppressMissingVariableLog(message) ||
                ShouldSuppressTemperatureDisplayLookRotationLog(message) ||
                ShouldSuppressNegativeBoxColliderLog(message) ||
                ShouldSuppressActionListStaggeredLog(message) ||
                ShouldSuppressNavMeshAgentLog(message) ||
                ShouldSuppressPathFailureLog(message);
        }

        private static bool ShouldSuppressTemperatureDisplayLookRotationLog(object message)
        {
            if (!_temperatureDisplayUpdateActive)
                return false;

            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf("Look rotation viewing vector is zero", StringComparison.OrdinalIgnoreCase) >= 0;
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

            if (text.StartsWith("Variable ", StringComparison.OrdinalIgnoreCase) &&
                text.EndsWith(" not found", StringComparison.OrdinalIgnoreCase))
            {
                return ContainsIgnoredMissingVariableName(text);
            }

            return text.StartsWith("Variable with name inventory", StringComparison.OrdinalIgnoreCase) &&
                text.IndexOf("does not exist in the database", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldSuppressNegativeBoxColliderLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf("BoxCollider does not support negative scale or size", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldSuppressActionListStaggeredLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf(
                "Error invoking StaggeredInvoke",
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldSuppressNavMeshAgentLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf("Failed to create agent because it is not close enough to the NavMesh", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldSuppressPathFailureLog(object message)
        {
            string text = message?.ToString() ?? string.Empty;
            return text.IndexOf("Path Failed : Computation Time", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Searched all reachable nodes, but could not find target", StringComparison.OrdinalIgnoreCase) >= 0;
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
            catch (System.NotSupportedException ex)
            {
                DebugLogService.Instance.Verbose("ActionList.InvokeAllStaggered patch skipped (method stripped): " + ex.Message);
            }
            catch (System.Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("ActionList.InvokeAllStaggered patch failed: " + ex.Message);
            }

            _actionListStaggeredPatched = true;
        }

        private void ApplyTemperatureDisplayPatch(HarmonyLib.Harmony harmony)
        {
            if (_temperatureDisplayPatched || harmony == null)
                return;

            try
            {
                Type type = FindGameType("ScheduleOne.UI.TemperatureDisplay") ??
                    FindGameType("Il2CppScheduleOne.UI.TemperatureDisplay") ??
                    AccessTools.TypeByName("ScheduleOne.UI.TemperatureDisplay") ??
                    AccessTools.TypeByName("Il2CppScheduleOne.UI.TemperatureDisplay");
                MethodInfo target = AccessTools.Method(type, "UpdateCanvas");
                MethodInfo prefix = typeof(CompatibilityService).GetMethod(
                    nameof(TemperatureDisplayUpdateCanvasPrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo finalizer = typeof(CompatibilityService).GetMethod(
                    nameof(TemperatureDisplayUpdateCanvasFinalizer),
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (target != null && prefix != null && finalizer != null)
                {
                    harmony.Patch(
                        target,
                        prefix: new HarmonyMethod(prefix),
                        finalizer: new HarmonyMethod(finalizer));
                    DebugLogService.Instance.Verbose("Patched TemperatureDisplay.UpdateCanvas zero-vector guard");
                }
            }
            catch (System.NotSupportedException ex)
            {
                DebugLogService.Instance.Verbose("TemperatureDisplay.UpdateCanvas patch skipped (method stripped): " + ex.Message);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Temperature display compatibility patch failed: " + ex.Message);
            }

            _temperatureDisplayPatched = true;
        }

        private void ApplyCookingBurnerInputPatch(HarmonyLib.Harmony harmony)
        {
            if (_cookingBurnerInputPatched || harmony == null)
                return;

            try
            {
                MethodInfo target = AccessTools.Method(typeof(BunsenBurner), "Update");
                MethodInfo prefix = typeof(CompatibilityService).GetMethod(
                    nameof(CookingBurnerUpdatePrefix),
                    BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo postfix = typeof(CompatibilityService).GetMethod(
                    nameof(CookingBurnerUpdatePostfix),
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (target != null && prefix != null && postfix != null)
                {
                    harmony.Patch(
                        target,
                        prefix: new HarmonyMethod(prefix),
                        postfix: new HarmonyMethod(postfix));
                    DebugLogService.Instance.Verbose(
                        "Patched BunsenBurner.Update native temperature-input fallback");
                }
            }
            catch (System.NotSupportedException ex)
            {
                DebugLogService.Instance.Verbose(
                    "BunsenBurner.Update patch skipped (method stripped): " + ex.Message);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Cooking burner input patch failed: " + ex.Message);
            }

            _cookingBurnerInputPatched = true;
        }

        private static void CookingBurnerUpdatePrefix(
            BunsenBurner __instance,
            out float __state)
        {
            __state = float.NaN;
            try
            {
                if (__instance != null)
                    __state = __instance.CurrentDialValue;
            }
            catch { }
        }

        private static void CookingBurnerUpdatePostfix(
            BunsenBurner __instance,
            float __state)
        {
            try
            {
                if (__instance == null || float.IsNaN(__state) ||
                    !__instance.Interactable || __instance.LockDial)
                    return;

                bool held = __instance.IsDialHeld;
                if (!held && __instance.HandleClickable != null)
                    held = __instance.HandleClickable.IsHeld;
                if (!held)
                    return;

                float current = __instance.CurrentDialValue;
                if (!Mathf.Approximately(current, __state))
                    return;

                Vector2 pointerDelta = GameInput.MouseDelta;
                if (pointerDelta.sqrMagnitude <= 0.0001f && Mouse.current != null)
                    pointerDelta = Mouse.current.delta.ReadValue();

                float directionalDelta = Mathf.Abs(pointerDelta.x) >= Mathf.Abs(pointerDelta.y)
                    ? pointerDelta.x
                    : -pointerDelta.y;
                float dialDelta;
                if (Mathf.Abs(directionalDelta) > 0.01f)
                {
                    float dragDistance = Mathf.Max(280f, Screen.width * 0.2f);
                    float speedScale = Mathf.Clamp(
                        __instance.HandleRotationSpeed / 100f,
                        0.5f,
                        2f);
                    dialDelta = directionalDelta * speedScale / dragDistance;
                }
                else if (GameInput.GetCurrentInputDeviceIsGamepad())
                {
                    float axis = GameInput.CameraAxis.x;
                    if (Mathf.Abs(axis) <= 0.05f)
                        return;
                    dialDelta = axis * Time.unscaledDeltaTime * 0.65f;
                }
                else
                {
                    return;
                }

                __instance.SetDialPosition(Mathf.Clamp01(current + dialDelta));
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Cooking burner temperature input fallback failed: " + ex.Message);
            }
        }

        private static bool TemperatureDisplayUpdateCanvasPrefix(object __instance)
        {
            if (ShouldSkipTemperatureDisplayCanvas(__instance))
            {
                _temperatureDisplayUpdateActive = false;
                return false;
            }

            _temperatureDisplayUpdateActive = true;
            return true;
        }

        private static Exception TemperatureDisplayUpdateCanvasFinalizer(Exception __exception)
        {
            _temperatureDisplayUpdateActive = false;
            return __exception;
        }

        private static bool ShouldSkipTemperatureDisplayCanvas(object instance)
        {
            try
            {
                Camera camera = GetTemperatureCamera();
                if (camera == null || camera.transform == null)
                    return true;

                Component component = instance as Component;
                if (component == null || component.transform == null)
                    return false;

                Vector3 cameraPosition = camera.transform.position;
                if (!IsFinite(cameraPosition))
                    return true;

                if (IsZeroLookVector(cameraPosition, component.transform.position))
                    return true;
            }
            catch { }

            return false;
        }

        private static Camera GetTemperatureCamera()
        {
            int frame = Time.frameCount;
            if (_temperatureCameraFrame == frame && _temperatureCamera != null)
                return _temperatureCamera;

            _temperatureCameraFrame = frame;
            try { _temperatureCamera = PlayerCamera.Instance?.Camera; }
            catch { _temperatureCamera = null; }
            if (_temperatureCamera == null)
            {
                try { _temperatureCamera = Camera.main; }
                catch { }
            }
            return _temperatureCamera;
        }

        private static bool IsZeroLookVector(Vector3 cameraPosition, Vector3 displayPosition)
        {
            if (!IsFinite(displayPosition))
                return true;

            Vector3 delta = cameraPosition - displayPosition;
            if (!IsFinite(delta))
                return true;

            if (delta.sqrMagnitude <= 0.0001f)
                return true;

            delta.y = 0f;
            return delta.sqrMagnitude <= 0.0001f;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) && !float.IsInfinity(value.z);
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
            var callbacks = new List<object>();
            if (enumerable != null)
            {
                foreach (object callback in enumerable)
                {
                    if (callback != null)
                        callbacks.Add(callback);
                }
                return callbacks.ToArray();
            }

            if (rawList == null)
                return null;

            Type listType = rawList.GetType();
            PropertyInfo countProperty = listType.GetProperty("Count");
            PropertyInfo itemProperty = listType.GetProperty("Item");
            MethodInfo itemGetter = itemProperty?.GetGetMethod() ??
                listType.GetMethod("get_Item");
            if (countProperty == null || itemGetter == null)
                return null;

            int count = Convert.ToInt32(countProperty.GetValue(rawList));
            for (int i = 0; i < count; i++)
            {
                object callback = itemGetter.Invoke(rawList, new object[] { i });
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
                text.IndexOf("playernearrv", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("PlayerNearRV", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Nugzz.VehicleTune", StringComparison.OrdinalIgnoreCase) >= 0 ||
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

}
