namespace NugzzMenu.Services
{
    internal sealed class LocalAnimationDefinition
    {
        internal readonly string Label;
        internal readonly string[] EnabledBools;
        internal readonly string[] DisabledBools;
        internal LocalAnimationDefinition(string label, string[] enabled, params string[] disabled)
        { Label = label; EnabledBools = enabled; DisabledBools = disabled; }
    }

    internal static class LocalAnimationCatalog
    {
        // These are Bool parameters in 0.4.7f12's AvatarAnimator, not NPC AI states.
        internal static readonly LocalAnimationDefinition[] Actions =
        {
            new("Talking on phone", new[] { "RightArm_HoldPhone_Raised" }, "RightArm_HoldPhone_Lowered"),
            new("Using laptop", new[] { "Sitting", "IsTyping" }),
            new("Sitting on ground", new[] { "IsCasualSitting" }),
            new("Dancing", new[] { "IsDancing1" }, "IsDancing2", "IsDancing3"),
            new("Peeing", new[] { "IsPissing" }, "IsPissing2"),
            new("Vomiting", new[] { "IsVomiting" }),
            // Authored render-skeleton emote; no invented native animator parameter.
            new("Boinking", System.Array.Empty<string>()),
            new("Revving motorcycle", new[] { "IsMotorbikeSitting" }, "Sitting")
        };
        internal const int Phone = 0, Laptop = 1, Pee = 4, Vomit = 5, Body = 6, Motorcycle = 7;
        internal const float Duration = 20f;
        internal static bool IsValid(int action) => action >= 0 && action < Actions.Length;
    }
}
