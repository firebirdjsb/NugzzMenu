using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    // A session-only motorcycle model on the game's registered vehicle chassis.
    // Native input, driver ownership and RPCs remain responsible for riding.
    // Bike-sized contacts and controlled display lean replace the sedan layout.
    internal sealed class MotorcycleService
    {
        internal static readonly MotorcycleService Instance = new();
        internal const int Limit = 4;
        internal sealed class Bike
        {
            internal LandVehicle Vehicle;
            internal GameObject Visual;
            internal Transform Seat, RearWheel, LeftGrip, RightGrip, LeftFoot, RightFoot;
            internal MotorcycleChassisSnapshot Snapshot;
            internal Renderer[] HiddenRenderers;
            internal MotorcycleHandling Handling;
            internal MotorcycleEngineAudio Engine;
        }
        private sealed class Rider
        {
            internal Player Player;
            internal Animator Animator;
            internal Transform Hip, AvatarRoot;
            internal LandVehicle Vehicle;
            internal Vector3 Position;
            internal Quaternion Rotation;
            internal bool WasVisible, ThirdPersonVisible;
            internal bool MotorbikeBool;
            internal int EvaluatedFrame = -1;
            internal MotorcycleRiderVisibility Visibility;
            internal MotorcycleHandPose Hands;
            internal MotorcycleLegPose Legs;
        }
        private readonly Dictionary<int, Bike> _bikes = new();
        private readonly Dictionary<int, Rider> _riders = new();
        private readonly List<int> _expired = new();
        private readonly HashSet<int> _missingRigWarnings = new();
        private readonly HashSet<int> _missingHandWarnings = new();
        private readonly HashSet<int> _missingLegWarnings = new();
        private bool _updateFailureReported, _poseFailureReported;
        internal IEnumerable<Bike> Bikes => _bikes.Values;
        internal int Count => _bikes.Count;
        internal bool IsMotorcycle(LandVehicle vehicle) => vehicle != null && _bikes.ContainsKey(vehicle.GetInstanceID());
        internal void PrepareSteering(LandVehicle vehicle)
        { if (vehicle != null && _bikes.TryGetValue(vehicle.GetInstanceID(), out Bike bike)) bike.Handling.PrepareSteering(); }
        internal void Stabilize(LandVehicle vehicle)
        { if (vehicle != null && _bikes.TryGetValue(vehicle.GetInstanceID(), out Bike bike)) bike.Handling.Stability.FixedUpdate(); }
        internal static bool IsRiderPart(Component component)
        {
            // A client may receive the bike tag after a seat-occupant RPC has
            // already parented a player's avatar below the vehicle.
            if (Player.PlayerList != null) foreach (Player player in Player.PlayerList)
                if (player?.Avatar != null && component.transform.IsChildOf(player.Avatar.transform)) return true;
            return false;
        }
        private static LandVehicle RidingVehicle(Player player)
        { try { return player?.CurrentVehicle?.GetComponent<LandVehicle>(); } catch { return null; } }

        internal void Spawn()
        {
            LandVehicle vehicle = null;
            try
            {
                Player player = Player.Local;
                if (!SessionAuthorityService.Instance.FeaturesAllowed)
                { NotificationService.Instance.Warning("The host must approve NugzzMenu first"); return; }
                if (!LocalAnimationNetworkService.IsLocalAuthority)
                { NotificationService.Instance.Warning("Motorcycle spawning requires the host; solo games are supported"); return; }
                if (_bikes.Count >= Limit) { NotificationService.Instance.Warning("Remove a motorcycle first (maximum four)"); return; }
                foreach (Player peer in Player.PlayerList)
                    if (peer != null && !peer.IsLocalPlayer && !LocalAnimationNetworkService.Instance.MatchingPeer(peer))
                    { NotificationService.Instance.Warning("Approve every player and install this matching motorcycle build first"); return; }
                if (NativeActivityGateService.Instance.TryGetBlockReason(out string reason))
                { NotificationService.Instance.Warning("Motorcycle unavailable while " + reason); return; }
                if (LocalAnimationVisualService.Instance.MotorcycleAsset == null || !LocalAnimationRenderService.EnsureInitialized())
                { NotificationService.Instance.Warning("The motorcycle model is not loaded yet"); return; }
                Vector3 ahead = player.transform.position + player.transform.forward * 5f;
                if (!LocalAnimationPlacementService.Ground(player, ahead, out Vector3 ground) ||
                    Physics.CheckBox(ground + Vector3.up * 1f, new Vector3(0.7f, 0.8f, 1.5f), player.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
                { NotificationService.Instance.Warning("Find a clear, flat space ahead of you first"); return; }
                VehicleManager manager = UnityEngine.Object.FindObjectOfType<VehicleManager>();
                LandVehicle prefab = null;
                if (manager != null) foreach (LandVehicle candidate in manager.VehiclePrefabs)
                    if (candidate != null && candidate.gameObject.name == "Sedan") { prefab = candidate; break; }
                if (prefab == null) throw new InvalidOperationException("Registered sedan chassis is unavailable");
                // false keeps this temporary chassis out of PlayerOwnedVehicles/save
                // persistence. Our interaction hook permits riding without ownership.
                vehicle = manager.SpawnAndReturnVehicle(prefab.VehicleCode, ground + Vector3.up * 0.55f,
                    Quaternion.Euler(0f, player.transform.eulerAngles.y, 0f), false);
                if (vehicle == null || !Attach(vehicle)) throw new InvalidOperationException("Motorcycle conversion failed");
                MotorcycleNetworkService.Instance.Announce(vehicle);
                NotificationService.Instance.Notify("Motorcycle spawned. Interact to ride; use normal vehicle controls to drive and exit.");
            }
            catch (Exception ex)
            {
                if (vehicle != null) { Detach(vehicle); try { vehicle.DestroyVehicle(); } catch { } }
                DebugLogService.Instance.VerboseWarning("Motorcycle spawn failed: " + ex.Message);
                NotificationService.Instance.Warning("Motorcycle could not spawn: " + ex.Message);
            }
        }
        internal bool Attach(LandVehicle vehicle)
        {
            if (vehicle == null) return false;
            if (IsMotorcycle(vehicle)) return true;
            GameObject visual = null;
            MotorcycleChassisSnapshot snapshot = null;
            try
            {
                Motorbike source = LocalAnimationVisualService.Instance.MotorcycleAsset;
                if (source == null || vehicle.Rb == null || vehicle.intObj == null || vehicle.Seats == null || vehicle.Seats.Length == 0 ||
                    !LocalAnimationRenderService.EnsureInitialized()) return false;
                visual = new GameObject("Nugzz rideable motorcycle");
                visual.transform.SetParent(vehicle.transform, false);
                visual.transform.localPosition = Vector3.down * 0.35f;
                var copies = new Dictionary<int, Transform>();
                Transform model = NativeVisualCloneService.Copy(source.transform, visual.transform, copies);
                model.localRotation = LocalAnimationPlacementService.Facing(source.transform, source.Seat.SittingPoint, Quaternion.identity);
                Transform seat = copies[source.Seat.SittingPoint.GetInstanceID()];
                // The hip joint is inside the pelvis, above the buttocks' skin.
                // Lift the rider, not the bike's wheels, to clear the cushion.
                seat.position += vehicle.transform.up * MotorcycleRiderTargets.PelvisLift +
                    vehicle.transform.forward * MotorcycleRiderTargets.PelvisForward;
                Transform rear = copies[source.BurnoutController.rearWheelVisual.GetInstanceID()];
                Transform front = copies[source.BurnoutController.frontWheelContact.GetInstanceID()];
                MotorcycleGripTargets.Create(model, vehicle, out Transform leftGrip, out Transform rightGrip);
                MotorcycleRiderTargets.CreateFeet(model, out Transform leftFoot, out Transform rightFoot);
                // Validate before changing native components, so a missing asset never
                // leaves a half-converted driveable car.
                var wheelColliders = new List<WheelCollider>(vehicle.GetComponentsInChildren<WheelCollider>(true)).ToArray();
                if (wheelColliders.Length != 4 || vehicle.boundingBox == null) throw new InvalidOperationException("Unexpected vehicle chassis layout");
                snapshot = new MotorcycleChassisSnapshot(vehicle);
                var hidden = new List<Renderer>();
                foreach (LODGroup lod in vehicle.GetComponentsInChildren<LODGroup>(true)) if (!IsRiderPart(lod)) lod.enabled = false;
                foreach (Renderer renderer in vehicle.GetComponentsInChildren<Renderer>(true))
                    if (!renderer.transform.IsChildOf(visual.transform) && !IsRiderPart(renderer)) { renderer.enabled = false; hidden.Add(renderer); }
                foreach (Collider collider in vehicle.GetComponentsInChildren<Collider>(true))
                    if (collider.TryCast<WheelCollider>() == null && collider != vehicle.boundingBox && !IsRiderPart(collider)) collider.enabled = false;
                MotorcycleHandling.SizeBox(vehicle.boundingBox, vehicle, new Vector3(0f, 0.35f, 0f), new Vector3(0.65f, 1.35f, 2.7f));
                // Physical body on the unscaled root; a separate native interaction
                // trigger is raycastable without suspending the bike on a car box.
                var body = vehicle.gameObject.AddComponent<BoxCollider>();
                MotorcycleHandling.SizeBox(body, vehicle, new Vector3(0f, 0.3f, 0f), new Vector3(0.55f, 0.65f, 1.8f));
                snapshot.TrackCreated(body);
                var interactionBox = vehicle.intObj.gameObject.AddComponent<BoxCollider>();
                interactionBox.isTrigger = true;
                MotorcycleHandling.SizeBox(interactionBox, vehicle, new Vector3(0f, 0.35f, 0f), new Vector3(0.65f, 1.2f, 2.5f));
                snapshot.TrackCreated(interactionBox);
                MotorcycleHandling.Configure(vehicle, front, rear, wheelColliders);
                // Lift only after measuring/configuring the working wheel layout.
                // Ground contacts and hill physics retain their existing geometry.
                visual.transform.localPosition += Vector3.up * MotorcycleHandling.VisualClearance;
                vehicle.vehicleName = "Motorcycle";
                // One driver, native ownership transfer and exit controls. The native
                // prefab code/GUID and networking components remain untouched.
                VehicleSeat driver = vehicle.Seats[0];
                foreach (VehicleSeat candidate in vehicle.Seats) if (candidate.isDriverSeat) driver = candidate;
                driver.transform.SetPositionAndRotation(seat.position, seat.rotation);
                // Preserve the native seat array/indices used by in-flight RPCs.
                // Ride() and our hover prompt allow only an unoccupied motorcycle.
                vehicle.cameraOrigin.SetPositionAndRotation(seat.position + Vector3.up * 0.65f, seat.rotation);
                foreach (Transform exit in vehicle.exitPoints)
                    if (exit != null) exit.position = vehicle.transform.position + vehicle.transform.right * 1.4f;
                if (vehicle.Agent != null) vehicle.Agent.enabled = false;
                if (vehicle.Trunk != null) vehicle.Trunk.enabled = false;
                if (vehicle.Storage != null) vehicle.Storage.enabled = false;
                foreach (var interaction in vehicle.GetComponentsInChildren<Il2CppScheduleOne.Storage.StorageEntityInteractable>(true)) interaction.enabled = false;
                Transform frontWheel = null, fork = null;
                foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
                { if (t.name == "Wheel_Front") frontWheel = t; if (t.name == "Pivot" && t.parent?.name == "Bars") fork = t; }
                var engine = new MotorcycleEngineAudio(vehicle, visual, source, snapshot);
                MotorcyclePromptService.Attach(vehicle, snapshot);
                _bikes[vehicle.GetInstanceID()] = new Bike { Vehicle = vehicle, Visual = visual, Seat = seat, RearWheel = rear,
                    LeftGrip = leftGrip, RightGrip = rightGrip,
                    LeftFoot = leftFoot, RightFoot = rightFoot,
                    Snapshot = snapshot, HiddenRenderers = hidden.ToArray(), Engine = engine,
                    Handling = new MotorcycleHandling(vehicle, visual.transform, rear, frontWheel, fork, wheelColliders) };
                return true;
            }
            catch (Exception ex)
            {
                snapshot?.Restore();
                if (visual != null) UnityEngine.Object.Destroy(visual);
                DebugLogService.Instance.VerboseWarning("Motorcycle attachment failed: " + ex.Message);
                return false;
            }
        }
        internal void Ride(LandVehicle vehicle)
        {
            try
            {
                if (!IsMotorcycle(vehicle) || !SessionAuthorityService.Instance.FeaturesAllowed || Player.Local?.CurrentVehicle != null) return;
                if (vehicle.IsOccupied) { NotificationService.Instance.Warning("Motorcycle occupied"); return; }
                if (NativeActivityGateService.Instance.TryGetBlockReason(out string reason)) { NotificationService.Instance.Warning(reason); return; }
                LocalAnimationService.Instance.Stop(false);
                vehicle.EnterVehicle();
                if (Player.Local.CurrentVehicle == null) NotificationService.Instance.Warning("The native driving state is not ready; try again in a moment");
            }
            catch (Exception ex) { NotificationService.Instance.Warning("Could not mount motorcycle: " + ex.Message); }
        }
        internal void RemoveUnoccupied()
        {
            if (!LocalAnimationNetworkService.IsLocalAuthority) { NotificationService.Instance.Warning("Only the host can remove motorcycles"); return; }
            _expired.Clear();
            foreach (var entry in _bikes) if (entry.Value.Vehicle == null || !entry.Value.Vehicle.IsOccupied) _expired.Add(entry.Key);
            foreach (int id in _expired)
            {
                Bike bike = _bikes[id]; _bikes.Remove(id);
                bike.Engine?.Dispose();
                MotorcycleNetworkService.Instance.Remove(bike.Vehicle);
                if (bike.Visual != null) UnityEngine.Object.Destroy(bike.Visual);
                if (bike.Vehicle != null) bike.Vehicle.DestroyVehicle();
            }
            NotificationService.Instance.Status("Removed unoccupied motorcycles");
        }
        internal void Detach(LandVehicle vehicle)
        {
            if (vehicle == null || !_bikes.Remove(vehicle.GetInstanceID(), out Bike bike)) return;
            bike.Engine?.Dispose();
            bike.Snapshot?.Restore();
            if (bike.Visual != null) UnityEngine.Object.Destroy(bike.Visual);
        }
        internal void Update()
        {
            try { UpdateCore(); }
            catch (Exception ex)
            {
                // One report per scene, never per-frame log spam or a game crash.
                if (!_updateFailureReported) DebugLogService.Instance.VerboseWarning("Motorcycle update failed: " + ex);
                _updateFailureReported = true;
            }
        }
        internal void RefreshVisibility(LandVehicle vehicle)
        {
            if (vehicle == null || !_bikes.TryGetValue(vehicle.GetInstanceID(), out Bike bike)) return;
            foreach (Renderer renderer in bike.HiddenRenderers) if (renderer != null) renderer.enabled = false;
            if (bike.Visual != null) bike.Visual.SetActive(vehicle.IsVisible);
        }
        private void UpdateCore()
        {
            MotorcycleNetworkService.Instance.Update();
            _expired.Clear();
            foreach (var entry in _bikes)
            {
                Bike bike = entry.Value;
                if (bike.Vehicle == null || bike.Visual == null) { _expired.Add(entry.Key); continue; }
                RefreshVisibility(bike.Vehicle);
                bike.Handling.Update(); bike.Engine.Update();
            }
            foreach (int id in _expired) { _bikes[id].Engine?.Dispose(); _bikes.Remove(id); }
            if (Player.PlayerList == null) return;
            foreach (Player player in Player.PlayerList)
            {
                if (player == null || !IsMotorcycle(RidingVehicle(player)) || player.IsRagdolled || player.Health == null || !player.Health.IsAlive) continue;
                Animator animator = player.Avatar?.Animation?._animator;
                Transform hip = LocalAnimationPlacementService.Hip(player);
                LandVehicle vehicle = RidingVehicle(player);
                Transform avatarRoot = RiderRoot(player, vehicle, hip);
                if (animator == null || hip == null || avatarRoot == null)
                {
                    if (_missingRigWarnings.Add(player.GetInstanceID()))
                        DebugLogService.Instance.VerboseWarning("Motorcycle rider rig unavailable: animator=" + (animator != null) +
                            ", hips=" + (hip != null) + ", child avatar=" + (avatarRoot != null));
                    continue;
                }
                int id = player.GetInstanceID();
                if (_riders.TryGetValue(id, out Rider existing) && (existing.Animator != animator || existing.Hip != hip || existing.AvatarRoot != avatarRoot || existing.Vehicle != vehicle))
                { Restore(existing); _riders.Remove(id); }
                if (!_riders.ContainsKey(id)) _riders[id] = new Rider { Player = player, Animator = animator, Hip = hip,
                    AvatarRoot = avatarRoot, Vehicle = vehicle, Position = avatarRoot.localPosition, Rotation = avatarRoot.localRotation,
                    WasVisible = avatarRoot.gameObject.activeSelf, ThirdPersonVisible = player.ThirdPersonMeshesVisibleToLocalPlayer,
                    MotorbikeBool = animator.GetBool("IsMotorbikeSitting"), Visibility = new MotorcycleRiderVisibility(player) };
                animator.SetBool("Sitting", false); animator.SetBool("IsMotorbikeSitting", true);
            }
            _expired.Clear();
            foreach (var entry in _riders)
                if (entry.Value.Player == null || !IsMotorcycle(RidingVehicle(entry.Value.Player)) || entry.Value.Player.IsRagdolled || !SessionAuthorityService.Instance.FeaturesAllowed) _expired.Add(entry.Key);
            foreach (int id in _expired) { Restore(_riders[id]); _riders.Remove(id); }
        }
        internal void ApplyRiderPoses()
        {
            try { ApplyRiderPosesCore(); }
            catch (Exception ex)
            {
                if (!_poseFailureReported) DebugLogService.Instance.VerboseWarning("Motorcycle rider presentation failed: " + ex);
                _poseFailureReported = true;
            }
        }
        private void ApplyRiderPosesCore()
        {
            if (!SessionAuthorityService.Instance.FeaturesAllowed) return;
            foreach (Bike bike in _bikes.Values) if (bike.Vehicle != null && bike.Visual != null) bike.Handling.AlignSuspension();
            foreach (Rider rider in _riders.Values)
                if (rider.Player != null && !rider.Player.IsRagdolled && rider.Player.Health != null && rider.Player.Health.IsAlive &&
                    rider.Player.Avatar?.Animation?._animator == rider.Animator && rider.Hip != null &&
                    rider.Player.Avatar.transform == rider.AvatarRoot && LocalAnimationPlacementService.Hip(rider.Player) == rider.Hip &&
                    RidingVehicle(rider.Player) != null && _bikes.TryGetValue(RidingVehicle(rider.Player).GetInstanceID(), out Bike bike))
                {
                    // Native Player.EnterVehicle hides and reparents the avatar.
                    // This open vehicle needs the real avatar visible on every peer.
                    if (!rider.Visibility.Apply()) continue;
                    rider.Animator.SetBool("Sitting", false); rider.Animator.SetBool("IsMotorbikeSitting", true);
                    if (rider.EvaluatedFrame != Time.frameCount)
                    { rider.Animator.Update(0f); rider.EvaluatedFrame = Time.frameCount; }
                    rider.AvatarRoot.rotation = bike.Seat.rotation;
                    LocalAnimationPlacementService.Align(rider.AvatarRoot, rider.Hip, bike.Seat);
                    if (rider.Hands == null || !rider.Hands.IsCurrent)
                        rider.Hands = MotorcycleHandPose.Create(rider.Player, rider.AvatarRoot);
                    if (rider.Hands != null) rider.Hands.Apply(bike.LeftGrip, bike.RightGrip, bike.Handling.SteeringDeltaWorld);
                    else if (_missingHandWarnings.Add(rider.Player.GetInstanceID()))
                        DebugLogService.Instance.VerboseWarning("Motorcycle hand anchors unavailable on the current appearance rig");
                    if (rider.Legs == null || !rider.Legs.IsCurrent)
                        rider.Legs = MotorcycleLegPose.Create(rider.Player, rider.AvatarRoot);
                    if (rider.Legs != null) rider.Legs.Apply(bike.LeftFoot, bike.RightFoot);
                    else if (_missingLegWarnings.Add(rider.Player.GetInstanceID()))
                        DebugLogService.Instance.VerboseWarning("Motorcycle leg anchors unavailable on the current appearance rig");
                }
        }
        private static Transform RiderRoot(Player player, LandVehicle vehicle, Transform hip)
        {
            if (player == null || vehicle == null) return null;
            Transform root = player?.Avatar?.transform;
            // Native seating moves Avatar directly under LandVehicle, outside the
            // player hierarchy. Never move a player/vehicle network root itself.
            return root != null && root != player.transform && root != vehicle.transform && hip != null && hip.IsChildOf(root) &&
                (root.IsChildOf(player.transform) || root.IsChildOf(vehicle.transform)) ? root : null;
        }
        private static void Restore(Rider rider)
        {
            rider.Hands?.Release(RidingVehicle(rider.Player) == rider.Vehicle);
            rider.Legs?.Release(RidingVehicle(rider.Player) == rider.Vehicle);
            rider.Visibility?.Release();
            if (rider.Player == null || rider.Player.IsRagdolled || rider.Player.Avatar?.Animation?._animator != rider.Animator) return;
            rider.Animator.SetBool("IsMotorbikeSitting", rider.MotorbikeBool);
            rider.Animator.SetBool("Sitting", rider.Player.CurrentVehicle != null);
            if (rider.AvatarRoot != null && rider.Player.Avatar.transform == rider.AvatarRoot)
            {
                // ExitVehicle restores the native walking transform before clearing
                // CurrentVehicle. Do not overwrite that with a saved car-seat offset.
                if (rider.Player.CurrentVehicle != null && rider.Vehicle != null && rider.AvatarRoot.IsChildOf(rider.Vehicle.transform))
                { rider.AvatarRoot.localPosition = rider.Position; rider.AvatarRoot.localRotation = rider.Rotation; rider.Player.Avatar.SetVisible(rider.WasVisible); }
                if (rider.Player.IsLocalPlayer) rider.Player.SetThirdPersonMeshesVisibility(rider.ThirdPersonVisible);
            }
        }
        internal void ResetForScene()
        {
            foreach (Rider rider in _riders.Values) { try { Restore(rider); } catch { } }
            foreach (Bike bike in _bikes.Values)
            {
                bike.Engine?.Dispose();
                bike.Snapshot?.Restore();
                if (bike.Visual != null) UnityEngine.Object.Destroy(bike.Visual);
            }
            _riders.Clear(); _bikes.Clear(); _expired.Clear(); _missingRigWarnings.Clear(); _missingHandWarnings.Clear(); _missingLegWarnings.Clear();
            _updateFailureReported = false; _poseFailureReported = false; MotorcycleNetworkService.Instance.Reset();
        }
    }
    [HarmonyPatch(typeof(LandVehicle), "SetVisible")]
    internal static class MotorcycleVisibilityPatch
    {
        private static void Postfix(LandVehicle __instance)
        { try { MotorcycleService.Instance.RefreshVisibility(__instance); } catch { } }
    }
    [HarmonyPatch(typeof(LandVehicle), "Interacted")]
    internal static class MotorcycleInteractionPatch
    {
        private static bool Prefix(LandVehicle __instance)
        {
            if (!MotorcycleService.Instance.IsMotorcycle(__instance)) return true;
            try { MotorcycleService.Instance.Ride(__instance); } catch { }
            return false;
        }
    }
    [HarmonyPatch(typeof(LandVehicle), "Hovered")]
    internal static class MotorcycleHoverPatch
    {
        private static bool Prefix(LandVehicle __instance)
        {
            if (!MotorcycleService.Instance.IsMotorcycle(__instance)) return true;
            try
            {
                if (__instance.intObj != null) MotorcyclePromptService.Hover(__instance);
            }
            catch { }
            // The car hover callback must not alternate Enter vehicle/full with
            // our motorcycle label. Native hover/click/input binding stay intact.
            return false;
        }
    }
}
