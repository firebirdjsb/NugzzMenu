using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class VehicleHudLifecycle
    {
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

            if (canvas.currentVehicle != null)
            {
                try
                {
                    canvas.VehicleExited(canvas.currentVehicle, player.transform);
                }
                catch
                {
                    // The explicit state cleanup below is the fallback for a broken
                    // or missed vanilla vehicle-exit callback.
                }
            }

            canvas.currentVehicle = null;
            if (canvas.Canvas != null)
                canvas.Canvas.enabled = false;
            if (canvas.DriverPromptsContainer != null)
                canvas.DriverPromptsContainer.SetActive(false);
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

    [HarmonyPatch(typeof(VehicleCanvas), nameof(VehicleCanvas.Update))]
    internal static class VehicleHudExitRecoveryPatch
    {
        private static void Postfix()
        {
            Player player = ManagerCacheService.Instance.LocalPlayer;
            VehicleCanvas canvas = VehicleCanvas.Instance;
            if (player == null || canvas == null || player.IsInVehicle || player.CurrentVehicleSeat != null)
                return;

            bool canvasVisible = canvas.Canvas != null &&
                                 canvas.Canvas.enabled &&
                                 canvas.Canvas.gameObject.activeInHierarchy;
            bool promptsVisible = canvas.DriverPromptsContainer != null &&
                                  canvas.DriverPromptsContainer.activeInHierarchy;
            if (canvas.currentVehicle != null || canvasVisible || promptsVisible)
                VehicleHudLifecycle.ClearAfterLocalExit(player);
        }
    }
}
