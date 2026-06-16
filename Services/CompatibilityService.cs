using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.Persistence;

namespace NugzzMenu.Services
{
    public sealed class CompatibilityService
    {
        private static readonly CompatibilityService _instance = new CompatibilityService();
        public static CompatibilityService Instance => _instance;

        private bool _cartelSavePatchChecked;
        private int _cartelSavePatchScanAttempts;
        private float _nextCartelSavePatchScanTime;
        private bool _cartelExitPatchChecked;
        private int _cartelExitPatchScanAttempts;
        private float _nextCartelExitPatchScanTime;

        private CompatibilityService() { }

        public void ApplyRuntimeCompatibilityFixes(HarmonyLib.Harmony harmony)
        {
            DisableBrokenCartelEnforcerPrefix(
                harmony,
                typeof(SaveManager),
                nameof(SaveManager.Save),
                new[] { typeof(string) },
                ref _cartelSavePatchChecked,
                ref _cartelSavePatchScanAttempts,
                ref _nextCartelSavePatchScanTime,
                "SaveManager.Save",
                "save");
            DisableBrokenCartelEnforcerPrefix(
                harmony,
                typeof(LoadManager),
                nameof(LoadManager.ExitToMenu),
                null,
                ref _cartelExitPatchChecked,
                ref _cartelExitPatchScanAttempts,
                ref _nextCartelExitPatchScanTime,
                "LoadManager.ExitToMenu",
                "exit");
        }

        public void Update(HarmonyLib.Harmony harmony)
        {
            if (harmony == null)
                return;

            if (!_cartelSavePatchChecked &&
                UnityEngine.Time.unscaledTime >= _nextCartelSavePatchScanTime)
            {
                DisableBrokenCartelEnforcerPrefix(
                    harmony,
                    typeof(SaveManager),
                    nameof(SaveManager.Save),
                    new[] { typeof(string) },
                    ref _cartelSavePatchChecked,
                    ref _cartelSavePatchScanAttempts,
                    ref _nextCartelSavePatchScanTime,
                    "SaveManager.Save",
                    "save");
            }

            if (!_cartelExitPatchChecked &&
                UnityEngine.Time.unscaledTime >= _nextCartelExitPatchScanTime)
            {
                DisableBrokenCartelEnforcerPrefix(
                    harmony,
                    typeof(LoadManager),
                    nameof(LoadManager.ExitToMenu),
                    null,
                    ref _cartelExitPatchChecked,
                    ref _cartelExitPatchScanAttempts,
                    ref _nextCartelExitPatchScanTime,
                    "LoadManager.ExitToMenu",
                    "exit");
            }
        }

        private void DisableBrokenCartelEnforcerPrefix(
            HarmonyLib.Harmony harmony,
            Type targetType,
            string methodName,
            Type[] argumentTypes,
            ref bool checkedFlag,
            ref int scanAttempts,
            ref float nextScanTime,
            string targetLabel,
            string guardLabel)
        {
            if (checkedFlag || harmony == null)
                return;

            scanAttempts++;
            nextScanTime = UnityEngine.Time.unscaledTime + 1f;
            try
            {
                MethodInfo original = argumentTypes == null
                    ? AccessTools.Method(targetType, methodName)
                    : AccessTools.Method(targetType, methodName, argumentTypes);
                if (original == null)
                {
                    checkedFlag = true;
                    return;
                }

                Patches patchInfo = HarmonyLib.Harmony.GetPatchInfo(original);
                if (patchInfo?.Prefixes == null)
                {
                    checkedFlag = scanAttempts > 20;
                    return;
                }

                int removed = 0;
                foreach (Patch prefix in patchInfo.Prefixes.ToArray())
                {
                    MethodInfo patchMethod = prefix.PatchMethod;
                    string owner = prefix.owner ?? string.Empty;
                    string declaringType = patchMethod?.DeclaringType?.FullName ?? string.Empty;
                    if (!LooksLikeCartelEnforcerPatch(owner, declaringType))
                        continue;

                    harmony.Unpatch(original, patchMethod);
                    removed++;
                }

                if (removed > 0)
                {
                    checkedFlag = true;
                    DebugLogService.Instance.VerboseWarning(
                        "Disabled CartelEnforcer " + targetLabel + " prefix for compatibility");
                    NotificationService.Instance.Notify(
                        "CartelEnforcer " + guardLabel + " crash guard enabled");
                    return;
                }

                checkedFlag = scanAttempts > 20;
            }
            catch (Exception ex)
            {
                checkedFlag = scanAttempts > 20;
                DebugLogService.Instance.VerboseWarning(
                    "CartelEnforcer " + targetLabel + " compatibility scan failed: " + ex.Message);
            }
        }

        private static bool LooksLikeCartelEnforcerPatch(string owner, string declaringType)
        {
            return owner.IndexOf("cartel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                declaringType.IndexOf("CartelEnforcer", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    [HarmonyPatch(typeof(LoadManager), nameof(LoadManager.ExitToMenu))]
    internal static class ExternalExitToMenuHookSafetyPatch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception == null || !ExternalPatchExceptionGuard.IsCartelEnforcerException(__exception))
                return __exception;

            DebugLogService.Instance.VerboseWarning(
                "Suppressed CartelEnforcer exit-hook exception: " + __exception.Message);
            NotificationService.Instance.Notify(
                "Suppressed CartelEnforcer exit-hook error");
            return null;
        }
    }

    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.Save), new[] { typeof(string) })]
    internal static class ExternalSaveHookSafetyPatch
    {
        private static Exception Finalizer(Exception __exception)
        {
            if (__exception == null || !ExternalPatchExceptionGuard.IsCartelEnforcerException(__exception))
                return __exception;

            DebugLogService.Instance.VerboseWarning(
                "Suppressed CartelEnforcer save-hook exception: " + __exception.Message);
            NotificationService.Instance.Notify(
                "Suppressed CartelEnforcer save-hook error");
            return null;
        }
    }

    internal static class ExternalPatchExceptionGuard
    {
        public static bool IsCartelEnforcerException(Exception exception)
        {
            string text = exception.ToString();
            return text.IndexOf("CartelEnforcer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Cartel Enforcer", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
