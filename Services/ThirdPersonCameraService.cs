using System;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class ThirdPersonCameraService
    {
        private static readonly ThirdPersonCameraService _instance = new ThirdPersonCameraService();
        public static ThirdPersonCameraService Instance => _instance;

        private bool _enabled;
        private bool _overrideActive;
        private bool _anglesReady;
        private bool _menuOpen;
        private int _lastInputFrame = -1;
        private int _nativeToolRaycastFrame = -1;
        private bool _nativeToolRaycastActive;
        private float _yaw;
        private float _pitch;
        private float _distance = 1.90f;
        private float _height = 0.80f;
        private float _shoulderOffset = 0.20f;

        private ThirdPersonCameraService() { }

        public bool Enabled => _enabled;
        public bool OverrideActive => _overrideActive;
        public float Distance => _distance;
        public float Height => _height;
        public float ShoulderOffset => _shoulderOffset;
        public bool IsCombatOverrideActive => _enabled && _overrideActive;
        public bool IsSkateboardActive =>
            IsLocalPlayerSkating(ManagerCacheService.Instance.LocalPlayer) ||
            IsSkateboardCamera(PlayerCamera.Instance);
        public bool ShouldUseVanillaToolRaycasts => IsNativeToolRaycastActive();
        public bool ShouldUseCustomInteractionRaycasts =>
            _enabled &&
            _overrideActive &&
            !ManagementClipboardService.Instance.IsActive() &&
            !IsBuildPlacementActive() &&
            !IsNativeToolRaycastActive();

        public void SetDistance(float value) => _distance = Mathf.Clamp(value, 1.5f, 6f);
        public void SetHeight(float value) => _height = Mathf.Clamp(value, 0.8f, 2.4f);
        public void SetShoulderOffset(float value) => _shoulderOffset = Mathf.Clamp(value, -1f, 1f);

        public bool CanEnable(out string reason)
        {
            reason = null;
            PlayerCamera playerCamera = PlayerCamera.Instance;
            Player player = ManagerCacheService.Instance.LocalPlayer;

            if (ManagementClipboardService.Instance.IsActive())
            {
                reason = "clipboard";
                return false;
            }

            if (IsBuildPlacementActive())
            {
                reason = "building";
                return false;
            }

            if (IsLocalPlayerInVehicle(player) || IsVehicleCamera(playerCamera))
            {
                reason = "vehicle";
                return false;
            }

            if (IsSkateboardActive)
            {
                reason = "skateboard";
                return false;
            }

            if (IsNativeExternalCamera(playerCamera))
            {
                reason = "vanilla camera";
                return false;
            }

            return true;
        }

        public void Toggle(bool enabled, bool menuOpen)
        {
            _menuOpen = menuOpen;
            if (!enabled)
            {
                Disable(menuOpen);
                NotificationService.Instance.Status("Camera: 1st Person");
                return;
            }

            if (!CanEnable(out string reason))
            {
                if (_enabled || _overrideActive)
                {
                    if (reason == "skateboard")
                        DisableForSkateboard(menuOpen);
                    else
                        DisableForNativeCamera(menuOpen, reason == "vanilla camera");
                }
                NotificationService.Instance.Status("Camera unavailable: " + reason);
                return;
            }

            Player player = ManagerCacheService.Instance.LocalPlayer;
            _enabled = true;
            _anglesReady = false;
            ViewModelVisibilityService.Instance.EnterThirdPerson(player);
            Apply(menuOpen);
            NotificationService.Instance.Status("Camera: 3rd Person");
        }

        public void Maintain(bool menuOpen)
        {
            _menuOpen = menuOpen;
            PlayerCamera playerCamera = PlayerCamera.Instance;
            Player player = ManagerCacheService.Instance.LocalPlayer;

            if (ManagementClipboardService.Instance.IsActive() ||
                IsBuildPlacementActive())
            {
                if (_enabled || _overrideActive)
                    Disable(false);
                return;
            }

            if (!_enabled)
            {
                if (IsSkateboardActive)
                {
                    ViewModelVisibilityService.Instance.EnterNativeSkateboard(player);
                    return;
                }

                if (ViewModelVisibilityService.Instance.IsCustomMode)
                {
                    if (IsNativeAvatarView(playerCamera))
                        ViewModelVisibilityService.Instance.EnterNativeAvatarView(player);
                    else if (!IsLocalPlayerInVehicle(player) && !IsVehicleCamera(playerCamera))
                        ViewModelVisibilityService.Instance.RestoreFirstPerson(player);
                }
                else if (!IsLocalPlayerInVehicle(player) &&
                    !IsLocalPlayerSkating(player) &&
                    !IsNativeExternalCamera(playerCamera))
                {
                    ViewModelVisibilityService.Instance.MaintainFirstPersonRepair();
                }

                return;
            }

            if (player == null)
            {
                Disable(menuOpen);
                return;
            }

            if (IsLocalPlayerInVehicle(player) || IsVehicleCamera(playerCamera))
            {
                DisableForNativeCamera(menuOpen, false);
                return;
            }

            if (IsSkateboardActive)
            {
                DisableForSkateboard(menuOpen);
                return;
            }

            if (IsNativeAvatarView(playerCamera))
            {
                DisableForNativeCamera(menuOpen, true);
                return;
            }

            if (IsNativeExternalCamera(playerCamera))
            {
                DisableForNativeCamera(menuOpen, false);
                return;
            }

            ViewModelVisibilityService.Instance.EnterThirdPerson(player);
        }

        public void Apply(bool menuOpen)
        {
            _menuOpen = menuOpen;
            if (!_enabled)
                return;

            if (ManagementClipboardService.Instance.IsActive() ||
                IsBuildPlacementActive())
            {
                Disable(false);
                return;
            }

            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                Player player = ManagerCacheService.Instance.LocalPlayer;
                if (playerCamera == null || player == null)
                    return;

                if (IsLocalPlayerInVehicle(player) || IsVehicleCamera(playerCamera))
                {
                    DisableForNativeCamera(menuOpen, false);
                    return;
                }

                if (IsSkateboardActive)
                {
                    DisableForSkateboard(menuOpen);
                    return;
                }

                if (IsNativeAvatarView(playerCamera))
                {
                    DisableForNativeCamera(menuOpen, true);
                    return;
                }

                if (IsNativeExternalCamera(playerCamera))
                {
                    DisableForNativeCamera(menuOpen, false);
                    return;
                }

                Camera camera = playerCamera.Camera != null
                    ? playerCamera.Camera
                    : Camera.main;
                if (camera == null)
                    return;

                CameraStateRestoreService.Instance.Capture(playerCamera);

                Transform cameraTransform = camera.transform;
                if (!_anglesReady)
                {
                    Vector3 angles = cameraTransform.rotation.eulerAngles;
                    _yaw = player.transform.rotation.eulerAngles.y;
                    _pitch = NormalizePitch(angles.x);
                    _anglesReady = true;
                }

                if (!_menuOpen && !InputLockService.Instance.IsLocked && _lastInputFrame != Time.frameCount)
                {
                    _yaw += Input.GetAxisRaw("Mouse X") * 1.6f;
                    _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * 1.3f, -35f, 55f);
                    _lastInputFrame = Time.frameCount;
                }

                Quaternion cameraRotation = Quaternion.Euler(_pitch, _yaw, 0f);
                Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
                try
                {
                    PlayerMovement movement = PlayerMovement.Instance;
                    if (movement != null)
                        movement.SetPlayerRotation(yawRotation);
                    else
                        player.transform.rotation = yawRotation;
                }
                catch { }

                Vector3 pivot = player.transform.position + Vector3.up * _height;
                Vector3 desired = pivot -
                    (cameraRotation * Vector3.forward * _distance) +
                    (yawRotation * Vector3.right * _shoulderOffset);
                desired = ResolveClippedPosition(player, pivot, desired);

                playerCamera.OverrideTransform(desired, cameraRotation, 0f, false);
                playerCamera.SetCanLook(false);
                _overrideActive = true;

                try
                {
                    player.CameraPosition = pivot;
                    player.CameraRotation = cameraRotation;
                    if (player.MimicCamera != null)
                    {
                        player.MimicCamera.position = pivot;
                        player.MimicCamera.rotation = cameraRotation;
                    }
                }
                catch { }

                ViewModelVisibilityService.Instance.EnterThirdPerson(player);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Third-person camera update failed", ex);
            }
        }

        public void ApplyLate()
        {
            Apply(_menuOpen);
        }

        public void Disable(bool menuOpen)
        {
            _enabled = false;
            _overrideActive = false;
            _anglesReady = false;
            CameraStateRestoreService.Instance.Restore(menuOpen);
            ViewModelVisibilityService.Instance.RestoreFirstPerson(ManagerCacheService.Instance.LocalPlayer);
        }

        public void ForceDisableForVehicle(bool menuOpen)
        {
            if (!_enabled && !_overrideActive)
                return;

            DisableForNativeCamera(menuOpen, false);
            NotificationService.Instance.Status("3rd person disabled in vehicle");
        }

        public void ForceDisableForSkateboard(bool menuOpen)
        {
            if (!_enabled && !_overrideActive)
                return;

            DisableForSkateboard(menuOpen);
            NotificationService.Instance.Status("3rd person disabled on skateboard");
        }

        public void NotifySkateboardMounted()
        {
            ViewModelVisibilityService.Instance.EnterNativeSkateboard(
                ManagerCacheService.Instance.LocalPlayer);
        }

        public void NotifySkateboardDismounted()
        {
            ViewModelVisibilityService.Instance.RestoreFirstPerson(
                ManagerCacheService.Instance.LocalPlayer);
        }

        public void ForceDisableForBuildPlacement()
        {
            // Never alter a first-person build start. If third person was active, restore
            // its captured state before Schedule I takes ownership of the build ray.
            if (_enabled || _overrideActive)
                Disable(false);
        }

        private void DisableForNativeCamera(bool menuOpen, bool keepPawnVisible)
        {
            _enabled = false;
            _overrideActive = false;
            _anglesReady = false;
            CameraStateRestoreService.Instance.ReleaseToNative(!menuOpen);
            Player player = ManagerCacheService.Instance.LocalPlayer;
            if (keepPawnVisible)
                ViewModelVisibilityService.Instance.EnterNativeAvatarView(player);
            else
                ViewModelVisibilityService.Instance.ReleaseToVanilla(player);
        }

        private void DisableForSkateboard(bool menuOpen)
        {
            _enabled = false;
            _overrideActive = false;
            _anglesReady = false;
            CameraStateRestoreService.Instance.ReleaseToNative(!menuOpen);
            ViewModelVisibilityService.Instance.ReleaseToVanilla(
                ManagerCacheService.Instance.LocalPlayer);
        }

        public bool TryInteractionRaycast(
            float range,
            LayerMask layerMask,
            bool includeTriggers,
            float radius,
            out RaycastHit hit)
        {
            hit = default;
            if (!ShouldUseCustomInteractionRaycasts)
                return false;

            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                Player player = ManagerCacheService.Instance.LocalPlayer;
                Camera camera = playerCamera?.Camera != null ? playerCamera.Camera : Camera.main;
                if (camera == null)
                    return true;

                Vector3 origin = player != null
                    ? player.transform.position + Vector3.up * _height
                    : camera.transform.position;
                Vector3 direction = camera.transform.forward;
                QueryTriggerInteraction query = includeTriggers
                    ? QueryTriggerInteraction.Collide
                    : QueryTriggerInteraction.Ignore;
                RaycastHit[] hits = radius > 0f
                    ? Physics.SphereCastAll(origin, radius, direction, range, layerMask, query)
                    : Physics.RaycastAll(origin, direction, range, layerMask, query);

                float nearest = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit candidate = hits[i];
                    if (candidate.collider == null ||
                        IsLocalPlayerCollider(candidate.collider, player) ||
                        IsLocalViewmodelOrEquippedCollider(candidate.collider))
                    {
                        continue;
                    }

                    if (candidate.distance >= nearest)
                        continue;

                    nearest = candidate.distance;
                    hit = candidate;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Third-person interaction ray failed: " + ex.Message);
            }

            return true;
        }

        private Vector3 ResolveClippedPosition(Player player, Vector3 pivot, Vector3 desired)
        {
            Vector3 offset = desired - pivot;
            float length = offset.magnitude;
            if (length <= 0.01f)
                return desired;

            try
            {
                RaycastHit[] hits = Physics.SphereCastAll(
                    pivot,
                    0.18f,
                    offset.normalized,
                    length,
                    -5,
                    QueryTriggerInteraction.Ignore);
                float nearest = length;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit candidate = hits[i];
                    if (candidate.collider == null || candidate.distance <= 0.05f)
                        continue;
                    if (IsLocalPlayerCollider(candidate.collider, player) ||
                        IsLocalViewmodelOrEquippedCollider(candidate.collider))
                    {
                        continue;
                    }

                    if (candidate.distance < nearest)
                        nearest = candidate.distance;
                }

                if (nearest < length)
                    return pivot + offset.normalized * Mathf.Max(0.45f, nearest - 0.15f);
            }
            catch { }

            return desired;
        }

        private static bool IsNativeAvatarView(PlayerCamera playerCamera)
        {
            try { return playerCamera != null && playerCamera.ViewingAvatar; }
            catch { return false; }
        }

        private static bool IsNativeExternalCamera(PlayerCamera playerCamera)
        {
            try
            {
                return playerCamera != null &&
                    playerCamera.CameraMode != PlayerCamera.ECameraMode.Default;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsVehicleCamera(PlayerCamera playerCamera)
        {
            try
            {
                return playerCamera != null &&
                    playerCamera.CameraMode == PlayerCamera.ECameraMode.Vehicle;
            }
            catch { return false; }
        }

        private static bool IsSkateboardCamera(PlayerCamera playerCamera)
        {
            try
            {
                return playerCamera != null &&
                    playerCamera.CameraMode == PlayerCamera.ECameraMode.Skateboard;
            }
            catch { return false; }
        }

        private static bool IsBuildPlacementActive()
        {
            try
            {
                if (BuildManager.Instance != null && BuildManager.Instance.isBuilding)
                    return true;
            }
            catch { }

            try
            {
                Equippable equipped = PlayerInventory.Instance?.equippable;
                return equipped is Equippable_BuildableItem ||
                    equipped is Equippable_SurfaceItem;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLocalPlayerInVehicle(Player player)
        {
            try
            {
                if (player != null &&
                    (player.IsInVehicle || player.CurrentVehicle != null || player.CurrentVehicleSeat != null))
                {
                    return true;
                }
            }
            catch { }

            try
            {
                return PlayerMovement.Instance != null &&
                    PlayerMovement.Instance.CurrentVehicle != null;
            }
            catch { return false; }
        }

        private static bool IsLocalPlayerSkating(Player player)
        {
            try
            {
                return player != null &&
                    (player.IsSkating || player.ActiveSkateboard != null);
            }
            catch
            {
                return false;
            }
        }

        private bool IsNativeToolRaycastActive()
        {
            if (!_enabled || !_overrideActive)
                return false;

            int frame = Time.frameCount;
            if (_nativeToolRaycastFrame == frame)
                return _nativeToolRaycastActive;

            _nativeToolRaycastFrame = frame;
            _nativeToolRaycastActive = IsCurrentEquippableToolRaycastNative();

            return _nativeToolRaycastActive;
        }

        private static bool IsCurrentEquippableToolRaycastNative()
        {
            try
            {
                Equippable tool = PlayerInventory.Instance?.equippable;
                return tool is Equippable_Trimmers || tool is Equippable_Pourable;
            }
            catch { }

            return false;
        }

        private static bool IsLocalPlayerCollider(Collider collider, Player player)
        {
            try
            {
                return collider != null && player != null &&
                    collider.transform != null && player.transform != null &&
                    collider.transform.root == player.transform.root;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLocalViewmodelOrEquippedCollider(Collider collider)
        {
            if (collider == null)
                return false;

            try
            {
                ViewmodelAvatar viewmodelAvatar = Singleton<ViewmodelAvatar>.Instance;
                if (viewmodelAvatar != null)
                {
                    if (IsChildOf(collider.transform, viewmodelAvatar.transform))
                        return true;
                    if (viewmodelAvatar.RightHandContainer != null &&
                        IsChildOf(collider.transform, viewmodelAvatar.RightHandContainer))
                    {
                        return true;
                    }
                    if (viewmodelAvatar.Avatar != null &&
                        IsChildOf(collider.transform, viewmodelAvatar.Avatar.transform))
                    {
                        return true;
                    }
                }
            }
            catch { }

            try
            {
                return collider.GetComponentInParent<Equippable_Viewmodel>() != null ||
                    collider.GetComponentInParent<Equippable_AvatarViewmodel>() != null ||
                    collider.GetComponentInParent<Equippable_MeleeWeapon>() != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsChildOf(Transform child, Transform parent)
        {
            if (child == null || parent == null)
                return false;

            try
            {
                Transform current = child;
                while (current != null)
                {
                    if (current == parent)
                        return true;
                    current = current.parent;
                }
            }
            catch { }

            return false;
        }

        private static float NormalizePitch(float pitch)
        {
            if (pitch > 180f)
                pitch -= 360f;
            return Mathf.Clamp(pitch, -35f, 55f);
        }
    }
}
