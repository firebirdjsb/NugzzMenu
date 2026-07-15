using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class CameraStateRestoreService
    {
        private static readonly CameraStateRestoreService _instance = new CameraStateRestoreService();
        public static CameraStateRestoreService Instance => _instance;

        private bool _captured;
        private PlayerCamera _playerCamera;
        private Transform _cameraContainer;
        private Transform _cameraTransform;
        private Player _player;
        private Vector3 _containerLocalPosition;
        private Quaternion _containerLocalRotation;
        private Vector3 _cameraLocalPosition;
        private Quaternion _cameraLocalRotation;
        private Vector3 _playerCameraPosition;
        private Quaternion _playerCameraRotation;
        private Transform _mimicCamera;
        private Vector3 _mimicCameraPosition;
        private Quaternion _mimicCameraRotation;
        private float _fieldOfView;

        private CameraStateRestoreService() { }

        public void Capture(PlayerCamera playerCamera)
        {
            if (_captured || playerCamera == null)
                return;

            try
            {
                _playerCamera = playerCamera;
                _cameraContainer = playerCamera.CameraContainer;
                _cameraTransform = playerCamera.Camera != null
                    ? playerCamera.Camera.transform
                    : null;

                if (_cameraContainer != null)
                {
                    _containerLocalPosition = _cameraContainer.localPosition;
                    _containerLocalRotation = _cameraContainer.localRotation;
                }

                if (_cameraTransform != null)
                {
                    _cameraLocalPosition = _cameraTransform.localPosition;
                    _cameraLocalRotation = _cameraTransform.localRotation;
                }

                if (playerCamera.Camera != null)
                    _fieldOfView = playerCamera.Camera.fieldOfView;

                CapturePlayerCameraState();
                _captured = true;
            }
            catch
            {
                _captured = false;
            }
        }

        public void Restore(bool menuOpen)
        {
            PlayerCamera playerCamera = _playerCamera != null
                ? _playerCamera
                : PlayerCamera.Instance;

            try
            {
                if (playerCamera != null && playerCamera.transformOverriden)
                    playerCamera.StopTransformOverride(0f, true, false);
            }
            catch { }

            try
            {
                if (playerCamera != null && playerCamera.fovOverriden)
                    playerCamera.StopFOVOverride(0f);
            }
            catch { }

            try
            {
                if (_captured && _cameraContainer != null)
                {
                    _cameraContainer.localPosition = _containerLocalPosition;
                    _cameraContainer.localRotation = _containerLocalRotation;
                }
            }
            catch { }

            try
            {
                if (_captured && _cameraTransform != null)
                {
                    _cameraTransform.localPosition = _cameraLocalPosition;
                    _cameraTransform.localRotation = _cameraLocalRotation;
                }
            }
            catch { }

            try
            {
                if (_captured && playerCamera?.Camera != null && _fieldOfView > 1f)
                    playerCamera.Camera.fieldOfView = _fieldOfView;
            }
            catch { }

            RestorePlayerCameraState();
            try { playerCamera?.SetCanLook(!menuOpen); } catch { }

            Clear();
        }

        public void ReleaseToNative(bool canLook)
        {
            PlayerCamera playerCamera = _playerCamera != null
                ? _playerCamera
                : PlayerCamera.Instance;

            try
            {
                if (playerCamera != null && playerCamera.transformOverriden)
                    playerCamera.StopTransformOverride(0f, canLook, false);
            }
            catch { }

            try
            {
                if (playerCamera != null && playerCamera.fovOverriden)
                    playerCamera.StopFOVOverride(0f);
            }
            catch { }

            try { playerCamera?.SetCanLook(canLook); } catch { }

            Clear();
        }

        private void CapturePlayerCameraState()
        {
            try
            {
                _player = ManagerCacheService.Instance.LocalPlayer;
                if (_player == null)
                    return;

                _playerCameraPosition = _player.CameraPosition;
                _playerCameraRotation = _player.CameraRotation;
                _mimicCamera = _player.MimicCamera;
                if (_mimicCamera != null)
                {
                    _mimicCameraPosition = _mimicCamera.position;
                    _mimicCameraRotation = _mimicCamera.rotation;
                }
            }
            catch { }
        }

        private void RestorePlayerCameraState()
        {
            if (!_captured || _player == null)
                return;

            try
            {
                _player.CameraPosition = _playerCameraPosition;
                _player.CameraRotation = _playerCameraRotation;
            }
            catch { }

            try
            {
                if (_mimicCamera != null)
                {
                    _mimicCamera.position = _mimicCameraPosition;
                    _mimicCamera.rotation = _mimicCameraRotation;
                }
            }
            catch { }
        }

        private void Clear()
        {
            _captured = false;
            _playerCamera = null;
            _cameraContainer = null;
            _cameraTransform = null;
            _player = null;
            _mimicCamera = null;
        }
    }
}
