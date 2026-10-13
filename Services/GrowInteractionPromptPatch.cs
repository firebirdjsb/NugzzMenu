using HarmonyLib;
using Il2CppScheduleOne.UI;

namespace NugzzMenu.Services
{
    [HarmonyPatch(typeof(InteractionCanvas), "LateUpdate")]
    internal static class GrowInteractionPromptLateUpdatePatch
    {
        // Native Update has already applied its equipped-tool hide rule. Publish
        // after the native LateUpdate work, for visibility in this same frame.
        [HarmonyPriority(Priority.Last)]
        private static void Postfix()
        {
            ShapePrefabService.Instance.RefreshPickupPrompt();
            GrowToolFallbackService.Instance.RefreshPromptForEquippedTool();
            GrowToolFallbackService.Instance.RenderQueuedPrompt();
        }
    }
}
