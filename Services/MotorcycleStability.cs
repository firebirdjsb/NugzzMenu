using System;
using HarmonyLib;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    // Only the native physics owner adjusts the Rigidbody. Observers receive the
    // existing vehicle transform/RPC stream and animate their displayed fork.
    internal sealed class MotorcycleStability
    {
        private readonly LandVehicle _vehicle;
        private readonly WheelCollider[] _wheels;
        private float _pitch;
        private bool _wasOwner;

        internal MotorcycleStability(LandVehicle vehicle, WheelCollider[] wheels)
        { _vehicle = vehicle; _wheels = wheels; }

        internal static void KeepGrip(WheelCollider wheel)
        {
            // Native ApplyFriction reassigns these curves every physics tick,
            // including rain/handbrake slip. Apply the bike's grip floor after it.
            WheelFrictionCurve forward = wheel.forwardFriction, side = wheel.sidewaysFriction;
            forward.stiffness = Math.Max(forward.stiffness, 1.3f);
            side.stiffness = Math.Max(side.stiffness, 2f);
            wheel.forwardFriction = forward; wheel.sidewaysFriction = side;
        }

        internal void FixedUpdate()
        {
            Rigidbody rb = _vehicle.Rb;
            if (rb == null || rb.isKinematic || !_vehicle.IsPhysicallySimulated ||
                !(_vehicle.IsOwner || (_vehicle.OwnerId == -1 && LocalAnimationNetworkService.IsLocalAuthority)))
            { _wasOwner = false; return; }

            // Freeze uncommanded pitch/roll, including the native car turnover
            // torque. MoveRotation supplies a bounded road-following pitch instead.
            rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Vector3 normal = Vector3.zero; int contacts = 0;
            foreach (WheelCollider wheel in _wheels)
                if (MotorcycleGroundProbe.TryNormal(_vehicle, wheel, out Vector3 surfaceNormal))
                { normal += surfaceNormal; contacts++; }
            // Read the physics orientation, not the interpolated rendering
            // transform: using yesterday's yaw would fight native steering.
            Vector3 forward = rb.rotation * Vector3.forward;
            float yaw = (float)Math.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            if (!_wasOwner)
                _pitch = Mathf.Clamp((float)Math.Atan2(-forward.y,
                    Math.Sqrt(forward.x * forward.x + forward.z * forward.z)) * Mathf.Rad2Deg, -40f, 40f);
            _wasOwner = true;
            float pitchTarget = _pitch;
            if (contacts > 0)
            {
                normal = normal.normalized;
                Vector3 heading = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                pitchTarget = Mathf.Clamp((float)Math.Atan2(Vector3.Dot(heading, normal), normal.y) * Mathf.Rad2Deg, -40f, 40f);
            }
            // Retain the take-off pitch in the air; don't snap level over crests.
            _pitch = Mathf.MoveTowards(_pitch, pitchTarget, Time.fixedDeltaTime * 60f);
            // Preserve the native tires' turn momentum before correcting pitch.
            // MoveRotation must not replace that input with its generated yaw.
            float nativeYawVelocity = rb.angularVelocity.y;
            rb.MoveRotation(Quaternion.Euler(_pitch, yaw, 0f));

            // Cap excessive yaw rather than inventing another steering input.
            // Native tires still initiate the turn; a sharp impact cannot spin
            // the chassis repeatedly. Angular velocity is radians per second.
            float yawLimit = Mathf.Clamp(1.5f - Math.Abs(_vehicle.Speed_Kmh) * 0.012f, 0.7f, 1.5f);
            rb.angularVelocity = Vector3.up * Mathf.Clamp(nativeYawVelocity, -yawLimit, yawLimit);
            if (contacts > 0 && _vehicle.IsOccupied)
                rb.AddForce(normal * -1.5f, ForceMode.Acceleration);
        }
    }

    [HarmonyPatch(typeof(LandVehicle), "UpdateSteerAngle")]
    internal static class MotorcycleSteeringPatch
    {
        private static void Prefix(LandVehicle __instance)
        { try { MotorcycleService.Instance.PrepareSteering(__instance); } catch { } }
    }

    [HarmonyPatch(typeof(LandVehicle), "FixedUpdate")]
    internal static class MotorcycleStabilityPatch
    {
        private static void Postfix(LandVehicle __instance)
        { try { MotorcycleService.Instance.Stabilize(__instance); } catch { } }
    }

    [HarmonyPatch(typeof(Wheel), "ApplyFriction")]
    internal static class MotorcycleGripPatch
    {
        private static void Postfix(Wheel __instance)
        {
            try
            {
                if (__instance?.wheelCollider != null && MotorcycleService.Instance.IsMotorcycle(__instance.vehicle))
                    MotorcycleStability.KeepGrip(__instance.wheelCollider);
            }
            catch { }
        }
    }
}
