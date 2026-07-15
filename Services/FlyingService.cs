using System;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;
using static UnityEngine.Object;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Manages flying/noclip movement for the player.
    /// </summary>
    public sealed class FlyingService
    {
        private static readonly FlyingService _instance = new FlyingService();
        public static FlyingService Instance => _instance;

        private bool _enabled = false;
        private float _speed = 20f;
        private bool _gravitySuppressed = false;
        private float _previousGravityMultiplier = 1.4f;
        private Player _cachedPlayer;
        private CharacterController _cachedController;
        private bool _controllerWasEnabled;
        private bool _controllerDisabledForFly;
        private Camera _cachedCamera;
        private Vector3 _lastFlyPosition;
        private bool _hasFlyPosition;
        private bool _vehicleFlyEnabled;
        private bool _vehicleFlyActive;
        private Rigidbody _vehicleFlyBody;
        private bool _vehicleFlyBodyHadGravity;
        private float _lastSpaceTapTime = -10f;
        private float _lastSpaceToggleTime = -10f;
        private const float DoubleSpaceWindowSeconds = 0.32f;
        private const float SpaceToggleCooldownSeconds = 0.45f;

        public bool Enabled => _enabled;
        public bool DoubleSpaceHotkeyEnabled { get; private set; } = true;
        public bool VehicleFlyEnabled => _vehicleFlyEnabled;
        public float Speed
        {
            get => _speed;
            set => _speed = Mathf.Clamp(value, 1f, 500f);
        }

        private FlyingService() { }

        public void SetEnabled(bool enabled)
        {
            if (_enabled == enabled)
                return;

            _enabled = enabled;
            if (_enabled && _speed <= 0f)
                _speed = 20f;

            if (_enabled)
            {
                CacheFlyComponents();
                SuppressGravity();
                DisableControllerForFly();
                _hasFlyPosition = false;
            }
            else
            {
                RestoreVehicleFlyPhysics();
                RestoreControllerAfterFly();
                RestoreGravity();
                _cachedPlayer = null;
                _cachedController = null;
                _cachedCamera = null;
                _hasFlyPosition = false;
            }
        }

        public void SetSpeed(float speed)
        {
            Speed = speed;
        }

        public void SetDoubleSpaceHotkeyEnabled(bool enabled)
        {
            DoubleSpaceHotkeyEnabled = enabled;
            if (!enabled)
                ResetSpaceTap();
        }

        public void SetVehicleFlyEnabled(bool enabled)
        {
            _vehicleFlyEnabled = enabled;
            if (!enabled)
                RestoreVehicleFlyPhysics();
        }

        public void UpdateHotkeys(bool menuOpen)
        {
            if (menuOpen || !DoubleSpaceHotkeyEnabled)
            {
                ResetSpaceTap();
                return;
            }

            if (!UnityEngine.Input.GetKeyDown(KeyCode.Space))
                return;

            if (ManagerCacheService.Instance.LocalPlayer == null)
            {
                ResetSpaceTap();
                return;
            }

            float now = Time.unscaledTime;
            if (now - _lastSpaceToggleTime < SpaceToggleCooldownSeconds)
            {
                _lastSpaceTapTime = now;
                return;
            }

            if (now - _lastSpaceTapTime <= DoubleSpaceWindowSeconds)
            {
                SetEnabled(!_enabled);
                NotificationService.Instance.Status(_enabled ? "Fly ON" : "Fly OFF");
                _lastSpaceToggleTime = now;
                ResetSpaceTap();
                return;
            }

            _lastSpaceTapTime = now;
        }

        public void ApplyFlyMovement()
        {
            try
            {
                var player = GetPlayer();
                if (player == null) return;

                if (_vehicleFlyEnabled && TryApplyVehicleFlyMovement())
                    return;

                RestoreVehicleFlyPhysics();
                if (!_controllerDisabledForFly)
                    DisableControllerForFly();

                var camera = GetCamera();
                if (camera == null) return;

                float moveAmount = _speed * Time.unscaledDeltaTime;
                Vector3 forward = camera.transform.forward;
                Vector3 right = camera.transform.right;

                Vector3 velocity = Vector3.zero;

                if (UnityEngine.Input.GetKey(KeyCode.W))
                {
                    velocity += forward * moveAmount;
                }
                if (UnityEngine.Input.GetKey(KeyCode.S))
                {
                    velocity -= forward * moveAmount;
                }
                if (UnityEngine.Input.GetKey(KeyCode.A))
                {
                    velocity -= right * moveAmount;
                }
                if (UnityEngine.Input.GetKey(KeyCode.D))
                {
                    velocity += right * moveAmount;
                }
                if (UnityEngine.Input.GetKey(KeyCode.Space))
                {
                    velocity += Vector3.up * moveAmount;
                }
                if (UnityEngine.Input.GetKey(KeyCode.LeftControl))
                {
                    velocity -= Vector3.up * moveAmount;
                }

                if (velocity != Vector3.zero)
                {
                    MovePlayer(player, velocity);
                }
                else if (_hasFlyPosition)
                {
                    // The game's movement loop can still apply tiny gravity deltas from cached state.
                    // Pin the transform when no fly input is provided so the player never slowly sinks.
                    player.transform.position = _lastFlyPosition;
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Status($"Fly error: {ex.Message}");
                _enabled = false;
                RestoreControllerAfterFly();
                RestoreGravity();
            }
        }

        public void ApplyPostMovementLock()
        {
            if (_vehicleFlyActive)
                return;

            if (!_enabled || !_hasFlyPosition)
                return;

            try
            {
                var player = GetPlayer();
                if (player != null)
                    player.transform.position = _lastFlyPosition;
            }
            catch { }
        }

        private void MovePlayer(Player player, Vector3 delta)
        {
            if (player == null)
                return;

            player.transform.position += delta;
            _lastFlyPosition = player.transform.position;
            _hasFlyPosition = true;
        }

        private bool TryApplyVehicleFlyMovement()
        {
            LandVehicle vehicle = GetDrivenVehicle();
            if (vehicle == null)
            {
                RestoreVehicleFlyPhysics();
                return false;
            }

            Camera camera = GetCamera();
            if (camera == null)
                return false;

            Vector3 delta = BuildFlyDelta(camera);
            Rigidbody body = GetVehicleBody(vehicle);
            CaptureVehicleFlyBody(body);

            if (body != null)
            {
                try
                {
                    body.useGravity = false;
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    if (delta != Vector3.zero)
                        body.MovePosition(body.position + delta);
                }
                catch
                {
                    if (delta != Vector3.zero)
                        vehicle.transform.position += delta;
                }
            }
            else if (delta != Vector3.zero)
            {
                vehicle.transform.position += delta;
            }

            _vehicleFlyActive = true;
            _hasFlyPosition = false;
            return true;
        }

        private Vector3 BuildFlyDelta(Camera camera)
        {
            float moveAmount = _speed * Time.unscaledDeltaTime;
            Vector3 forward = camera.transform.forward;
            Vector3 right = camera.transform.right;
            Vector3 delta = Vector3.zero;

            if (UnityEngine.Input.GetKey(KeyCode.W)) delta += forward * moveAmount;
            if (UnityEngine.Input.GetKey(KeyCode.S)) delta -= forward * moveAmount;
            if (UnityEngine.Input.GetKey(KeyCode.A)) delta -= right * moveAmount;
            if (UnityEngine.Input.GetKey(KeyCode.D)) delta += right * moveAmount;
            if (UnityEngine.Input.GetKey(KeyCode.Space)) delta += Vector3.up * moveAmount;
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl)) delta -= Vector3.up * moveAmount;

            return delta;
        }

        private void CaptureVehicleFlyBody(Rigidbody body)
        {
            if (body == null || body == _vehicleFlyBody)
                return;

            RestoreVehicleFlyPhysics();
            _vehicleFlyBody = body;
            try { _vehicleFlyBodyHadGravity = body.useGravity; }
            catch { _vehicleFlyBodyHadGravity = true; }
        }

        private void RestoreVehicleFlyPhysics()
        {
            try
            {
                if (_vehicleFlyBody != null)
                    _vehicleFlyBody.useGravity = _vehicleFlyBodyHadGravity;
            }
            catch { }

            _vehicleFlyBody = null;
            _vehicleFlyBodyHadGravity = true;
            _vehicleFlyActive = false;
        }

        private static Rigidbody GetVehicleBody(LandVehicle vehicle)
        {
            if (vehicle == null)
                return null;

            try
            {
                Rigidbody body = vehicle.GetComponent<Rigidbody>();
                if (body != null)
                    return body;
            }
            catch { }

            try { return vehicle.GetComponentInChildren<Rigidbody>(true); }
            catch { return null; }
        }

        private static LandVehicle GetDrivenVehicle()
        {
            try
            {
                var manager = ManagerCacheService.Instance.VehicleManager ?? FindObjectOfType<VehicleManager>();
                var vehicles = manager?.AllVehicles;
                if (vehicles != null)
                {
                    for (int i = 0; i < vehicles.Count; i++)
                    {
                        LandVehicle vehicle = vehicles[i];
                        if (vehicle != null && vehicle.LocalPlayerIsDriver)
                            return vehicle;
                    }
                }
            }
            catch { }

            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                var seat = player?.CurrentVehicleSeat;
                if (seat != null && seat.isDriverSeat)
                    return seat.GetComponentInParent<LandVehicle>(true);
            }
            catch { }

            return null;
        }

        private void ResetSpaceTap()
        {
            _lastSpaceTapTime = -10f;
        }

        private void SuppressGravity()
        {
            try
            {
                if (!_gravitySuppressed)
                {
                    _previousGravityMultiplier = PlayerMovement.GravityMultiplier;
                    if (_previousGravityMultiplier <= 0f)
                        _previousGravityMultiplier = 1.4f;
                    _gravitySuppressed = true;
                }

                PlayerMovement.GravityMultiplier = 0f;

                var player = GetPlayer();
                if (player != null)
                    player.SetGravityMultiplier(0f);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Fly gravity suppress failed: " + ex.Message);
            }
        }

        private void RestoreGravity()
        {
            try
            {
                float restore = _previousGravityMultiplier > 0f ? _previousGravityMultiplier : 1.4f;
                PlayerMovement.GravityMultiplier = restore;

                var player = GetPlayer();
                if (player != null)
                    player.SetGravityMultiplier(restore);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Fly gravity restore failed: " + ex.Message);
            }
            finally
            {
                _gravitySuppressed = false;
            }
        }

        private Player GetPlayer()
        {
            if (_cachedPlayer != null)
                return _cachedPlayer;

            _cachedPlayer = ManagerCacheService.Instance.LocalPlayer;
            return _cachedPlayer;
        }

        private Camera GetCamera()
        {
            if (_cachedCamera != null)
                return _cachedCamera;

            _cachedCamera = Camera.main;
            return _cachedCamera;
        }

        private void CacheFlyComponents()
        {
            try
            {
                _cachedPlayer = ManagerCacheService.Instance.LocalPlayer;
                if (_cachedPlayer != null)
                {
                    _cachedController = _cachedPlayer.CharacterController;
                    if (_cachedController == null)
                        _cachedController = _cachedPlayer.GetComponent<CharacterController>();
                    _lastFlyPosition = _cachedPlayer.transform.position;
                    _hasFlyPosition = true;
                }
                _cachedCamera = Camera.main;
            }
            catch { }
        }

        private void DisableControllerForFly()
        {
            try
            {
                if (_cachedController == null)
                    CacheFlyComponents();

                if (_cachedController == null || _controllerDisabledForFly)
                    return;

                _controllerWasEnabled = _cachedController.enabled;
                _cachedController.enabled = false;
                _controllerDisabledForFly = true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Fly controller disable failed: " + ex.Message);
            }
        }

        private void RestoreControllerAfterFly()
        {
            try
            {
                if (_cachedController != null && _controllerDisabledForFly)
                    _cachedController.enabled = _controllerWasEnabled;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Fly controller restore failed: " + ex.Message);
            }
            finally
            {
                _controllerDisabledForFly = false;
            }
        }
    }
}
