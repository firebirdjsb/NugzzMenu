using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.Experimental;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Skating;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Stores independent tuning for every skateboard used during the session.
    /// Each board receives its own runtime settings clone so tuning never leaks to another board.
    /// </summary>
    public sealed class SkateboardTuneService
    {
        private static readonly SkateboardTuneService _instance =
            new SkateboardTuneService();

        private readonly Dictionary<int, BoardProfile> _profiles =
            new Dictionary<int, BoardProfile>();
        private Skateboard _board;
        private BoardProfile _profile;

        public static SkateboardTuneService Instance => _instance;

        public bool UnlimitedJumps { get; set; }
        public float SpeedMultiplier = 1f;
        public float TurnMultiplier = 1f;
        public float PushMultiplier = 1f;
        public float StopMultiplier = 1f;

        private SkateboardTuneService() { }

        public bool HasActiveBoard
        {
            get
            {
                try { return Player.Local != null && Player.Local.ActiveSkateboard != null; }
                catch { return false; }
            }
        }

        public void Update()
        {
            Skateboard current = null;
            try { current = Player.Local?.ActiveSkateboard; }
            catch { }

            if (current != _board)
                SelectBoard(current);
        }

        public void ApplyNow()
        {
            if (_profile == null || _board == null)
                return;

            SaveControls(_profile);
            ApplyToBoard(_profile);
        }

        public void ResetCurrent()
        {
            if (_profile == null || _board == null)
                return;

            _profile.Speed = 1f;
            _profile.Turn = 1f;
            _profile.Push = 1f;
            _profile.Stop = 1f;
            _profile.Baseline.Restore(_board);
            LoadControls(_profile);
            NotificationService.Instance.Status("Active skateboard tuning reset");
        }

        public void ResetAll()
        {
            foreach (BoardProfile profile in _profiles.Values)
            {
                try
                {
                    if (profile?.Board != null)
                        profile.Baseline.Restore(profile.Board);
                }
                catch { }
            }

            _profiles.Clear();
            _board = null;
            _profile = null;
            UnlimitedJumps = false;
            SetDefaultControls();
        }

        public void ResetForScene()
        {
            _profiles.Clear();
            _board = null;
            _profile = null;
            SetDefaultControls();
        }

        private void SelectBoard(Skateboard board)
        {
            if (_profile != null)
                SaveControls(_profile);

            _board = board;
            _profile = null;
            if (board == null)
            {
                SetDefaultControls();
                return;
            }

            int id = board.GetInstanceID();
            if (!_profiles.TryGetValue(id, out _profile) ||
                _profile == null || _profile.Board != board)
            {
                _profile = new BoardProfile(board);
                _profiles[id] = _profile;
            }

            LoadControls(_profile);
            ApplyToBoard(_profile);
        }

        private void ApplyToBoard(BoardProfile profile)
        {
            if (profile?.Board == null || !profile.Baseline.Valid)
                return;

            TunedValues values = GetValues(profile);
            values.Apply(profile.Board, profile.Baseline.PushDuration);
            try
            {
                SkateboardSettings settings = profile.Settings ?? profile.Board.CurentSettings;
                values.Apply(settings, profile.Baseline.PushDuration);
            }
            catch { }
        }

        private static TunedValues GetValues(BoardProfile profile)
        {
            Baseline baseline = profile.Baseline;
            float speed = Mathf.Clamp(profile.Speed, 0.25f, 8f);
            float turn = Mathf.Clamp(profile.Turn, 0.25f, 5f);
            float push = Mathf.Clamp(profile.Push, 0.25f, 8f);
            float stop = Mathf.Clamp(profile.Stop, 0.25f, 8f);

            return new TunedValues
            {
                TopSpeed = baseline.TopSpeed * speed,
                ReverseSpeed = baseline.ReverseSpeed * speed,
                TurnForce = baseline.TurnForce * turn,
                TurnChangeRate = baseline.TurnChangeRate * turn,
                TurnReturnRate = baseline.TurnReturnRate * turn,
                TurnSpeedBoost = baseline.TurnSpeedBoost * turn,
                PushForce = baseline.PushForce * push,
                PushDelay = Mathf.Max(0.02f, baseline.PushDelay / push),
                BrakeForce = baseline.BrakeForce * stop,
                LongitudinalFriction = baseline.LongitudinalFriction * stop,
                LateralFriction = baseline.LateralFriction * stop
            };
        }

        private void SaveControls(BoardProfile profile)
        {
            profile.Speed = SpeedMultiplier;
            profile.Turn = TurnMultiplier;
            profile.Push = PushMultiplier;
            profile.Stop = StopMultiplier;
        }

        private void LoadControls(BoardProfile profile)
        {
            SpeedMultiplier = profile.Speed;
            TurnMultiplier = profile.Turn;
            PushMultiplier = profile.Push;
            StopMultiplier = profile.Stop;
        }

        private void SetDefaultControls()
        {
            SpeedMultiplier = 1f;
            TurnMultiplier = 1f;
            PushMultiplier = 1f;
            StopMultiplier = 1f;
        }

        private sealed class BoardProfile
        {
            public BoardProfile(Skateboard board)
            {
                Board = board;
                Baseline = Baseline.Capture(board);
                try
                {
                    SkateboardSettings current = board?.CurentSettings;
                    Settings = current?.Clone() ?? current;
                    if (board != null && Settings != null)
                        board._settings = Settings;
                }
                catch { }
            }

            public Skateboard Board;
            public SkateboardSettings Settings;
            public Baseline Baseline;
            public float Speed = 1f;
            public float Turn = 1f;
            public float Push = 1f;
            public float Stop = 1f;
        }

        private struct TunedValues
        {
            public float TopSpeed;
            public float ReverseSpeed;
            public float TurnForce;
            public float TurnChangeRate;
            public float TurnReturnRate;
            public float TurnSpeedBoost;
            public float PushForce;
            public float PushDelay;
            public float BrakeForce;
            public float LongitudinalFriction;
            public float LateralFriction;

            public void Apply(Skateboard board, float pushDuration)
            {
                board.TopSpeed_Kmh = TopSpeed;
                board.ReverseTopSpeed_Kmh = ReverseSpeed;
                board.TurnForce = TurnForce;
                board.TurnChangeRate = TurnChangeRate;
                board.TurnReturnToRestRate = TurnReturnRate;
                board.TurnSpeedBoost = TurnSpeedBoost;
                board.PushForceMultiplier = PushForce;
                board.PushForceDuration = pushDuration;
                board.PushDelay = PushDelay;
                board.BrakeForce = BrakeForce;
                board.LongitudinalFrictionMultiplier = LongitudinalFriction;
                board.LateralFrictionForceMultiplier = LateralFriction;
            }

            public void Apply(SkateboardSettings settings, float pushDuration)
            {
                settings.TopSpeed_Kmh = TopSpeed;
                settings.ReverseTopSpeed_Kmh = ReverseSpeed;
                settings.TurnForce = TurnForce;
                settings.TurnChangeRate = TurnChangeRate;
                settings.TurnReturnToRestRate = TurnReturnRate;
                settings.TurnSpeedBoost = TurnSpeedBoost;
                settings.PushForceMultiplier = PushForce;
                settings.PushForceDuration = pushDuration;
                settings.PushDelay = PushDelay;
                settings.BrakeForce = BrakeForce;
                settings.LongitudinalFrictionMultiplier = LongitudinalFriction;
                settings.LateralFrictionForceMultiplier = LateralFriction;
            }
        }

        private struct Baseline
        {
            public bool Valid;
            public float TopSpeed;
            public float ReverseSpeed;
            public float TurnForce;
            public float TurnChangeRate;
            public float TurnReturnRate;
            public float TurnSpeedBoost;
            public float PushForce;
            public float PushDuration;
            public float PushDelay;
            public float BrakeForce;
            public float LongitudinalFriction;
            public float LateralFriction;

            public static Baseline Capture(Skateboard board)
            {
                SkateboardSettings settings = null;
                try { settings = board?.CurentSettings; }
                catch { }

                return new Baseline
                {
                    Valid = board != null,
                    TopSpeed = settings?.TopSpeed_Kmh ?? board.TopSpeed_Kmh,
                    ReverseSpeed = settings?.ReverseTopSpeed_Kmh ?? board.ReverseTopSpeed_Kmh,
                    TurnForce = settings?.TurnForce ?? board.TurnForce,
                    TurnChangeRate = settings?.TurnChangeRate ?? board.TurnChangeRate,
                    TurnReturnRate = settings?.TurnReturnToRestRate ?? board.TurnReturnToRestRate,
                    TurnSpeedBoost = settings?.TurnSpeedBoost ?? board.TurnSpeedBoost,
                    PushForce = settings?.PushForceMultiplier ?? board.PushForceMultiplier,
                    PushDuration = settings?.PushForceDuration ?? board.PushForceDuration,
                    PushDelay = settings?.PushDelay ?? board.PushDelay,
                    BrakeForce = settings?.BrakeForce ?? board.BrakeForce,
                    LongitudinalFriction = settings?.LongitudinalFrictionMultiplier ??
                        board.LongitudinalFrictionMultiplier,
                    LateralFriction = settings?.LateralFrictionForceMultiplier ??
                        board.LateralFrictionForceMultiplier
                };
            }

            public void Restore(Skateboard board)
            {
                if (!Valid || board == null)
                    return;

                board.TopSpeed_Kmh = TopSpeed;
                board.ReverseTopSpeed_Kmh = ReverseSpeed;
                board.TurnForce = TurnForce;
                board.TurnChangeRate = TurnChangeRate;
                board.TurnReturnToRestRate = TurnReturnRate;
                board.TurnSpeedBoost = TurnSpeedBoost;
                board.PushForceMultiplier = PushForce;
                board.PushForceDuration = PushDuration;
                board.PushDelay = PushDelay;
                board.BrakeForce = BrakeForce;
                board.LongitudinalFrictionMultiplier = LongitudinalFriction;
                board.LateralFrictionForceMultiplier = LateralFriction;

                try
                {
                    SkateboardSettings settings = board.CurentSettings;
                    if (settings == null)
                        return;
                    settings.TopSpeed_Kmh = TopSpeed;
                    settings.ReverseTopSpeed_Kmh = ReverseSpeed;
                    settings.TurnForce = TurnForce;
                    settings.TurnChangeRate = TurnChangeRate;
                    settings.TurnReturnToRestRate = TurnReturnRate;
                    settings.TurnSpeedBoost = TurnSpeedBoost;
                    settings.PushForceMultiplier = PushForce;
                    settings.PushForceDuration = PushDuration;
                    settings.PushDelay = PushDelay;
                    settings.BrakeForce = BrakeForce;
                    settings.LongitudinalFrictionMultiplier = LongitudinalFriction;
                    settings.LateralFrictionForceMultiplier = LateralFriction;
                }
                catch { }
            }
        }
    }

    [HarmonyPatch(typeof(Skateboard), "CheckJump")]
    internal static class SkateboardUnlimitedJumpPatch
    {
        private static bool Prefix(Skateboard __instance)
        {
            SkateboardTuneService service = SkateboardTuneService.Instance;
            if (!service.UnlimitedJumps ||
                GameplayStateGateService.Instance.AreGameplayActionsBlocked() ||
                !Input.GetKeyDown(KeyCode.Space))
                return true;

            try
            {
                Player player = Player.Local;
                if (player == null || player.ActiveSkateboard != __instance ||
                    __instance.IsGrounded() || __instance.Rb == null)
                    return true;

                float force = Mathf.Clamp(__instance.JumpForce * 0.5f, 3.5f, 12f);
                Vector3 velocity = __instance.Rb.velocity;
                velocity.y = Mathf.Min(25f,
                    Mathf.Max(force, velocity.y + force * 0.65f));
                __instance.Rb.velocity = velocity;
                try { __instance.OnJump?.Invoke(__instance.JumpDuration_Min); }
                catch { }
                return false;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException(
                    "Unlimited skateboard jump failed", ex);
                return true;
            }
        }
    }
}
