using HarmonyLib;
using Il2CppScheduleOne.Lighting;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(ReflectionProbeUpdater), "UpdateProbe")]
    internal static class ReflectionProbeUpdateThrottlePatch
    {
        private static bool Prefix(ReflectionProbeUpdater __instance)
        {
            return PerformanceService.Instance.ShouldUpdateReflectionProbe(__instance);
        }
    }
}
