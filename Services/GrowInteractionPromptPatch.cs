using HarmonyLib;
using Il2CppScheduleOne.UI;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(InteractionCanvas), "LateUpdate")]
    internal static class GrowInteractionPromptLateUpdatePatch
    {
        private static void Prefix()
        {
            GrowToolFallbackService.Instance.RefreshPromptForEquippedTool();
            GrowToolFallbackService.Instance.RenderQueuedPrompt();
        }
    }
}
