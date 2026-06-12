using System;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;
using GameEffect = Il2CppScheduleOne.Effects.Effect;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Service for applying local player product/drug effects.
    /// </summary>
    public sealed class EffectsService
    {
        private static readonly EffectsService _instance = new EffectsService();
        public static EffectsService Instance => _instance;

        private readonly string[] _effectIds =
        {
            "AntiGravity", "Athletic", "Balding", "BrightEyed", "Calming",
            "CalorieDense", "Cyclopean", "Disorienting", "Electrifying",
            "Energizing", "Euphoric", "Explosive", "Focused", "Foggy",
            "Gingeritis", "LongFaced", "Glowie", "Jennerising", "Laxative",
            "Lethal", "Munchies", "Paranoia", "Refreshing", "Schizophrenic",
            "Sedating", "Seizure", "Shrinking", "Slippery", "Smelly",
            "Sneaky", "Spicy", "ThoughtProvoking", "Toxic", "TropicThunder",
            "Zombifying"
        };

        private readonly string[] _effectLabels =
        {
            "Anti-Gravity", "Athletic", "Balding", "Bright Eyed", "Calming",
            "Calorie Dense", "Cyclopean", "Disorienting", "Electrifying",
            "Energizing", "Euphoric", "Explosive", "Focused", "Foggy",
            "Gingeritis", "Long Faced", "Glowie", "Jennerising", "Laxative",
            "Lethal", "Munchies", "Paranoia", "Refreshing", "Schizophrenic",
            "Sedating", "Seizure", "Shrinking", "Slippery", "Smelly",
            "Sneaky", "Spicy", "Thought Provoking", "Toxic", "Tropic Thunder",
            "Zombifying"
        };

        private GameEffect[] _cachedEffects = new GameEffect[0];
        private bool _cacheInitialized;

        private EffectsService() { }

        public string[] EffectIds => _effectIds;
        public string[] EffectLabels => _effectLabels;

        public void ApplyEffect(string effectName, float duration = 30f)
        {
            try
            {
                var player = Player.Local;
                if (player == null)
                {
                    Debug.LogError("[Nugzz] No local player found");
                    return;
                }

                GameEffect effect = FindEffect(effectName);
                if (effect == null)
                {
                    NotificationService.Instance.Error("Effect not found: " + effectName);
                    Debug.LogError("[Nugzz] Effect not found: " + effectName);
                    return;
                }

                effect.ApplyToPlayer(player);
                NotificationService.Instance.Status("FX: " + GetLabel(effectName));
                Debug.Log("[Nugzz] Applied effect: " + effectName + " for " + duration + "s");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error("FX failed: " + effectName);
                Debug.LogError("[Nugzz] Failed to apply effect " + effectName + ": " + ex);
            }
        }

        public void ClearAllEffects()
        {
            try
            {
                var player = Player.Local;
                if (player == null)
                    return;

                EnsureEffectCache();
                for (int i = 0; i < _cachedEffects.Length; i++)
                {
                    try
                    {
                        _cachedEffects[i]?.ClearFromPlayer(player);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[Nugzz] Failed clearing effect " + SafeEffectName(_cachedEffects[i]) + ": " + ex.Message);
                    }
                }

                NotificationService.Instance.Status("Cleared local FX");
                Debug.Log("[Nugzz] Cleared all local player effects");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Nugzz] Failed to clear effects: " + ex);
            }
        }

        public void ClearEffect(string effectName)
        {
            try
            {
                var player = Player.Local;
                if (player == null)
                {
                    Debug.LogError("[Nugzz] No local player found");
                    return;
                }

                GameEffect effect = FindEffect(effectName);
                if (effect == null)
                    return;

                effect.ClearFromPlayer(player);
                NotificationService.Instance.Status("Cleared FX: " + GetLabel(effectName));
                Debug.Log("[Nugzz] Cleared effect: " + effectName);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Nugzz] Failed to clear effect " + effectName + ": " + ex);
            }
        }

        private GameEffect FindEffect(string effectName)
        {
            EnsureEffectCache();
            string target = Normalize(effectName);

            for (int i = 0; i < _cachedEffects.Length; i++)
            {
                GameEffect effect = _cachedEffects[i];
                if (effect == null)
                    continue;

                if (Normalize(effect.ID) == target ||
                    Normalize(effect.Name) == target ||
                    Normalize(effect.name) == target ||
                    Normalize(SafeEffectName(effect)) == target)
                {
                    return effect;
                }
            }

            return null;
        }

        private void EnsureEffectCache()
        {
            if (_cacheInitialized)
                return;

            try
            {
                var found = Resources.FindObjectsOfTypeAll<GameEffect>();
                if (found == null)
                {
                    _cachedEffects = new GameEffect[0];
                    _cacheInitialized = true;
                    return;
                }

                _cachedEffects = new GameEffect[found.Length];
                for (int i = 0; i < found.Length; i++)
                    _cachedEffects[i] = found[i];

                Debug.Log("[Nugzz] Cached " + _cachedEffects.Length + " player effects");
            }
            catch (Exception ex)
            {
                _cachedEffects = new GameEffect[0];
                Debug.LogError("[Nugzz] Failed to cache player effects: " + ex);
            }

            _cacheInitialized = true;
        }

        private string GetLabel(string effectName)
        {
            string target = Normalize(effectName);
            for (int i = 0; i < _effectIds.Length; i++)
            {
                if (Normalize(_effectIds[i]) == target)
                    return _effectLabels[i];
            }

            return effectName;
        }

        private static string SafeEffectName(GameEffect effect)
        {
            if (effect == null)
                return "";

            try
            {
                return effect.GetIl2CppType()?.Name ?? "";
            }
            catch
            {
                return effect.name ?? "";
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .ToLowerInvariant();
        }
    }
}
