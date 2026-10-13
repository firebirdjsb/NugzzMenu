// This models the observed 0.4.7f12 canvas boundary, not Unity rendering.
// Native Update disables the prompt for an interaction-blocking equipped tool,
// applies the display-request flag to its container, and clears that flag.
// EnableInteractionDisplay changes content/intent without activating the parent.
namespace UnityEngine
{
    public struct Vector3 { }
    public struct Vector2 { }
    public struct Color { }
    public sealed class Sprite { }
    public sealed class GameObject
    {
        public bool Active { get; private set; }
        public void SetActive(bool value) => Active = value;
    }
    public sealed class RectTransform
    {
        public GameObject gameObject { get; } = new GameObject();
    }
    public sealed class Canvas { public bool enabled { get; set; } }
}

namespace Il2CppScheduleOne.UI
{
    using UnityEngine;
    public sealed class InteractionCanvas
    {
        public RectTransform Container { get; set; } = new RectTransform();
        public Canvas Canvas { get; set; } = new Canvas();
        public bool _interactionDisplayEnabledThisFrame;
        public string Message { get; private set; }
        public bool FailDisplay { get; set; }
        public void NativeUpdate(bool equippedToolBlocks, int activeWorldLabels = 0)
        {
            if (equippedToolBlocks) _interactionDisplayEnabledThisFrame = false;
            Container.gameObject.SetActive(_interactionDisplayEnabledThisFrame);
            Canvas.enabled = _interactionDisplayEnabledThisFrame || activeWorldLabels > 0;
            _interactionDisplayEnabledThisFrame = false;
        }
        public void EnableInteractionDisplay(Vector3 position, string message, Color messageColor,
            Sprite icon, Color iconColor, string iconText, float pixelMultiplier,
            Vector2 iconSize, bool backdrop)
        {
            _interactionDisplayEnabledThisFrame = true;
            if (FailDisplay) throw new InvalidOperationException("Unavailable UI object");
            Message = message;
        }
    }
}

namespace NugzzMenu.Services
{
    public sealed class SessionAuthorityService
    {
        public static SessionAuthorityService Instance { get; } = new SessionAuthorityService();
        public bool FeaturesAllowed { get; set; } = true;
    }
    public sealed class GameplayStateGateService
    {
        public static GameplayStateGateService Instance { get; } = new GameplayStateGateService();
        public bool Blocked { get; set; }
        public bool AreFeatureHotkeysBlocked() => Blocked;
    }
}
