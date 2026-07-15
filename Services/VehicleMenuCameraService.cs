using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Keeps the vanilla vehicle camera in its normal follow state while Nugzz owns the cursor.
    /// </summary>
    public sealed class VehicleMenuCameraService
    {
        private static readonly VehicleMenuCameraService _instance = new VehicleMenuCameraService();
        public static VehicleMenuCameraService Instance => _instance;

        private LandVehicle _controlledVehicle;
        private bool _capturedControls;
        private bool _previousOverrideControls;
        private float _previousThrottleOverride;
        private float _previousSteerOverride;
        private bool _previousHandbrakeOverride;

        private bool _blockCameraInput;

        private VehicleMenuCameraService() { }

        public bool ShouldBlockCameraInput => _blockCameraInput;

        public void MaintainDrivingLock(LandVehicle vehicle)
        {
            if (!_blockCameraInput || vehicle == null)
                return;

            LandVehicle drivenVehicle = GetLocalDrivenVehicle();
            if (drivenVehicle == vehicle)
                CaptureAndNeutralizeVehicleControls(vehicle);
        }

        public void NotifyMenuStateChanged(bool isOpen, bool wasOpen)
        {
            if (isOpen)
            {
                LandVehicle vehicle = GetLocalDrivenVehicle();
                if (vehicle != null)
                {
                    _blockCameraInput = true;
                    ThirdPersonCameraService.Instance.ForceDisableForVehicle(true);
                    CaptureAndNeutralizeVehicleControls(vehicle);
                }

                return;
            }

            _blockCameraInput = false;
            if (wasOpen)
                RestoreVehicleControls();
        }

        public void Update(bool menuOpen)
        {
            LandVehicle vehicle = GetLocalDrivenVehicle();
            _blockCameraInput = menuOpen && vehicle != null;
            if (vehicle != null)
                ThirdPersonCameraService.Instance.ForceDisableForVehicle(menuOpen);

            if (menuOpen && vehicle != null)
            {
                CaptureAndNeutralizeVehicleControls(vehicle);
                return;
            }

            RestoreVehicleControls();
        }

        public void LateUpdate(bool menuOpen)
        {
            // The native VehicleCamera owns its transform and orbit state.
        }

        public void Reset()
        {
            RestoreVehicleControls();
            _blockCameraInput = false;
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

        private static LandVehicle GetLocalDrivenVehicle()
        {
            try
            {
                Player player = ManagerCacheService.Instance.LocalPlayer;
                if (player != null && player.IsInVehicle)
                {
                    if (player.CurrentVehicle != null)
                    {
                        LandVehicle vehicle = player.CurrentVehicle.GetComponent<LandVehicle>();
                        if (vehicle != null)
                            return vehicle;
                    }

                    if (player.CurrentVehicleSeat != null)
                        return player.CurrentVehicleSeat.GetComponentInParent<LandVehicle>();
                }
            }
            catch { }

            try
            {
                PlayerMovement movement = PlayerMovement.Instance;
                if (movement != null && movement.CurrentVehicle != null)
                    return movement.CurrentVehicle.GetComponent<LandVehicle>();
            }
            catch { }

            return null;
        }
    }
}
