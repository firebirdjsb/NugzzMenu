using Il2CppScheduleOne.UI;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class InteractionPromptDisplayService
    {
        internal static void ShowForCurrentFrame(
            InteractionCanvas canvas, Vector3 position, string message,
            Color messageColor, Sprite icon, Color iconColor,
            string iconText, Vector2 iconSize)
        {
            if (canvas == null || canvas.Container == null || canvas.Canvas == null ||
                !SessionAuthorityService.Instance.FeaturesAllowed ||
                GameplayStateGateService.Instance.AreFeatureHotkeysBlocked())
                return;

            // Native Update clears this flag and hides the container when an
            // equipped tool has CanInteractWhenEquipped=false. EnableDisplay only
            // sets content/next-frame intent; it never activates the container.
            // We run after Update, so present this already-validated custom action
            // now without enabling ordinary interactions for the equipped tool.
            bool nativeDisplayRequested = canvas._interactionDisplayEnabledThisFrame;
            try
            {
                // EnableDisplay hides its icon text when no sprite is available.
                // Keep the input hint readable even with missing native icon assets.
                if (icon == null && !string.IsNullOrWhiteSpace(iconText))
                    message = "[" + iconText + "] " + message;

                canvas.EnableInteractionDisplay(position, message, messageColor,
                    icon, iconColor, iconText, 1f, iconSize, true);
                canvas.Container.gameObject.SetActive(true);
                canvas.Canvas.enabled = true;
            }
            finally
            {
                // A custom action is refreshed every frame. Do not keep it alive
                // through the next Update after the target/tool has gone away.
                // Preserve any independent request from native interaction code.
                canvas._interactionDisplayEnabledThisFrame = nativeDisplayRequested;
            }
        }
    }
}
