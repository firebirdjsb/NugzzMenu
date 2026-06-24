using System.Collections;
using Il2CppScheduleOne.PlayerScripts;
using MelonLoader;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class InputLockService
    {
        private static readonly InputLockService _instance = new InputLockService();
        public static InputLockService Instance => _instance;

        private bool _routineRunning;
        private bool _capturedMovement;
        private bool _previousCanMove = true;
        private bool _previousCanJump = true;
        private float _lockedUntilRealtime;

        private InputLockService() { }

        public bool IsLocked => Time.realtimeSinceStartup < _lockedUntilRealtime;

        public void LockFor(float seconds)
        {
            if (seconds <= 0f)
                return;

            _lockedUntilRealtime = Mathf.Max(_lockedUntilRealtime, Time.realtimeSinceStartup + seconds);
            CaptureMovementState();
            ApplyLockState();

            if (!_routineRunning)
                MelonCoroutines.Start(ReleaseWhenReady());
        }

        private IEnumerator ReleaseWhenReady()
        {
            _routineRunning = true;

            while (Time.realtimeSinceStartup < _lockedUntilRealtime)
            {
                ApplyLockState();
                yield return null;
            }

            RestoreMovementState();
            RestoreLookIfGameplayHasFocus();
            _routineRunning = false;
        }

        private void CaptureMovementState()
        {
            if (_capturedMovement)
                return;

            try
            {
                PlayerMovement movement = PlayerMovement.Instance;
                if (movement == null)
                    return;

                _previousCanMove = movement.CanMove;
                _previousCanJump = movement.CanJump;
                _capturedMovement = true;
            }
            catch { }
        }

        private static void ApplyLockState()
        {
            try
            {
                PlayerMovement movement = PlayerMovement.Instance;
                if (movement != null)
                {
                    movement.CanMove = false;
                    movement.CanJump = false;
                }
            }
            catch { }

            try { PlayerCamera.Instance?.SetCanLook(false); } catch { }
        }

        private void RestoreMovementState()
        {
            try
            {
                PlayerMovement movement = PlayerMovement.Instance;
                if (movement != null && _capturedMovement)
                {
                    movement.CanMove = _previousCanMove;
                    movement.CanJump = _previousCanJump;
                }
            }
            catch { }

            _capturedMovement = false;
        }

        private static void RestoreLookIfGameplayHasFocus()
        {
            try
            {
                if (Cursor.visible || Cursor.lockState != CursorLockMode.Locked)
                    return;

                if (CameraService.Instance.ThirdPersonEnabled)
                    return;

                PlayerCamera.Instance?.SetCanLook(true);
            }
            catch { }
        }
    }
}
