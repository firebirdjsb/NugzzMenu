using HarmonyLib;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.Interaction;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class GrowPromptOwnershipService
    {
        private static InteractableObject _target;
        private static string _message;
        private static int _frame = -1;

        internal static void Claim(GrowContainer container, string message)
        {
            try
            {
                _target = container?._interactionHandler?._interactableObject;
                _message = "[LMB] " + message;
                _frame = Time.frameCount;
            }
            catch { Clear(); }
        }

        internal static void Clear() { _target = null; _message = null; _frame = -1; }

        internal static bool TryGetMessage(InteractableObject target, out string message)
        {
            message = null;
            if (_frame != Time.frameCount || target == null || target != _target ||
                !SessionAuthorityService.Instance.FeaturesAllowed ||
                GameplayStateGateService.Instance.AreFeatureHotkeysBlocked()) return false;
            message = _message;
            return !string.IsNullOrEmpty(message);
        }
    }

    // Container LateUpdate writes a fallback label after the tool Update. Keep
    // that native label consistent with the validated custom action, irrespective
    // of the order Unity chooses for the two different MonoBehaviours.
    [HarmonyPatch(typeof(GrowContainerInteraction), "LateUpdate")]
    internal static class GrowFallbackLabelPatch
    {
        private static void Postfix(GrowContainerInteraction __instance)
        {
            try
            {
                InteractableObject target = __instance?._interactableObject;
                if (GrowPromptOwnershipService.TryGetMessage(target, out string message))
                    target.SetMessage(message);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(InteractableObject), "ShowMessage")]
    internal static class GrowNativeMessagePatch
    {
        private static bool Prefix(InteractableObject __instance)
        {
            try
            {
                // Hover can run before the equipped tool's Update. Resolve aim
                // and readiness now so this frame never reuses yesterday's lease.
                if (__instance != null && __instance.GetComponentInParent<GrowContainer>() != null)
                    GrowToolFallbackService.Instance.RefreshPromptForEquippedTool();
                if (GrowPromptOwnershipService.TryGetMessage(__instance, out string message))
                {
                    __instance.SetMessage(message);
                    // Native ShowMessage writes the same InteractionCanvas and
                    // can overwrite the custom LMB display later in LateUpdate.
                    // Skip only that display writer for this validated target;
                    // hover events, interaction state, and clicks remain native.
                    return false;
                }
            }
            catch { }
            return true;
        }
    }
}
