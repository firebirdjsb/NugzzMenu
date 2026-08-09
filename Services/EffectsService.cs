using System;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;
using GameEffect = Il2CppScheduleOne.Effects.Effect;

namespace NugzzMenu.Services
{
    public sealed class EffectsService
    {
        private static readonly EffectsService _instance = new EffectsService();
        private const float LethalKillDelay = 7.5f;

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
        private float _lethalKillTimer = -1f;

        public static EffectsService Instance => _instance;
        public string[] EffectIds => _effectIds;
        public string[] EffectLabels => _effectLabels;

        private EffectsService() { }

        public void Update()
        {
            if (_lethalKillTimer < 0f)
                return;

            _lethalKillTimer -= Time.deltaTime;
            if (_lethalKillTimer > 0f)
                return;

            _lethalKillTimer = -1f;
            PlayerCheatService.Instance.ForceKillLocalPlayer();
        }

        public void ApplyEffect(string effectName, float duration = 30f)
        {
            try
            {
                if (Player.Local == null)
                {
                    NotificationService.Instance.Error("No local player found");
                    return;
                }

                GameEffect effect = FindEffect(effectName);
                if (effect == null)
                {
                    NotificationService.Instance.Error("Effect not found: " + effectName);
                    return;
                }

                if (Normalize(effectName) == "lethal")
                {
                    _lethalKillTimer = LethalKillDelay;
                    NotificationService.Instance.Warning(
                        "Lethal will kill in " + LethalKillDelay.ToString("F1") + "s");
                }

                int applied = ApplyToLoadedPlayers(effect);
                NotificationService.Instance.Status(
                    "Applied FX: " + GetLabel(effectName) + " (" + applied + ")");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error("FX failed: " + effectName);
                DebugLogService.Instance.VerboseWarning(
                    "Effect apply failed: " + ex.Message);
            }
        }

        public void ClearAllEffects()
        {
            _lethalKillTimer = -1f;
            EnsureEffectCache();

            int cleared = 0;
            for (int i = 0; i < _cachedEffects.Length; i++)
            {
                try
                {
                    cleared += ClearFromLoadedPlayers(_cachedEffects[i]);
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning(
                        "Effect clear failed: " + ex.Message);
                }
            }

            NotificationService.Instance.Status("Cleared visible FX (" + cleared + ")");
        }

        private static int ApplyToLoadedPlayers(GameEffect effect)
        {
            if (effect == null)
                return 0;

            int applied = 0;
            var players = Player.PlayerList;
            if (players == null || players.Count == 0)
            {
                Player local = Player.Local;
                if (local == null)
                    return 0;

                effect.ApplyToPlayer(local);
                return 1;
            }

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null)
                    continue;

                try
                {
                    effect.ApplyToPlayer(player);
                    applied++;
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning(
                        "Effect apply skipped: " + ex.Message);
                }
            }

            return applied;
        }

        private static int ClearFromLoadedPlayers(GameEffect effect)
        {
            if (effect == null)
                return 0;

            int cleared = 0;
            var players = Player.PlayerList;
            if (players == null || players.Count == 0)
            {
                Player local = Player.Local;
                if (local == null)
                    return 0;

                effect.ClearFromPlayer(local);
                return 1;
            }

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null)
                    continue;

                try
                {
                    effect.ClearFromPlayer(player);
                    cleared++;
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning(
                        "Effect clear skipped: " + ex.Message);
                }
            }

            return cleared;
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
                _cachedEffects = Resources.FindObjectsOfTypeAll<GameEffect>() ??
                    new GameEffect[0];
            }
            catch (Exception ex)
            {
                _cachedEffects = new GameEffect[0];
                DebugLogService.Instance.VerboseWarning(
                    "Effect cache failed: " + ex.Message);
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
            try
            {
                return effect?.GetIl2CppType()?.Name ?? "";
            }
            catch
            {
                return effect?.name ?? "";
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
