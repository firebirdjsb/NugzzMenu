using System;
using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Equipping;
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
        private bool _skateboardVisibilityForced;
        private bool _vehicleVisibilityForced;
        private bool _anglesReady;
        private bool _overrideActive;
        private bool _menuOpen;
        private int _lastInputFrame = -1;
        private float _yaw;
        private float _pitch;
        private float _distance = 2.65f;
        private float _height = 1.45f;
        private float _shoulderOffset;
        private int _nativeToolRaycastFrame = -1;
        private bool _nativeToolRaycastActive;

        private CameraService() { }

        public bool ThirdPersonEnabled => _enabled;
        public float Distance => _distance;
        public float Height => _height;
        public float ShoulderOffset => _shoulderOffset;
        public bool ShouldUseCustomCombatHit => _enabled && _overrideActive;
        public bool ShouldUseVanillaManagementRaycasts => ManagementClipboardService.Instance.IsActive();
        public bool ShouldUseVanillaToolRaycasts => IsNativeToolRaycastActive();

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
            if (ManagementClipboardService.Instance.IsActive())
            {
                ReleaseCameraForManagementClipboard();
                return;
            }

            if (!_enabled)
            {
                MaintainFirstPersonSkateboardVisibility();
                _vehicleVisibilityForced = false;
                return;
            }

            if (MaintainThirdPersonVehicleVisibility())
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
                if (ManagementClipboardService.Instance.IsActive())
                {
                    ReleaseCameraForManagementClipboard();
                    return;
                }

                PlayerCamera playerCamera = PlayerCamera.Instance;
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (playerCamera == null || player == null)
                    return;

                if (IsNativeExternalCamera(playerCamera) || player.IsInVehicle)
                {
                    if (_overrideActive)
                        StopCustomOverride(_menuOpen);
                    MaintainThirdPersonVehicleVisibility();
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

                if (!_menuOpen && !InputLockService.Instance.IsLocked && _lastInputFrame != Time.frameCount)
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
                        if (IsLocalPlayerCollider(candidate.collider, player) ||
                            IsLocalViewmodelOrEquippedCollider(candidate.collider))
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
            if (ManagementClipboardService.Instance.IsActive() || IsNativeToolRaycastActive())
                return false;

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
                int layerMask = -5;
                try
                {
                    if (CombatManager.Instance != null)
                        layerMask = CombatManager.Instance.MeleeLayerMask;
                }
                catch { }

                float range = thirdPerson ? 1.65f : PunchController.PUNCH_RANGE;
                RaycastHit[] hits = Physics.SphereCastAll(
                    origin,
                    0.32f,
                    direction,
                    range,
                    layerMask,
                    QueryTriggerInteraction.Ignore);

                NPC nearestNpc = null;
                PhysicsDamageable nearestPhysicsDamageable = null;
                Player nearestPlayer = null;
                Vector3 hitPoint = origin + direction * range;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit candidate = hits[i];
                    if (candidate.collider == null || IsLocalPlayerCollider(candidate.collider, player))
                        continue;

                    NPC npc = candidate.collider.GetComponentInParent<NPC>();
                    Player hitPlayer = candidate.collider.GetComponentInParent<Player>();
                    PhysicsDamageable physicsDamageable =
                        npc == null && hitPlayer == null
                            ? candidate.collider.GetComponentInParent<PhysicsDamageable>()
                            : null;
                    if (npc == null && physicsDamageable == null && hitPlayer == null)
                        continue;
                    if (candidate.distance >= nearestDistance)
                        continue;

                    nearestNpc = npc;
                    nearestPhysicsDamageable = physicsDamageable;
                    nearestPlayer = hitPlayer;
                    hitPoint = candidate.point;
                    nearestDistance = candidate.distance;
                }

                if (nearestPlayer == null && nearestNpc == null && nearestPhysicsDamageable == null)
                    TryGetAimedPlayer(player, origin, direction, range, 0.32f, out nearestPlayer, out hitPoint);

                if (nearestNpc == null && nearestPhysicsDamageable == null && nearestPlayer == null)
                    return true;

                float normalizedPower = Mathf.Clamp01(power);
                float damage = Mathf.Lerp(punchController.MinPunchDamage, punchController.MaxPunchDamage, normalizedPower);
                float force = Mathf.Lerp(punchController.MinPunchForce, punchController.MaxPunchForce, normalizedPower);
                var impact = new Impact(
                    hitPoint,
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

        public bool ExecuteMeleeSafely(
            Equippable_MeleeWeapon weapon, float power)
        {
            if (weapon == null)
                return false;

            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                PlayerCamera playerCamera = PlayerCamera.Instance;
                Camera camera = playerCamera?.Camera != null
                    ? playerCamera.Camera
                    : Camera.main;
                if (player == null || camera == null)
                    return false;

                bool thirdPerson = _enabled && _overrideActive;
                Vector3 origin = thirdPerson
                    ? player.transform.position +
                        Vector3.up * Mathf.Clamp(_height * 0.82f, 1.05f, 1.45f)
                    : camera.transform.position;
                Vector3 direction = camera.transform.forward.normalized;
                float range = Mathf.Max(0.5f, weapon.Range);
                float radius = Mathf.Max(0.05f, weapon.HitRadius);
                int layerMask = -5;
                try
                {
                    if (CombatManager.Instance != null)
                        layerMask = CombatManager.Instance.MeleeLayerMask;
                }
                catch { }

                RaycastHit[] hits = Physics.SphereCastAll(
                    origin,
                    radius,
                    direction,
                    thirdPerson ? range + 0.4f : range,
                    layerMask,
                    QueryTriggerInteraction.Ignore);

                NPC nearestNpc = null;
                PhysicsDamageable nearestPhysicsDamageable = null;
                Player nearestPlayer = null;
                Vector3 hitPoint = origin + direction * (thirdPerson ? range + 0.4f : range);
                float nearestDistance = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit candidate = hits[i];
                    if (candidate.collider == null ||
                        IsLocalPlayerCollider(candidate.collider, player))
                    {
                        continue;
                    }

                    NPC npc = candidate.collider.GetComponentInParent<NPC>();
                    Player hitPlayer = candidate.collider.GetComponentInParent<Player>();
                    PhysicsDamageable physicsDamageable =
                        npc == null && hitPlayer == null
                            ? candidate.collider.GetComponentInParent<PhysicsDamageable>()
                            : null;
                    if (npc == null && physicsDamageable == null && hitPlayer == null)
                        continue;
                    if (candidate.distance >= nearestDistance)
                        continue;

                    nearestNpc = npc;
                    nearestPhysicsDamageable = physicsDamageable;
                    nearestPlayer = hitPlayer;
                    hitPoint = candidate.point;
                    nearestDistance = candidate.distance;
                }

                if (nearestPlayer == null &&
                    nearestNpc == null &&
                    nearestPhysicsDamageable == null)
                {
                    TryGetAimedPlayer(
                        player,
                        origin,
                        direction,
                        thirdPerson ? range + 0.4f : range,
                        radius,
                        out nearestPlayer,
                        out hitPoint);
                }

                if (nearestNpc == null &&
                    nearestPhysicsDamageable == null &&
                    nearestPlayer == null)
                {
                    return true;
                }

                float normalizedPower = Mathf.Clamp01(power);
                float damage = Mathf.Lerp(
                    weapon.MinDamage, weapon.MaxDamage, normalizedPower);
                float force = Mathf.Lerp(
                    weapon.MinForce, weapon.MaxForce, normalizedPower);
                var impact = new Impact(
                    hitPoint,
                    direction,
                    force,
                    damage,
                    weapon.ImpactType,
                    player.NetworkObject);

                if (nearestNpc != null)
                    nearestNpc.SendImpact(impact);
                else if (nearestPhysicsDamageable != null)
                    nearestPhysicsDamageable.SendImpact(impact);
                else
                    nearestPlayer.SendImpact(impact);

                try { weapon.ImpactSound?.PlayOneShot(); } catch { }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException(
                    "Safe melee hit test failed", ex);
            }

            return true;
        }

        private static bool TryGetAimedPlayer(
            Player localPlayer,
            Vector3 origin,
            Vector3 direction,
            float range,
            float radius,
            out Player targetPlayer,
            out Vector3 hitPoint)
        {
            targetPlayer = null;
            hitPoint = origin + direction * range;

            try
            {
                var players = Player.PlayerList;
                if (players == null)
                    return false;

                float bestDistance = float.MaxValue;
                float allowedMissDistance = Mathf.Max(0.75f, radius + 0.55f);
                for (int i = 0; i < players.Count; i++)
                {
                    Player candidate = players[i];
                    if (candidate == null || candidate == localPlayer)
                        continue;

                    Vector3 targetCenter = candidate.transform.position + Vector3.up * 1.05f;
                    Vector3 toTarget = targetCenter - origin;
                    float alongRay = Vector3.Dot(toTarget, direction);
                    if (alongRay < 0.05f || alongRay > range + 0.65f)
                        continue;

                    Vector3 closestPoint = origin + direction * alongRay;
                    float missDistance = Vector3.Distance(targetCenter, closestPoint);
                    if (missDistance > allowedMissDistance || alongRay >= bestDistance)
                        continue;

                    targetPlayer = candidate;
                    hitPoint = closestPoint;
                    bestDistance = alongRay;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("PvP aim fallback failed: " + ex.Message);
            }

            return targetPlayer != null;
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

        private void ReleaseCameraForManagementClipboard()
        {
            if (_enabled)
            {
                ToggleThirdPerson(false, false);
                return;
            }

            try
            {
                if (_overrideActive)
                    StopCustomOverride(false);

                PlayerCamera playerCamera = PlayerCamera.Instance;
                playerCamera?.SetCanLook(true);
            }
            catch { }
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
                if (visible)
                    SetViewmodelRenderersVisible(false);
                else
                    RestoreViewmodelVisuals();
            }
            catch { }

            try
            {
                var viewmodelAvatar = Singleton<ViewmodelAvatar>.Instance;
                if (viewmodelAvatar != null)
                {
                    if (visible)
                        SetRenderersVisible(viewmodelAvatar.gameObject, false);
                }
            }
            catch { }
        }

        private void RestoreFirstPersonVisuals()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player != null)
                {
                    player.SetThirdPersonMeshesVisibility(false);
                    player.SetVisibleToLocalPlayer(true);
                    if (player.Avatar != null)
                        player.Avatar.SetVisible(true);
                    _thirdPersonBodyVisible = false;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("First-person mesh restore failed: " + ex.Message);
            }

            RestoreViewmodelVisuals();
        }

        private void HideLocalPawnForVehicle()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player != null)
                {
                    player.SetThirdPersonMeshesVisibility(false);
                    player.SetVisibleToLocalPlayer(false);
                    if (player.Avatar != null)
                        player.Avatar.SetVisible(false);
                    _thirdPersonBodyVisible = false;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Vehicle pawn hide failed: " + ex.Message);
            }

            RestoreViewmodelVisuals();
        }

        private static void RestoreViewmodelVisuals()
        {
            try
            {
                var inventory = PlayerInventory.Instance;
                if (inventory != null)
                    inventory.SetViewmodelVisible(true);
            }
            catch { }

            try
            {
                var viewmodelAvatar = Singleton<ViewmodelAvatar>.Instance;
                if (viewmodelAvatar != null)
                {
                    viewmodelAvatar.SetVisibility(true);
                    SetRenderersVisible(viewmodelAvatar.gameObject, true);
                    if (viewmodelAvatar.RightHandContainer != null)
                        SetRenderersVisible(viewmodelAvatar.RightHandContainer.gameObject, true);
                    if (viewmodelAvatar.Avatar != null)
                        SetRenderersVisible(viewmodelAvatar.Avatar.gameObject, true);
                }
            }
            catch { }
        }

        private static void SetViewmodelRenderersVisible(bool visible)
        {
            try
            {
                var viewmodelAvatar = Singleton<ViewmodelAvatar>.Instance;
                if (viewmodelAvatar != null)
                {
                    SetRenderersVisible(viewmodelAvatar.gameObject, visible);
                    if (viewmodelAvatar.RightHandContainer != null)
                        SetRenderersVisible(viewmodelAvatar.RightHandContainer.gameObject, visible);
                    if (viewmodelAvatar.Avatar != null)
                        SetRenderersVisible(viewmodelAvatar.Avatar.gameObject, visible);
                }
            }
            catch { }

        }

        private static void SetRenderersVisible(GameObject root, bool visible)
        {
            if (root == null)
                return;

            try
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers == null)
                    return;

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null)
                        renderer.enabled = visible;
                }
            }
            catch { }
        }

        private bool MaintainThirdPersonVehicleVisibility()
        {
            if (!_enabled)
                return false;

            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null)
                    return false;

                bool shouldHide = player.IsInVehicle;
                if (shouldHide)
                {
                    if (!_vehicleVisibilityForced || _thirdPersonBodyVisible != false)
                    {
                        HideLocalPawnForVehicle();
                        _vehicleVisibilityForced = true;
                    }

                    return true;
                }

                if (_vehicleVisibilityForced)
                {
                    ForceThirdPersonVisuals(true);
                    _vehicleVisibilityForced = false;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Vehicle visibility repair failed: " + ex.Message);
            }

            return false;
        }

        private void MaintainFirstPersonSkateboardVisibility()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null)
                    return;

                if (!IsLocalPlayerSkating(player))
                {
                    if (_skateboardVisibilityForced)
                    {
                        _skateboardVisibilityForced = false;
                        ForceThirdPersonVisuals(false);
                    }

                    return;
                }

                player.SetThirdPersonMeshesVisibility(true);
                player.SetVisibleToLocalPlayer(true);
                if (player.Avatar != null)
                    player.Avatar.SetVisible(true);
                _skateboardVisibilityForced = true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Skateboard visibility repair failed: " + ex.Message);
            }
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
                var viewmodelAvatar = Singleton<ViewmodelAvatar>.Instance;
                if (viewmodelAvatar != null)
                {
                    if (IsChildOf(collider.transform, viewmodelAvatar.transform))
                        return true;
                    if (viewmodelAvatar.RightHandContainer != null &&
                        IsChildOf(collider.transform, viewmodelAvatar.RightHandContainer))
                        return true;
                    if (viewmodelAvatar.Avatar != null &&
                        IsChildOf(collider.transform, viewmodelAvatar.Avatar.transform))
                        return true;
                }
            }
            catch { }

            try
            {
                if (collider.GetComponentInParent<Equippable_Viewmodel>() != null)
                    return true;
                if (collider.GetComponentInParent<Equippable_AvatarViewmodel>() != null)
                    return true;
                if (collider.GetComponentInParent<Equippable_MeleeWeapon>() != null)
                    return true;
            }
            catch { }

            return false;
        }

        private bool IsNativeToolRaycastActive()
        {
            if (!_enabled || !_overrideActive)
                return false;

            int frame = Time.frameCount;
            if (_nativeToolRaycastFrame == frame)
                return _nativeToolRaycastActive;

            _nativeToolRaycastFrame = frame;
            _nativeToolRaycastActive =
                HasActiveEquippable<Equippable_Trimmers>() ||
                HasActiveEquippable<Equippable_Pourable>();

            return _nativeToolRaycastActive;
        }

        private static bool HasActiveEquippable<T>()
            where T : Equippable
        {
            try
            {
                T[] tools = UnityEngine.Object.FindObjectsOfType<T>();
                if (tools == null)
                    return false;

                for (int i = 0; i < tools.Length; i++)
                {
                    T tool = tools[i];
                    if (tool == null || !tool.enabled)
                        continue;

                    GameObject gameObject = tool.gameObject;
                    if (gameObject != null && gameObject.activeInHierarchy)
                        return true;
                }
            }
            catch { }

            return false;
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
