using System;
using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
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
        private bool _speedBaselineCaptured;
        private float _speedBaseline = 1f;
        private float _nextWantedClearTime;

        public bool GodMode { get; set; }
        public bool InfiniteStamina { get; set; }
        public bool InfiniteEnergy { get; set; }
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
        public bool InfiniteAmmo { get; set; }
        public bool NeverWanted { get; set; }

        private PlayerCheatService() { }

        public void Update()
        {
            if (GodMode) ApplyGodMode();
            if (InfiniteStamina) ApplyInfiniteStamina();
            if (InfiniteEnergy) ApplyInfiniteEnergy();
            if (SpeedBoost) ApplySpeedBoost();
            if (InfiniteAmmo) ApplyInfiniteAmmo();
            if (NeverWanted) ApplyNeverWanted();
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

        private void ApplyInfiniteEnergy()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null) return;

                if (player.Energy != null)
                    player.Energy._CurrentEnergy_k__BackingField = PlayerEnergy.MAX_ENERGY;
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Energy failed: {ex.Message}");
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

        public void TeleportToTutorialTown()
        {
            TeleportService.Instance.TeleportToTutorialTown();
        }

        public void EndTutorialMode()
        {
            TeleportService.Instance.EndTutorialMode();
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
}
