using System;
using System.Reflection;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;
using static UnityEngine.Object;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Keeps the vanilla vehicle camera in its normal follow state while Nugzz owns the cursor.
    /// </summary>
    public sealed class VehicleMenuCameraService
    {
        private static readonly VehicleMenuCameraService _instance = new VehicleMenuCameraService();
        public static VehicleMenuCameraService Instance => _instance;

        private MethodInfo _forceCameraReturnMethod;
        private FieldInfo _timeSinceManualField;
        private FieldInfo _lastManualOffsetField;
        private FieldInfo _lastFrameCameraOffsetField;

        private LandVehicle _controlledVehicle;
        private bool _capturedControls;
        private bool _previousOverrideControls;
        private float _previousThrottleOverride;
        private float _previousSteerOverride;
        private bool _previousHandbrakeOverride;

        private LandVehicle _lastVehicle;
        private int _settleFramesRemaining;

        private VehicleMenuCameraService() { }

        public void NotifyMenuStateChanged(bool isOpen, bool wasOpen)
        {
            if (isOpen)
            {
                LandVehicle vehicle = GetLocalDrivenVehicle();
                if (vehicle != null)
                {
                    _lastVehicle = vehicle;
                    ReturnVanillaVehicleCamera(vehicle);
                }
                return;
            }

            if (wasOpen)
            {
                RestoreVehicleControls();
                LandVehicle vehicle = GetLocalDrivenVehicle() ?? _lastVehicle;
                if (vehicle != null)
                {
                    _lastVehicle = vehicle;
                    _settleFramesRemaining = 2;
                    ReturnVanillaVehicleCamera(vehicle);
                }
            }
        }

        public void Update(bool menuOpen)
        {
            LandVehicle vehicle = GetLocalDrivenVehicle();
            if (menuOpen && vehicle != null)
            {
                _lastVehicle = vehicle;
                CaptureAndNeutralizeVehicleControls(vehicle);
                ReturnVanillaVehicleCamera(vehicle);
                return;
            }

            RestoreVehicleControls();

            if (_settleFramesRemaining > 0)
            {
                _settleFramesRemaining--;
                ResetManualCameraState(GetVehicleCamera(vehicle ?? _lastVehicle));
            }

            _lastVehicle = vehicle ?? _lastVehicle;
        }

        public void LateUpdate(bool menuOpen)
        {
            if (!menuOpen)
                return;

            ReturnVanillaVehicleCamera(GetLocalDrivenVehicle() ?? _lastVehicle);
        }

        public void Reset()
        {
            RestoreVehicleControls();
            _lastVehicle = null;
            _settleFramesRemaining = 0;
        }

        private void CaptureAndNeutralizeVehicleControls(LandVehicle vehicle)
        {
            if (vehicle == null)
                return;

            if (_capturedControls && _controlledVehicle != vehicle)
                RestoreVehicleControls();

            if (!_capturedControls)
            {
                _controlledVehicle = vehicle;
                try { _previousOverrideControls = vehicle.overrideControls; } catch { }
                try { _previousThrottleOverride = vehicle.throttleOverride; } catch { }
                try { _previousSteerOverride = vehicle.steerOverride; } catch { }
                try { _previousHandbrakeOverride = vehicle.handbrakeOverride; } catch { }
                _capturedControls = true;
            }

            try { vehicle.overrideControls = true; } catch { }
            try { vehicle.throttleOverride = 0f; } catch { }
            try { vehicle.steerOverride = 0f; } catch { }
            try { vehicle.handbrakeOverride = true; } catch { }
        }

        private void RestoreVehicleControls()
        {
            if (!_capturedControls)
                return;

            LandVehicle vehicle = _controlledVehicle;
            _capturedControls = false;
            _controlledVehicle = null;

            if (vehicle == null)
                return;

            try { vehicle.throttleOverride = _previousThrottleOverride; } catch { }
            try { vehicle.steerOverride = _previousSteerOverride; } catch { }
            try { vehicle.handbrakeOverride = _previousHandbrakeOverride; } catch { }
            try { vehicle.overrideControls = _previousOverrideControls; } catch { }
        }

        private void ReturnVanillaVehicleCamera(LandVehicle vehicle)
        {
            VehicleCamera vehicleCamera = GetVehicleCamera(vehicle);
            if (vehicleCamera == null)
                return;

            ResetManualCameraState(vehicleCamera);
            EnsureVehicleCameraMode();

            try
            {
                MethodInfo method = GetForceCameraReturnMethod();
                method?.Invoke(vehicleCamera, null);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Vehicle camera return failed: " + ex.Message);
            }
        }

        private void ResetManualCameraState(VehicleCamera vehicleCamera)
        {
            if (vehicleCamera == null)
                return;

            try { GetTimeSinceManualField()?.SetValue(vehicleCamera, 999f); } catch { }
            try { GetLastManualOffsetField()?.SetValue(vehicleCamera, Vector3.zero); } catch { }
            try { GetLastFrameCameraOffsetField()?.SetValue(vehicleCamera, Vector3.zero); } catch { }
        }

        private static void EnsureVehicleCameraMode()
        {
            try
            {
                PlayerCamera camera = PlayerCamera.Instance;
                if (camera != null && camera.CameraMode != PlayerCamera.ECameraMode.Vehicle)
                    camera.SetCameraMode(PlayerCamera.ECameraMode.Vehicle);
            }
            catch { }
        }

        private MethodInfo GetForceCameraReturnMethod()
        {
            if (_forceCameraReturnMethod != null)
                return _forceCameraReturnMethod;

            _forceCameraReturnMethod = typeof(VehicleCamera).GetMethod(
                "ForceCameraReturn",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return _forceCameraReturnMethod;
        }

        private FieldInfo GetTimeSinceManualField()
        {
            if (_timeSinceManualField != null)
                return _timeSinceManualField;

            _timeSinceManualField = typeof(VehicleCamera).GetField(
                "timeSinceCameraManuallyAdjusted",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return _timeSinceManualField;
        }

        private FieldInfo GetLastManualOffsetField()
        {
            if (_lastManualOffsetField != null)
                return _lastManualOffsetField;

            _lastManualOffsetField = typeof(VehicleCamera).GetField(
                "lastManualOffset",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return _lastManualOffsetField;
        }

        private FieldInfo GetLastFrameCameraOffsetField()
        {
            if (_lastFrameCameraOffsetField != null)
                return _lastFrameCameraOffsetField;

            _lastFrameCameraOffsetField = typeof(VehicleCamera).GetField(
                "lastFrameCameraOffset",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return _lastFrameCameraOffsetField;
        }

        private static LandVehicle GetLocalDrivenVehicle()
        {
            try
            {
                Player player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null || !player.IsInVehicle)
                    return null;

                if (player.CurrentVehicle != null)
                {
                    LandVehicle vehicle = player.CurrentVehicle.GetComponent<LandVehicle>();
                    if (vehicle != null)
                        return vehicle;
                }

                if (player.CurrentVehicleSeat != null)
                    return player.CurrentVehicleSeat.GetComponentInParent<LandVehicle>();
            }
            catch { }

            return null;
        }

        private static VehicleCamera GetVehicleCamera(LandVehicle vehicle)
        {
            if (vehicle == null)
                return null;

            try
            {
                VehicleCamera camera = vehicle.GetComponentInChildren<VehicleCamera>(true);
                if (camera != null)
                    return camera;
            }
            catch { }

            try
            {
                VehicleCamera[] cameras = FindObjectsOfType<VehicleCamera>(true);
                if (cameras == null)
                    return null;

                for (int i = 0; i < cameras.Length; i++)
                {
                    VehicleCamera camera = cameras[i];
                    if (camera == null)
                        continue;

                    try
                    {
                        if (camera.vehicle == vehicle)
                            return camera;
                    }
                    catch { }
                }
            }
            catch { }

            return null;
        }
    }
}
