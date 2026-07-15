using System;
using System.Globalization;
using HarmonyLib;
using Il2CppFishNet.Connection;
using Il2CppScheduleOne.AvatarFramework;
using Il2CppScheduleOne.AvatarFramework.Customization;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.Variables;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Manages player-related cheats and modifications.
    /// </summary>
    public sealed class PlayerCheatService
    {
        private static readonly PlayerCheatService _instance = new PlayerCheatService();
        public static PlayerCheatService Instance => _instance;

        private bool _speedBoost;
        private float _speedMultiplier = 2f;
        private float _jumpMultiplier = 1f;
        private float _gravityMultiplier = 1f;
        private bool _speedBaselineCaptured;
        private float _speedBaseline = 1f;
        private float _nextWantedClearTime;
        private const string NetworkScaleVariable = "Nugzz.PlayerScale";
        private const float ScaleRebroadcastInterval = 2f;
        private float _playerScale = 1f;
        private float _lastAppliedPlayerScale = -1f;
        private float _lastBroadcastPlayerScale = -1f;
        private float _nextScaleBroadcastTime = -1f;
        private bool _baseAppearanceScaleCaptured;
        private float _baseGenderScaleMultiplier = 1f;
        private float _baseAvatarWeight = 0.5f;
        private float _baseAvatarHeight = 1f;
        private float _allowForcedDeathUntil = -1f;
        private bool _scaleNetworkSyncDisabled;
        private bool _vanillaVisibleScaleDisabled;

        public bool GodMode { get; set; }
        public bool InfiniteStamina { get; set; }
        public bool SpeedBoost
        {
            get => _speedBoost;
            set
            {
                if (_speedBoost == value)
                    return;
                _speedBoost = value;
                if (!_speedBoost)
                    RemoveSpeedBoost();
            }
        }
        public float SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = Mathf.Clamp(value, 1f, 10f);
        }
        public float JumpMultiplier
        {
            get => _jumpMultiplier;
            set => _jumpMultiplier = Mathf.Clamp(value, 0.1f, 6f);
        }
        public float GravityMultiplier
        {
            get => _gravityMultiplier;
            set => _gravityMultiplier = Mathf.Clamp(value, 0f, 5f);
        }
        public bool InfiniteAmmo { get; set; }
        public bool NeverWanted { get; set; }
        public float PlayerScale
        {
            get => _playerScale;
            set
            {
                float clamped = Mathf.Clamp(value, 0.25f, 4f);
                _playerScale = clamped;
            }
        }

        private PlayerCheatService() { }

        public void Update()
        {
            if (GodMode) ApplyGodMode();
            if (InfiniteStamina) ApplyInfiniteStamina();
            if (SpeedBoost) ApplySpeedBoost();
            if (InfiniteAmmo) ApplyInfiniteAmmo();
            if (NeverWanted) ApplyNeverWanted();
            ApplyPlayerScale();
            ApplyMovementTuning();
            MaintainPlayerScaleSync();
        }

        private void ApplyGodMode()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null) return;

                var health = player.Health;
                if (health != null && health.CurrentHealth < PlayerHealth.MAX_HEALTH)
                {
                    health.SetHealth(PlayerHealth.MAX_HEALTH);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"God mode failed: {ex.Message}");
            }
        }

        private void ApplyInfiniteStamina()
        {
            try
            {
                var movement = GetLocalMovement();
                if (movement != null)
                    movement._CurrentStaminaReserve_k__BackingField = PlayerMovement.StaminaReserveMax;
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Stamina failed: {ex.Message}");
            }
        }

        private void ApplySpeedBoost()
        {
            try
            {
                if (!_speedBaselineCaptured)
                {
                    _speedBaseline = PlayerMovement.StaticMoveSpeedMultiplier;
                    if (_speedBaseline <= 0f)
                        _speedBaseline = 1f;
                    _speedBaselineCaptured = true;
                }

                PlayerMovement.StaticMoveSpeedMultiplier = _speedBaseline * _speedMultiplier;
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Speed boost failed: {ex.Message}");
            }
        }

        private void ApplyMovementTuning()
        {
            try
            {
                PlayerMovement.JumpMultiplier = _jumpMultiplier;
                if (!FlyingService.Instance.Enabled)
                {
                    float gravity = PlayerMovement.BaseGravityMultiplier * _gravityMultiplier;
                    PlayerMovement.GravityMultiplier = gravity;
                    var player = ManagerCacheService.Instance.LocalPlayer;
                    if (player != null)
                        player.SetGravityMultiplier(gravity);
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Movement tuning failed: " + ex.Message);
            }
        }

        private void ApplyInfiniteAmmo()
        {
            try
            {
                var inventory = ManagerCacheService.Instance.PlayerInventory;
                if (inventory == null) return;

                var equippedItem = inventory.EquippedItem;
                if (equippedItem == null) return;

                var integerItem = equippedItem.TryCast<Il2CppScheduleOne.ItemFramework.IntegerItemInstance>();
                if (integerItem != null)
                {
                    integerItem.SetValue(99);
                }
            }
            catch
            {
            }
        }

        private void ApplyNeverWanted()
        {
            try
            {
                var crimeData = ManagerCacheService.Instance.LocalPlayer?.CrimeData;
                if (crimeData != null)
                {
                    if (Time.unscaledTime < _nextWantedClearTime)
                        return;
                    _nextWantedClearTime = Time.unscaledTime + 0.25f;

                    if (crimeData.Crimes != null && crimeData.Crimes.Count > 0)
                        crimeData.ClearCrimes();
                    if (crimeData.CurrentPursuitLevel != PlayerCrimeData.EPursuitLevel.None)
                        crimeData.SetPursuitLevel(PlayerCrimeData.EPursuitLevel.None);
                    crimeData.SetArrestProgress(0f);
                    crimeData.SetBodySearchProgress(0f);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Never wanted failed: {ex.Message}");
            }
        }

        private void ApplyPlayerScale()
        {
            try
            {
                if (Mathf.Abs(_lastAppliedPlayerScale - _playerScale) < 0.001f)
                    return;

                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null)
                    return;

                Vector3 previousPosition = player.transform.position;
                player.SetScale(_playerScale, 0.2f);
                ApplyLocalMovementScale(_playerScale);
                RestoreScalePosition(player, previousPosition);
                ApplyVanillaVisibleScale(player, _playerScale, true);
                _lastAppliedPlayerScale = _playerScale;
                BroadcastPlayerScale(player, true);
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Player size failed: {ex.Message}");
            }
        }

        private void MaintainPlayerScaleSync()
        {
            if (Time.unscaledTime < _nextScaleBroadcastTime)
                return;

            _nextScaleBroadcastTime = Time.unscaledTime + ScaleRebroadcastInterval;
            if (Mathf.Abs(_playerScale - 1f) < 0.001f &&
                Mathf.Abs(_lastBroadcastPlayerScale - 1f) < 0.001f)
            {
                return;
            }

            Player player = ManagerCacheService.Instance.LocalPlayer;
            if (player != null)
                BroadcastPlayerScale(player, false);
        }

        private void BroadcastPlayerScale(Player player, bool force)
        {
            if (player == null || _scaleNetworkSyncDisabled)
                return;

            if (!force &&
                Mathf.Abs(_lastBroadcastPlayerScale - _playerScale) < 0.001f &&
                Mathf.Abs(_playerScale - 1f) < 0.001f)
            {
                return;
            }

            try
            {
                ApplyVanillaVisibleScale(player, _playerScale, true);
                if (!EnsureScaleVariable(player))
                {
                    _scaleNetworkSyncDisabled = true;
                    _lastBroadcastPlayerScale = _playerScale;
                    return;
                }

                string value = _playerScale.ToString("0.###", CultureInfo.InvariantCulture);
                try { player.SetVariableValue(NetworkScaleVariable, value, false); } catch { }
                player.SendValue(NetworkScaleVariable, value, true);
                _lastBroadcastPlayerScale = _playerScale;
            }
            catch (Exception ex)
            {
                _scaleNetworkSyncDisabled = true;
                _lastBroadcastPlayerScale = _playerScale;
                DebugLogService.Instance.VerboseWarning("Player scale sync failed: " + ex.Message);
            }
        }

        private void ApplyVanillaVisibleScale(Player player, float scale, bool broadcast)
        {
            if (player == null || _vanillaVisibleScaleDisabled)
                return;

            try
            {
                BasicAvatarSettings settings = player.CurrentAvatarSettings;
                if (settings == null)
                    return;

                CaptureBaseAppearanceScale(settings);

                float visibleScale = Mathf.Clamp(scale, 0.25f, 4f);
                settings.SetValue<float>(
                    nameof(BasicAvatarSettings.GenderScaleMultiplier),
                    Mathf.Clamp(_baseGenderScaleMultiplier * visibleScale, 0.05f, 10f));
                settings.Weight = Mathf.Clamp01(_baseAvatarWeight);

                try { player.SetAppearance(settings, false); } catch { }
                if (broadcast)
                {
                    try { player.SendAppearance(settings); } catch { }
                }

                try
                {
                    AvatarSettings avatarSettings = settings.GetAvatarSettings();
                    if (avatarSettings != null)
                    {
                        avatarSettings.Height = Mathf.Clamp(_baseAvatarHeight * visibleScale, 0.05f, 10f);
                        try { player.SetAvatarSettings(avatarSettings); } catch { }
                        if (broadcast)
                            player.SendAvatarSettings(avatarSettings);
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                if (ex is FieldAccessException ||
                    ex.Message.IndexOf("constant field", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _vanillaVisibleScaleDisabled = true;
                }

                DebugLogService.Instance.VerboseWarning("Vanilla visible scale sync failed: " + ex.Message);
            }
        }

        private void CaptureBaseAppearanceScale(BasicAvatarSettings settings)
        {
            if (_baseAppearanceScaleCaptured || settings == null)
                return;

            try
            {
                _baseGenderScaleMultiplier = Mathf.Max(
                    0.05f,
                    settings.GetValue<float>(nameof(BasicAvatarSettings.GenderScaleMultiplier)));
                _baseAvatarWeight = Mathf.Clamp01(settings.Weight);
                AvatarSettings avatarSettings = settings.GetAvatarSettings();
                if (avatarSettings != null)
                    _baseAvatarHeight = Mathf.Max(0.05f, avatarSettings.Height);
                _baseAppearanceScaleCaptured = true;
            }
            catch
            {
                _baseAppearanceScaleCaptured = true;
            }
        }

        private static bool EnsureScaleVariable(Player player)
        {
            if (player == null)
                return false;

            try
            {
                if (player.GetVariable(NetworkScaleVariable) != null)
                    return true;
            }
            catch { }

            try
            {
                var variable = new NumberVariable(
                    NetworkScaleVariable,
                    EVariableReplicationMode.Networked,
                    false,
                    EVariableMode.Player,
                    player,
                    1f);
                player.AddVariable(variable);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Scale variable registration failed: " + ex.Message);
                return false;
            }
        }

        internal static bool TryApplyNetworkScale(Player player, string variableName, string value)
        {
            if (player == null ||
                !string.Equals(variableName, NetworkScaleVariable, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                return true;

            scale = Mathf.Clamp(scale, 0.25f, 4f);
            try
            {
                ApplyActualPlayerScale(player, scale);
                Instance.ApplyVanillaVisibleScale(player, scale, false);
                if (player.IsLocalPlayer)
                    Instance._lastAppliedPlayerScale = scale;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Received player scale apply failed: " + ex.Message);
            }

            return true;
        }

        private static void ApplyActualPlayerScale(Player player, float scale)
        {
            if (player == null)
                return;

            try
            {
                Vector3 previousPosition = player.transform.position;
                player.SetScale(scale, 0.2f);
                if (player.IsLocalPlayer)
                    ApplyLocalMovementScale(scale);
                RestoreScalePosition(player, previousPosition);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Actual player scale apply failed: " + ex.Message);
            }
        }

        private static void ApplyLocalMovementScale(float scale)
        {
            try
            {
                PlayerMovement movement = GetLocalMovement();
                if (movement != null)
                    movement._StandingScale_k__BackingField = scale;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Movement scale apply failed: " + ex.Message);
            }
        }

        private static void RestoreScalePosition(Player player, Vector3 previousPosition)
        {
            if (player == null)
                return;

            try
            {
                Vector3 currentPosition = player.transform.position;
                if (currentPosition.y > previousPosition.y + 0.05f)
                    currentPosition.y = previousPosition.y;
                player.transform.position = currentPosition;
            }
            catch { }
        }

        public void ForceKillLocalPlayer()
        {
            try
            {
                var health = ManagerCacheService.Instance.LocalPlayer?.Health;
                if (health == null)
                    return;

                _allowForcedDeathUntil = Time.unscaledTime + 2f;
                health.SetAfflictedWithLethalEffect(true);
                health.TakeDamage(PlayerHealth.MAX_HEALTH + 999f, true, true);
                health.SendDie();
                NotificationService.Instance.Status("Lethal effect killed player");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Lethal kill failed: {ex.Message}");
            }
        }

        internal bool IsForcedDeathAllowed()
        {
            return Time.unscaledTime <= _allowForcedDeathUntil;
        }

        private void RemoveSpeedBoost()
        {
            try
            {
                if (_speedBaselineCaptured)
                    PlayerMovement.StaticMoveSpeedMultiplier = _speedBaseline;
            }
            catch { }

            _speedBaselineCaptured = false;
        }

        internal static PlayerMovement GetLocalMovement()
        {
            try
            {
                PlayerMovement movement = PlayerMovement.Instance;
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (movement != null && player != null &&
                    (movement.Player == null || movement.Player == player || movement.Player.IsLocalPlayer))
                    return movement;
            }
            catch { }
            return UnityEngine.Object.FindObjectOfType<PlayerMovement>();
        }
    }

    [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.ChangeStamina))]
    internal static class InfiniteStaminaChangePatch
    {
        private static bool Prefix(PlayerMovement __instance, float change)
        {
            if (!PlayerCheatService.Instance.InfiniteStamina || change >= 0f)
                return true;
            return __instance != PlayerCheatService.GetLocalMovement();
        }
    }

    [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.SetStamina))]
    internal static class InfiniteStaminaSetPatch
    {
        private static void Prefix(PlayerMovement __instance, ref float value)
        {
            if (PlayerCheatService.Instance.InfiniteStamina &&
                __instance == PlayerCheatService.GetLocalMovement())
                value = PlayerMovement.StaminaReserveMax;
        }
    }

    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.TakeDamage))]
    internal static class GodModeDamagePatch
    {
        private static bool Prefix(PlayerHealth __instance)
        {
            return !IsProtectedLocalPlayer(__instance);
        }

        internal static bool IsProtectedLocalPlayer(PlayerHealth health)
        {
            if (!PlayerCheatService.Instance.GodMode || health == null)
                return false;
            if (PlayerCheatService.Instance.IsForcedDeathAllowed())
                return false;
            try { return health.Player != null && health.Player.IsLocalPlayer; }
            catch { return false; }
        }
    }

    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.SendDie))]
    internal static class GodModeSendDiePatch
    {
        private static bool Prefix(PlayerHealth __instance)
        {
            return !GodModeDamagePatch.IsProtectedLocalPlayer(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Die))]
    internal static class GodModeDiePatch
    {
        private static bool Prefix(PlayerHealth __instance)
        {
            return !GodModeDamagePatch.IsProtectedLocalPlayer(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), "ReceiveValue", new[] { typeof(NetworkConnection), typeof(string), typeof(string) })]
    internal static class PlayerReceiveValueTargetPatch
    {
        private static void Postfix(Player __instance, string variableName, string value)
        {
            PlayerCheatService.TryApplyNetworkScale(__instance, variableName, value);
            VehicleService.Instance.TryApplyNetworkVehicleTune(__instance, variableName, value);
        }
    }

    [HarmonyPatch(typeof(Player), "ReceiveValue", new[] { typeof(string), typeof(string) })]
    internal static class PlayerReceiveValueLocalPatch
    {
        private static void Postfix(Player __instance, string variableName, string value)
        {
            PlayerCheatService.TryApplyNetworkScale(__instance, variableName, value);
            VehicleService.Instance.TryApplyNetworkVehicleTune(__instance, variableName, value);
        }
    }
}
