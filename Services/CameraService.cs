using System;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Provides a local third-person camera while leaving vehicle cameras native.
    /// </summary>
    public sealed class CameraService
    {
        private static readonly CameraService _instance = new CameraService();
        public static CameraService Instance => _instance;

        private bool _enabled;
        private bool? _thirdPersonBodyVisible;
        private bool _anglesReady;
        private bool _overrideActive;
        private bool _menuOpen;
        private int _lastInputFrame = -1;
        private float _yaw;
        private float _pitch;
        private float _distance = 2.65f;
        private float _height = 1.45f;
        private float _shoulderOffset;

        private CameraService() { }

        public bool ThirdPersonEnabled => _enabled;
        public float Distance => _distance;
        public float Height => _height;
        public float ShoulderOffset => _shoulderOffset;

        public void SetDistance(float value) => _distance = Mathf.Clamp(value, 1.5f, 6f);
        public void SetHeight(float value) => _height = Mathf.Clamp(value, 0.8f, 2.4f);
        public void SetShoulderOffset(float value) => _shoulderOffset = Mathf.Clamp(value, -1f, 1f);

        public void ToggleThirdPerson(bool enabled, bool menuOpen = false)
        {
            _menuOpen = menuOpen;
            _enabled = enabled;
            _thirdPersonBodyVisible = null;
            _anglesReady = false;

            if (_enabled)
            {
                ForceThirdPersonVisuals(true);
                ApplyThirdPersonCamera(menuOpen);
                NotificationService.Instance.Status("Camera: 3rd Person");
                return;
            }

            StopCustomOverride(menuOpen);
            ForceThirdPersonVisuals(false);
            NotificationService.Instance.Status("Camera: 1st Person");
        }

        public void MaintainThirdPersonState(bool menuOpen = false)
        {
            _menuOpen = menuOpen;
            if (!_enabled)
                return;

            ForceThirdPersonVisuals(true);
        }

        public void ApplyThirdPersonCamera(bool menuOpen = false)
        {
            _menuOpen = menuOpen;
            ApplyThirdPersonCameraInternal();
        }

        public void ApplyThirdPersonCameraLate()
        {
            ApplyThirdPersonCameraInternal();
        }

        private void ApplyThirdPersonCameraInternal()
        {
            if (!_enabled)
                return;

            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (playerCamera == null || player == null)
                    return;

                if (IsNativeExternalCamera(playerCamera) || player.IsInVehicle)
                {
                    if (_overrideActive)
                        StopCustomOverride(_menuOpen);
                    return;
                }

                Camera camera = playerCamera.Camera != null ? playerCamera.Camera : Camera.main;
                if (camera == null)
                    return;

                Transform cameraTransform = camera.transform;
                if (!_anglesReady)
                {
                    Vector3 angles = cameraTransform.rotation.eulerAngles;
                    _yaw = player.transform.rotation.eulerAngles.y;
                    _pitch = NormalizePitch(angles.x);
                    _anglesReady = true;
                }

                if (!_menuOpen && _lastInputFrame != Time.frameCount)
                {
                    _yaw += Input.GetAxisRaw("Mouse X") * 1.6f;
                    _pitch = Mathf.Clamp(_pitch - Input.GetAxisRaw("Mouse Y") * 1.3f, -35f, 55f);
                    _lastInputFrame = Time.frameCount;
                }

                Quaternion cameraRotation = Quaternion.Euler(_pitch, _yaw, 0f);
                Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
                PlayerMovement movement = PlayerMovement.Instance;
                if (movement != null)
                    movement.SetPlayerRotation(yawRotation);
                else
                    player.transform.rotation = yawRotation;

                Vector3 pivot = player.transform.position + Vector3.up * _height;
                Vector3 desired = pivot -
                    (cameraRotation * Vector3.forward * _distance) +
                    (yawRotation * Vector3.right * _shoulderOffset);

                Vector3 offset = desired - pivot;
                float length = offset.magnitude;
                if (length > 0.01f)
                {
                    RaycastHit[] hits = Physics.SphereCastAll(
                        pivot, 0.18f, offset.normalized, length, -5, QueryTriggerInteraction.Ignore);
                    float nearest = length;
                    for (int i = 0; i < hits.Length; i++)
                    {
                        RaycastHit candidate = hits[i];
                        if (candidate.collider == null || candidate.distance < 0.6f)
                            continue;
                        if (IsLocalPlayerCollider(candidate.collider, player))
                            continue;
                        if (candidate.distance < nearest)
                            nearest = candidate.distance;
                    }

                    if (nearest < length)
                        desired = pivot + offset.normalized * Mathf.Max(0.75f, nearest - 0.15f);
                }

                if (!playerCamera.transformOverriden)
                    playerCamera.OverrideTransform(desired, cameraRotation, 0f, false);

                _overrideActive = true;
                playerCamera.SetCanLook(false);
                if (playerCamera.CameraContainer != null)
                {
                    playerCamera.CameraContainer.position = desired;
                    playerCamera.CameraContainer.rotation = cameraRotation;
                }
                cameraTransform.position = desired;
                cameraTransform.rotation = cameraRotation;
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
                ForceThirdPersonVisuals(true);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Third-person camera update failed", ex);
            }
        }

        public bool TryThirdPersonInteractionRaycast(float range, LayerMask layerMask, bool includeTriggers, float radius, out RaycastHit hit)
        {
            hit = default;
            if (!_enabled || !_overrideActive)
                return false;

            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                var player = ManagerCacheService.Instance.LocalPlayer;
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
                    if (candidate.collider == null || IsLocalPlayerCollider(candidate.collider, player))
                        continue;
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

        public bool ExecutePunchSafely(PunchController punchController, float power)
        {
            if (punchController == null)
                return false;

            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                PlayerCamera playerCamera = PlayerCamera.Instance;
                Camera camera = playerCamera?.Camera != null ? playerCamera.Camera : Camera.main;
                if (player == null || camera == null)
                    return false;

                bool thirdPerson = _enabled && _overrideActive;
                Vector3 origin = thirdPerson
                    ? player.transform.position + Vector3.up * Mathf.Clamp(_height * 0.82f, 1.05f, 1.45f)
                    : camera.transform.position;
                Vector3 direction = camera.transform.forward.normalized;
                RaycastHit[] hits = Physics.SphereCastAll(
                    origin,
                    0.32f,
                    direction,
                    thirdPerson ? 1.65f : PunchController.PUNCH_RANGE,
                    -5,
                    QueryTriggerInteraction.Ignore);

                NPC nearestNpc = null;
                PhysicsDamageable nearestPhysicsDamageable = null;
                Player nearestPlayer = null;
                RaycastHit nearestHit = default;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit candidate = hits[i];
                    if (candidate.collider == null || IsLocalPlayerCollider(candidate.collider, player))
                        continue;

                    NPC npc = candidate.collider.GetComponentInParent<NPC>();
                    PhysicsDamageable physicsDamageable =
                        npc == null ? candidate.collider.GetComponentInParent<PhysicsDamageable>() : null;
                    Player hitPlayer =
                        npc == null && physicsDamageable == null
                            ? candidate.collider.GetComponentInParent<Player>()
                            : null;
                    if (npc == null && physicsDamageable == null && hitPlayer == null)
                        continue;
                    if (candidate.distance >= nearestDistance)
                        continue;

                    nearestNpc = npc;
                    nearestPhysicsDamageable = physicsDamageable;
                    nearestPlayer = hitPlayer;
                    nearestHit = candidate;
                    nearestDistance = candidate.distance;
                }

                if (nearestNpc == null && nearestPhysicsDamageable == null && nearestPlayer == null)
                    return true;

                float normalizedPower = Mathf.Clamp01(power);
                float damage = Mathf.Lerp(punchController.MinPunchDamage, punchController.MaxPunchDamage, normalizedPower);
                float force = Mathf.Lerp(punchController.MinPunchForce, punchController.MaxPunchForce, normalizedPower);
                var impact = new Impact(
                    nearestHit.point,
                    direction,
                    force,
                    damage,
                    EImpactType.Punch,
                    player.NetworkObject);
                if (nearestNpc != null)
                    nearestNpc.SendImpact(impact);
                else if (nearestPhysicsDamageable != null)
                    nearestPhysicsDamageable.SendImpact(impact);
                else
                    nearestPlayer.SendImpact(impact);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Safe punch hit test failed", ex);
            }

            return true;
        }

        private static bool IsNativeExternalCamera(PlayerCamera playerCamera)
        {
            try
            {
                return playerCamera != null && playerCamera.CameraMode != PlayerCamera.ECameraMode.Default;
            }
            catch
            {
                return false;
            }
        }

        private void StopCustomOverride(bool menuOpen)
        {
            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                if (playerCamera != null && !IsNativeExternalCamera(playerCamera))
                    playerCamera.StopTransformOverride(0f, !menuOpen, false);
            }
            catch { }

            _overrideActive = false;
            _anglesReady = false;
        }

        private void ForceThirdPersonVisuals(bool visible)
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player != null)
                {
                    player.SetThirdPersonMeshesVisibility(visible);
                    player.SetVisibleToLocalPlayer(visible);
                    if (player.Avatar != null)
                        player.Avatar.SetVisible(visible);
                    _thirdPersonBodyVisible = visible;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Third-person mesh visibility failed: " + ex.Message);
            }

            try
            {
                var inventory = PlayerInventory.Instance;
                if (inventory != null)
                    inventory.SetViewmodelVisible(!visible);
            }
            catch { }

            try
            {
                if (Singleton<ViewmodelAvatar>.Instance != null)
                    Singleton<ViewmodelAvatar>.Instance.SetVisibility(!visible);
            }
            catch { }
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

        private static float NormalizePitch(float pitch)
        {
            if (pitch > 180f)
                pitch -= 360f;
            return Mathf.Clamp(pitch, -35f, 55f);
        }
    }
}
