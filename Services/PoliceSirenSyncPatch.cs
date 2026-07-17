using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.Vehicles;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(VehicleLights), "UpdateVisuals")]
    internal static class PoliceSirenSyncPatch
    {
        private static readonly Dictionary<int, bool> AppliedStates =
            new Dictionary<int, bool>();

        private static void Postfix(VehicleLights __instance)
        {
            Apply(__instance, false);
        }

        internal static void Apply(VehicleLights vehicleLights, bool force)
        {
            if (vehicleLights == null)
                return;

            try
            {
                int id = vehicleLights.GetInstanceID();
                bool on = vehicleLights.HeadlightsOn;
                if (!force && AppliedStates.TryGetValue(id, out bool applied) && applied == on)
                    return;

                if (VehicleService.Instance.ApplyPoliceSirenState(vehicleLights))
                    AppliedStates[id] = on;
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(VehicleLights), "RpcLogic___set_HeadlightsOn_1140765316")]
    internal static class PoliceSirenRpcSyncPatch
    {
        private static void Postfix(VehicleLights __instance)
        {
            PoliceSirenSyncPatch.Apply(__instance, true);
        }
    }

    [HarmonyPatch(typeof(VehicleLights), "ReadSyncVar___ScheduleOne_Vehicles_VehicleLights")]
    internal static class PoliceSirenReadSyncVarPatch
    {
        private static void Postfix(VehicleLights __instance, bool __result)
        {
            if (__result)
                PoliceSirenSyncPatch.Apply(__instance, true);
        }
    }

    [HarmonyPatch(typeof(VehicleLights), "Method_Private_Void_PDM_0")]
    internal static class PoliceSirenSyncVarChangedPatch
    {
        private static void Postfix(VehicleLights __instance)
        {
            PoliceSirenSyncPatch.Apply(__instance, true);
        }
    }
}
