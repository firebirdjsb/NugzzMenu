using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    // Roll back only the cloned chassis this conversion owns, including failed
    // attachment attempts. Never mutate the registered prefab or other vehicles.
    internal sealed class MotorcycleChassisSnapshot
    {
        private readonly List<Action> _restore = new();
        internal MotorcycleChassisSnapshot(LandVehicle v)
        {
            if (v.cameraOrigin == null || v.exitPoints == null || v.steerWheels == null)
                throw new InvalidOperationException("Vehicle seat/camera/wheel references are incomplete");
            foreach (Renderer r in v.GetComponentsInChildren<Renderer>(true)) if (!MotorcycleService.IsRiderPart(r)) { bool enabled = r.enabled; _restore.Add(() => { if (r != null) r.enabled = enabled; }); }
            foreach (LODGroup lod in v.GetComponentsInChildren<LODGroup>(true)) if (!MotorcycleService.IsRiderPart(lod)) { bool enabled = lod.enabled; _restore.Add(() => { if (lod != null) lod.enabled = enabled; }); }
            if (v.Storage != null) { bool enabled = v.Storage.enabled; _restore.Add(() => { if (v.Storage != null) v.Storage.enabled = enabled; }); }
            foreach (var interaction in v.GetComponentsInChildren<Il2CppScheduleOne.Storage.StorageEntityInteractable>(true))
            { bool enabled = interaction.enabled; _restore.Add(() => { if (interaction != null) interaction.enabled = enabled; }); }
            foreach (Collider c in v.GetComponentsInChildren<Collider>(true)) if (!MotorcycleService.IsRiderPart(c)) { bool enabled = c.enabled; _restore.Add(() => { if (c != null) c.enabled = enabled; }); }
            foreach (WheelCollider w in v.GetComponentsInChildren<WheelCollider>(true))
            {
                Vector3 pos = w.transform.localPosition, center = w.center; float radius = w.radius, distance = w.suspensionDistance, mass = w.mass; JointSpring spring = w.suspensionSpring;
                WheelFrictionCurve forward = w.forwardFriction, side = w.sidewaysFriction;
                _restore.Add(() => { if (w != null) { w.transform.localPosition = pos; w.center = center; w.radius = radius; w.suspensionDistance = distance; w.mass = mass; w.suspensionSpring = spring; w.forwardFriction = forward; w.sidewaysFriction = side; } });
            }
            Vector3 boundCenter = v.boundingBox.center, size = v.boundingBox.size;
            _restore.Add(() => { v.boundingBox.center = boundCenter; v.boundingBox.size = size; });
            float rbMass = v.Rb.mass, drag = v.Rb.angularDrag; Vector3 com = v.Rb.centerOfMass; RigidbodyConstraints constraints = v.Rb.constraints;
            _restore.Add(() => { v.Rb.mass = rbMass; v.Rb.centerOfMass = com; v.Rb.constraints = constraints; v.Rb.angularDrag = drag; });
            float speed = v.TopSpeed, steer = v.maxSteeringAngle, force = v.downforce, gearing = v.diffGearing; string name = v.vehicleName;
            _restore.Add(() => { v.TopSpeed = speed; v.maxSteeringAngle = steer; v.downforce = force; v.diffGearing = gearing; v.vehicleName = name; });
            float rate = v.steerRate, overridden = v.OverriddenMaxSteerAngle; bool hasOverride = v.MaxSteerAngleOverridden;
            _restore.Add(() => { v.steerRate = rate; if (hasOverride) v.OverrideMaxSteerAngle(overridden); else v.ResetMaxSteerAngle(); });
            float brake = v.BrakeForceMultiplier, handbrake = v.handBrakeForce;
            bool controls = v.overrideControls, handbrakeOverride = v.handbrakeOverride;
            float throttle = v.throttleOverride, steerOverride = v.steerOverride;
            _restore.Add(() => { v.BrakeForceMultiplier = brake; v.handBrakeForce = handbrake; v.overrideControls = controls;
                v.throttleOverride = throttle; v.steerOverride = steerOverride; v.handbrakeOverride = handbrakeOverride; });
            var seats = v.Seats; _restore.Add(() => v.Seats = seats);
            if (v.centerOfMass != null) SaveTransform(v.centerOfMass);
            SaveTransform(v.cameraOrigin);
            foreach (VehicleSeat seat in seats) SaveTransform(seat.transform);
            foreach (Transform exit in v.exitPoints) if (exit != null) SaveTransform(exit);
            if (v.Agent != null) { bool enabled = v.Agent.enabled; _restore.Add(() => { if (v.Agent != null) v.Agent.enabled = enabled; }); }
            if (v.Trunk != null) { bool enabled = v.Trunk.enabled; _restore.Add(() => { if (v.Trunk != null) v.Trunk.enabled = enabled; }); }
        }
        private void SaveTransform(Transform t)
        {
            Vector3 position = t.localPosition; Quaternion rotation = t.localRotation;
            _restore.Add(() => { if (t != null) { t.localPosition = position; t.localRotation = rotation; } });
        }
        internal void TrackCreated(UnityEngine.Object component)
        { _restore.Add(() => { if (component != null) UnityEngine.Object.Destroy(component); }); }
        internal void TrackRestore(Action restore) => _restore.Add(restore);
        internal void Restore()
        {
            foreach (Action restore in _restore) { try { restore(); } catch { } }
            _restore.Clear();
        }
    }
}
