using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class VehicleHudLifecycle
    {
        private static float _nextRecoveryCheck;

        internal static void Update()
        {
            if (Time.unscaledTime < _nextRecoveryCheck)
                return;

            _nextRecoveryCheck = Time.unscaledTime + 0.25f;
            Player player = ManagerCacheService.Instance.LocalPlayer;
            VehicleCanvas canvas = VehicleCanvas.Instance;
            if (player == null || canvas == null || player.IsInVehicle || player.CurrentVehicleSeat != null)
                return;

            bool canvasVisible = canvas.Canvas != null &&
                                 canvas.Canvas.enabled &&
                                 canvas.Canvas.gameObject.activeInHierarchy;
            if (canvasVisible)
                ClearAfterLocalExit(player);
        }

        internal static void ClearAfterLocalExit(Player player)
        {
            if (player == null || player != ManagerCacheService.Instance.LocalPlayer)
                return;

            // ExitVehicle can finish across multiple callbacks. Only clean the HUD once
            // the local player is definitively no longer seated in a vehicle.
            if (player.IsInVehicle || player.CurrentVehicleSeat != null)
                return;

            VehicleCanvas canvas = VehicleCanvas.Instance;
            if (canvas == null)
                return;

            if (canvas.Canvas != null)
                canvas.Canvas.enabled = false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ExitVehicle))]
    internal static class PlayerVehicleHudExitPatch
    {
        private static void Postfix(Player __instance)
        {
            VehicleHudLifecycle.ClearAfterLocalExit(__instance);
        }
    }

}
