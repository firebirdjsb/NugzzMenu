using HarmonyLib;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.PlayerScripts;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.SetEquippable))]
    internal static class FirstPersonEquipVisibilityPatch
    {
        private static void Postfix()
        {
            ViewModelVisibilityService.Instance.EnsureFirstPersonViewmodelVisible();
        }
    }

    [HarmonyPatch(typeof(PunchController), "Punch")]
    internal static class FirstPersonPunchVisibilityPatch
    {
        private static void Prefix()
        {
            ViewModelVisibilityService.Instance.EnsureFirstPersonViewmodelVisible();
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "ViewAvatar")]
    internal static class NativeAvatarViewVisibilityPatch
    {
        private static bool Prefix()
        {
            if (!ThirdPersonCameraService.Instance.IsSkateboardActive)
                return true;

            NotificationService.Instance.Status("Camera unavailable: skateboard");
            return false;
        }

        private static void Postfix()
        {
            ViewModelVisibilityService.Instance.EnterNativeAvatarView(
                ManagerCacheService.Instance.LocalPlayer);
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "StopViewingAvatar")]
    internal static class NativeAvatarViewStopVisibilityPatch
    {
        private static void Postfix()
        {
            ViewModelVisibilityService.Instance.RestoreFirstPerson(
                ManagerCacheService.Instance.LocalPlayer);
        }
    }
}
