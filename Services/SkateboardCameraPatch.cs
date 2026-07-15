using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(Player), nameof(Player.MountSkateboard))]
    internal static class PlayerSkateboardMountCameraPatch
    {
        private static void Prefix(Player __instance)
        {
            if (__instance == ManagerCacheService.Instance.LocalPlayer)
                ThirdPersonCameraService.Instance.ForceDisableForSkateboard(false);
        }

        private static void Postfix(Player __instance)
        {
            if (__instance == ManagerCacheService.Instance.LocalPlayer)
                ThirdPersonCameraService.Instance.NotifySkateboardMounted();
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.DismountSkateboard))]
    internal static class PlayerSkateboardDismountCameraPatch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance == ManagerCacheService.Instance.LocalPlayer)
                ThirdPersonCameraService.Instance.NotifySkateboardDismounted();
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), nameof(PlayerCamera.SetCameraMode))]
    internal static class PlayerSkateboardCameraModePatch
    {
        private static void Prefix(PlayerCamera.ECameraMode mode)
        {
            if (mode == PlayerCamera.ECameraMode.Skateboard)
                ThirdPersonCameraService.Instance.ForceDisableForSkateboard(false);
        }

        private static void Postfix(PlayerCamera.ECameraMode mode)
        {
            if (mode == PlayerCamera.ECameraMode.Skateboard)
                ThirdPersonCameraService.Instance.NotifySkateboardMounted();
        }
    }
}
