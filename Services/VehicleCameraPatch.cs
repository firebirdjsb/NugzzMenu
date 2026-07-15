using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(LandVehicle), "UpdateThrottle")]
    internal static class VehicleMenuThrottleLockPatch
    {
        private static void Prefix(LandVehicle __instance)
        {
            VehicleMenuCameraService.Instance.MaintainDrivingLock(__instance);
        }
    }

    [HarmonyPatch(typeof(LandVehicle), "UpdateSteerAngle")]
    internal static class VehicleMenuSteeringLockPatch
    {
        private static void Prefix(LandVehicle __instance)
        {
            VehicleMenuCameraService.Instance.MaintainDrivingLock(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.EnterVehicle))]
    internal static class PlayerVehicleEntryThirdPersonGuardPatch
    {
        private static void Prefix(Player __instance)
        {
            if (__instance == ManagerCacheService.Instance.LocalPlayer)
                ThirdPersonCameraService.Instance.ForceDisableForVehicle(false);
        }
    }

    [HarmonyPatch(typeof(VehicleCamera), "CheckForMouseMovement")]
    internal static class VehicleCameraMenuMouseMovementPatch
    {
        private static bool Prefix()
        {
            return !VehicleMenuCameraService.Instance.ShouldBlockCameraInput;
        }
    }

    [HarmonyPatch(typeof(VehicleCamera), "CheckForClick")]
    internal static class VehicleCameraMenuClickPatch
    {
        private static bool Prefix()
        {
            return !VehicleMenuCameraService.Instance.ShouldBlockCameraInput;
        }
    }

    [HarmonyPatch(typeof(VehicleCamera), "HandleNonSecondaryClickCameraMovement")]
    internal static class VehicleCameraMenuPrimaryMovementPatch
    {
        private static bool Prefix()
        {
            return !VehicleMenuCameraService.Instance.ShouldBlockCameraInput;
        }
    }

    [HarmonyPatch(typeof(VehicleCamera), "HandleSecondaryClickCameraMovement")]
    internal static class VehicleCameraMenuSecondaryMovementPatch
    {
        private static bool Prefix()
        {
            return !VehicleMenuCameraService.Instance.ShouldBlockCameraInput;
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.SetCameraMode))]
    internal static class VehicleCameraModeThirdPersonGuardPatch
    {
        private static void Prefix(PlayerCamera.ECameraMode mode)
        {
            if (mode == PlayerCamera.ECameraMode.Vehicle)
                ThirdPersonCameraService.Instance.ForceDisableForVehicle(false);
        }
    }
}
