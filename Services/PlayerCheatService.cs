using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using Il2CppFishNet.Connection;
using Il2CppScheduleOne.Equipping;
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
        private float _jumpMultiplier = 1f;
        private float _gravityMultiplier = 1f;
        private bool _speedBaselineCaptured;
        private float _speedBaseline = 1f;
        private float _nextWantedClearTime;
        private const string NetworkScaleVariable = "Nugzz.PlayerScale";
        private const string NetworkScaleRequestVariable = "Nugzz.PlayerScale.Request";
        private const float ScaleRebroadcastInterval = 2f;
        private static readonly Dictionary<int, ScaleAnchorState> ScaleAnchors =
            new Dictionary<int, ScaleAnchorState>();
        private float _playerScale = 1f;
        private float _localScaleCorrection;
        private float _lastAppliedPlayerScale = -1f;
        private float _lastBroadcastPlayerScale = -1f;
        private float _nextScaleBroadcastTime = -1f;
        private float _allowForcedDeathUntil = -1f;
        private float _lastAppliedJumpMultiplier = float.NaN;
        private float _lastAppliedGravityMultiplier = float.NaN;
        private bool _wasFlying;

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
        public bool BottomlessTrashGrabber { get; set; }
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

        public void ResetAll()
        {
            GodMode = false;
            InfiniteStamina = false;
            InfiniteAmmo = false;
            NeverWanted = false;
            BottomlessTrashGrabber = false;
            SpeedBoost = false;
            SpeedMultiplier = 2f;
            PlayerScale = 1f;
            JumpMultiplier = 1f;
            GravityMultiplier = 1f;
            _lastAppliedPlayerScale = -1f;
            _lastAppliedJumpMultiplier = float.NaN;
            _lastAppliedGravityMultiplier = float.NaN;
            _nextWantedClearTime = 0f;

            try
            {
                ApplyPlayerScale();
                ApplyMovementTuning();
            }
            catch { }
        }

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
                if (health != null && health.CurrentHealth < PlayerHealth.MaxHealth)
                {
                    health.SetHealth(PlayerHealth.MaxHealth);
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
                if (movement != null &&
                    movement._CurrentStaminaReserve_k__BackingField < PlayerMovement.StaminaReserveMax)
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

                float target = _speedBaseline * _speedMultiplier;
                if (Mathf.Abs(PlayerMovement.StaticMoveSpeedMultiplier - target) > 0.001f)
                    PlayerMovement.StaticMoveSpeedMultiplier = target;
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
                if (float.IsNaN(_lastAppliedJumpMultiplier) ||
                    Mathf.Abs(_lastAppliedJumpMultiplier - _jumpMultiplier) > 0.001f)
                {
                    PlayerMovement.JumpMultiplier = _jumpMultiplier;
                    _lastAppliedJumpMultiplier = _jumpMultiplier;
                }

                bool flying = FlyingService.Instance.Enabled;
                float gravity = PlayerMovement.BaseGravityMultiplier * _gravityMultiplier;
                if (!flying && (_wasFlying || float.IsNaN(_lastAppliedGravityMultiplier) ||
                    Mathf.Abs(_lastAppliedGravityMultiplier - gravity) > 0.001f))
                {
                    PlayerMovement.GravityMultiplier = gravity;
                    var player = ManagerCacheService.Instance.LocalPlayer;
                    if (player != null)
                        player.SetGravityMultiplier(gravity);
                    _lastAppliedGravityMultiplier = gravity;
                }

                _wasFlying = flying;
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
                if (integerItem != null && integerItem.Value < 99)
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

                _localScaleCorrection = ApplyActualPlayerScale(player, _playerScale);
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
            if (player == null)
                return;

            if (!force &&
                Mathf.Abs(_lastBroadcastPlayerScale - _playerScale) < 0.001f &&
                Mathf.Abs(_playerScale - 1f) < 0.001f)
            {
                return;
            }

            try
            {
                string value = FormatScalePayload(_playerScale, _localScaleCorrection);
                if (LobbyService.Instance.IsInLobby())
                {
                    if (LobbyService.Instance.IsHost())
                    {
                        PlayerValueRpcService.BroadcastToApprovedClients(
                            player, NetworkScaleVariable, value);
                    }
                    else
                    {
                        player.SendValue(NetworkScaleRequestVariable, value, true);
                    }
                }
                _lastBroadcastPlayerScale = _playerScale;
            }
            catch (Exception ex)
            {
                _lastBroadcastPlayerScale = _playerScale;
                DebugLogService.Instance.VerboseWarning("Player scale sync failed: " + ex.Message);
            }
        }

        internal static bool IsNetworkScaleVariable(string variableName)
        {
            return string.Equals(variableName, NetworkScaleVariable,
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(variableName, NetworkScaleRequestVariable,
                       StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TryApplyNetworkScale(Player player, string variableName, string value)
        {
            if (player == null || !IsNetworkScaleVariable(variableName))
                return false;

            if (!TryParseScalePayload(value, out float scale, out float correction))
                return true;

            try
            {
                if (string.Equals(variableName, NetworkScaleRequestVariable,
                    StringComparison.OrdinalIgnoreCase))
                {
                    if (!LobbyService.Instance.IsHost() || player.IsLocalPlayer ||
                        !SessionAuthorityService.Instance.IsClientApproved(player))
                    {
                        return true;
                    }

                    float appliedCorrection = ApplyActualPlayerScale(player, scale, correction);
                    PlayerValueRpcService.BroadcastToApprovedClients(player,
                        NetworkScaleVariable, FormatScalePayload(scale, appliedCorrection));
                    return true;
                }

                // The authoritative value is echoed to its owner. The owner already
                // applied this exact scale and correction before sending the request.
                if (player.IsLocalPlayer)
                    return true;

                ApplyActualPlayerScale(player, scale, correction);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Received player scale apply failed: " + ex.Message);
            }

            return true;
        }

        private static string FormatScalePayload(float scale, float correction)
        {
            return scale.ToString("0.###", CultureInfo.InvariantCulture) + "|" +
                   correction.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        private static bool TryParseScalePayload(string value, out float scale,
            out float correction)
        {
            scale = 1f;
            correction = 0f;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string[] parts = value.Split('|');
            if (!float.TryParse(parts[0], NumberStyles.Float,
                CultureInfo.InvariantCulture, out scale))
            {
                return false;
            }

            if (parts.Length > 1)
                float.TryParse(parts[1], NumberStyles.Float,
                    CultureInfo.InvariantCulture, out correction);

            scale = Mathf.Clamp(scale, 0.25f, 4f);
            correction = Mathf.Clamp(correction, -10f, 10f);
            return true;
        }

        private static float ApplyActualPlayerScale(Player player, float scale,
            float? correctionOverride = null)
        {
            if (player == null)
                return 0f;

            try
            {
                bool isLocalPlayer = player.IsLocalPlayer;
                Transform avatarTransform = player.Avatar?.transform;
                Transform avatarParent = avatarTransform?.parent;
                int playerId = player.GetInstanceID();
                int avatarId = avatarTransform != null ? avatarTransform.GetInstanceID() : 0;
                if (!ScaleAnchors.TryGetValue(playerId, out ScaleAnchorState anchor) ||
                    anchor.AvatarId != avatarId)
                {
                    anchor = new ScaleAnchorState { AvatarId = avatarId };
                    ScaleAnchors[playerId] = anchor;
                }

                if (anchor.HasCorrection &&
                    Mathf.Abs(anchor.Scale - scale) < 0.001f &&
                    (!correctionOverride.HasValue ||
                     Mathf.Abs(anchor.Correction - correctionOverride.Value) < 0.0001f))
                {
                    return anchor.Correction;
                }

                float oldCorrection = anchor.HasCorrection ? anchor.Correction : 0f;
                float baseAvatarY = avatarTransform != null
                    ? avatarTransform.localPosition.y - oldCorrection
                    : 0f;
                float footPlaneBefore = 0f;
                bool hasFootPlane = TryGetFootPlane(player, out footPlaneBefore);
                float previousScale = Mathf.Max(0.001f, Mathf.Abs(player.Scale));
                float controllerBottomBefore = 0f;
                bool hasControllerBottom = isLocalPlayer &&
                    TryGetControllerBottom(player, out controllerBottomBefore);
                float normalizedFootClearance = hasControllerBottom
                    ? (footPlaneBefore - controllerBottomBefore) / previousScale
                    : 0f;

                player.SetScale(scale);
                float localCorrection = correctionOverride ?? 0f;
                if (!correctionOverride.HasValue && hasFootPlane &&
                    TryGetFootPlane(player, out float footPlaneAfter))
                {
                    float targetFootPlane = footPlaneBefore;
                    if (hasControllerBottom && TryGetControllerBottom(player, out float controllerBottomAfter))
                        targetFootPlane = controllerBottomAfter + normalizedFootClearance * scale;
                    localCorrection = WorldToAvatarLocalCorrection(
                        avatarParent, targetFootPlane - footPlaneAfter);
                }

                ApplyFootCorrection(avatarTransform, baseAvatarY, localCorrection);
                anchor.Scale = scale;
                anchor.Correction = localCorrection;
                anchor.HasCorrection = avatarTransform != null;

                if (isLocalPlayer)
                {
                    RestoreVanillaStandingScale();
                    RefreshLocalViewmodel();
                }

                return localCorrection;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Actual player scale apply failed: " + ex.Message);
                return 0f;
            }
        }

        private static bool TryGetControllerBottom(Player player, out float bottom)
        {
            bottom = 0f;
            try
            {
                CharacterController controller = player?.CharacterController;
                if (controller == null)
                    return false;

                bottom = controller.bounds.min.y;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetFootPlane(Player player, out float footPlane)
        {
            footPlane = 0f;
            try
            {
                var avatar = player?.Avatar;
                Transform leftFoot = avatar?.LeftFootBone;
                Transform rightFoot = avatar?.RightFootBone;
                if (leftFoot == null && rightFoot == null)
                    return false;

                footPlane = leftFoot != null ? leftFoot.position.y : rightFoot.position.y;
                if (rightFoot != null)
                    footPlane = Mathf.Min(footPlane, rightFoot.position.y);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static float WorldToAvatarLocalCorrection(Transform parent,
            float verticalCorrection)
        {
            if (parent == null)
                return 0f;

            float parentScaleY = Mathf.Abs(parent.lossyScale.y);
            if (parentScaleY < 0.001f)
                return 0f;

            return verticalCorrection / parentScaleY;
        }

        private static void ApplyFootCorrection(Transform avatarTransform,
            float baseAvatarY, float localCorrection)
        {
            if (avatarTransform == null)
                return;

            Vector3 localPosition = avatarTransform.localPosition;
            localPosition.y = baseAvatarY + localCorrection;
            avatarTransform.localPosition = localPosition;
        }

        private sealed class ScaleAnchorState
        {
            public int AvatarId;
            public float Scale = float.NaN;
            public float Correction;
            public bool HasCorrection;
        }

        private static void RestoreVanillaStandingScale()
        {
            try
            {
                PlayerMovement movement = GetLocalMovement();
                if (movement != null &&
                    Mathf.Abs(movement._StandingScale_k__BackingField - 1f) > 0.001f)
                {
                    movement._StandingScale_k__BackingField = 1f;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Standing scale restore failed: " + ex.Message);
            }
        }

        private static void RefreshLocalViewmodel()
        {
            try
            {
                ViewmodelSway sway = ViewmodelSway.Instance;
                if (sway != null)
                    sway.RefreshViewmodel();
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
                health.TakeDamage(PlayerHealth.MaxHealth + 999f, true, true);
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

    [HarmonyPatch(typeof(Equippable_TrashGrabber), nameof(Equippable_TrashGrabber.GetCapacity))]
    internal static class BottomlessTrashGrabberPatch
    {
        private static void Postfix(ref int __result)
        {
            if (PlayerCheatService.Instance.BottomlessTrashGrabber)
                __result = int.MaxValue;
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

    [HarmonyPatch(typeof(Player), "ReceiveValue", new[] { typeof(string), typeof(string) })]
    internal static class PlayerReceiveValueLocalPatch
    {
        private static bool Prefix(string variableName)
        {
            return !SessionAuthorityService.IsNugzzControlVariable(variableName);
        }

        private static void Postfix(Player __instance, string variableName, string value)
        {
            PlayerNetworkValueDispatcher.Dispatch(__instance, variableName, value,
                "ReceiveValue(local)");
        }
    }

    [HarmonyPatch(typeof(Player), "RpcLogic___ReceiveValue_3895153758",
        new[] { typeof(NetworkConnection), typeof(string), typeof(string) })]
    internal static class PlayerReceiveValueRpcLogicPatch
    {
        private static bool Prefix(Player __instance, string variableName, string value)
        {
            if (!SessionAuthorityService.IsNugzzControlVariable(variableName))
                return true;

            PlayerNetworkValueDispatcher.Dispatch(__instance, variableName, value,
                "RpcLogic(target)");
            return false;
        }
    }

    internal static class PlayerNetworkValueDispatcher
    {
        internal static void Dispatch(Player source, string variableName, string value,
            string receiveHook)
        {
            SessionAuthorityService.Instance.TryReceiveNetworkValue(
                source, variableName, value, receiveHook);
            ShapePrefabService.Instance.TryReceiveNetworkValue(source, variableName, value);
            RelationshipService.Instance.TryReceiveNetworkValue(source, variableName, value);
            PlayerCheatService.TryApplyNetworkScale(source, variableName, value);
            VehicleService.Instance.TryApplyNetworkVehicleTune(source, variableName, value);
        }
    }
}
