using System.Collections.Generic;
using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class EffectAppearanceResetService
    {
        internal static void RestoreSkin(Player player)
        {
            if (player?.Avatar?.Appearance == null) return;
            // Use the chosen appearance, not its drug-tinted rendered color.
            var naked = player.Avatar.Appearance.AppliedNakedAppearance;
            if (player.CurrentAppearance == null && naked == null) return;
            Color color = player.CurrentAppearance != null ? player.CurrentAppearance.SkinColor : naked.SkinColor;
            var seen = new HashSet<int>();
            Restore(player.Avatar.Effects, color, seen);
            player.Avatar.Appearance.SetSkinColor(color);
        }
        private static void Restore(AvatarEffects effects, Color color, HashSet<int> seen)
        {
            if (effects == null || !seen.Add(effects.GetInstanceID())) return;
            var smoother = effects.SkinColorSmoother;
            if (smoother != null)
            {
                // Lethal.ClearFromPlayer calls SetSicklySkinColor(false): false
                // controls mirroring, it does not remove the sickly tint.
                smoother.RemoveOverride("sickly");
                smoother.RemoveOverride("inverted");
                smoother.RemoveOverride("Zombified");
                effects.SetDefaultSkinColor(color, false);
                smoother._CurrentValue_k__BackingField = color;
            }
            if (effects.Avatar?.Appearance != null) effects.Avatar.Appearance.SetSkinColor(color);
            if (effects.MirrorEffectsTo != null)
                foreach (AvatarEffects mirror in effects.MirrorEffectsTo)
                    try { Restore(mirror, color, seen); } catch { }
        }
    }
}
