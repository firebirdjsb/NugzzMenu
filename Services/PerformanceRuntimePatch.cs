using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.Lighting;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Installs the reflection-probe hook only while throttling is enabled. Keeping this
    /// detached in native mode avoids an IL2CPP-to-managed transition for every probe update.
    /// </summary>
    internal static class ReflectionProbeUpdateThrottlePatch
    {
        private const string HarmonyId = "com.xunfairx.nugzzmenu.reflection-throttle";
        private static HarmonyLib.Harmony _harmony;

        internal static bool Installed => _harmony != null;

        internal static bool SetInstalled(bool installed)
        {
            if (installed == Installed)
                return true;

            try
            {
                if (!installed)
                {
                    _harmony.UnpatchSelf();
                    _harmony = null;
                    return true;
                }

                MethodBase target = AccessTools.Method(typeof(ReflectionProbeUpdater), "UpdateProbe");
                MethodInfo prefix = AccessTools.Method(
                    typeof(ReflectionProbeUpdateThrottlePatch), nameof(Prefix));
                if (target == null || prefix == null)
                    return false;

                _harmony = new HarmonyLib.Harmony(HarmonyId);
                _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                return true;
            }
            catch (System.NotSupportedException ex)
            {
                DebugLogService.Instance.Verbose("ReflectionProbeUpdater.UpdateProbe patch skipped (method stripped): " + ex.Message);
                try { _harmony?.UnpatchSelf(); } catch { }
                _harmony = null;
                return false;
            }
            catch (System.Exception ex)
            {
                try { _harmony?.UnpatchSelf(); } catch { }
                _harmony = null;
                DebugLogService.Instance.VerboseWarning("ReflectionProbeUpdater.UpdateProbe patch failed: " + ex.Message);
                return false;
            }
        }

        private static bool Prefix(ReflectionProbeUpdater __instance)
        {
            return PerformanceService.Instance.ShouldUpdateReflectionProbe(__instance);
        }
    }
}
