using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class MotorcycleGroundProbe
    {
        internal static bool TryNormal(LandVehicle vehicle, WheelCollider wheel, out Vector3 normal)
        {
            normal = Vector3.zero;
            if (vehicle == null || wheel == null || !wheel.enabled || !wheel.isGrounded) return false;

            // Do not call GetGroundHit: the generated WheelHit by-ref binding
            // supplies pointer-sized storage for a larger native value structure.
            // RaycastAll returns a blittable RaycastHit array, as used for spawning.
            const float aboveAxle = 0.3f;
            float reach = aboveAxle + wheel.radius + wheel.suspensionDistance + 0.15f;
            Vector3 origin = wheel.transform.TransformPoint(wheel.center) + Vector3.up * aboveAxle;
            float closest = float.MaxValue;
            bool found = false;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, reach, ~0, QueryTriggerInteraction.Ignore))
            {
                Collider collider = hit.collider;
                if (collider == null || !collider.enabled || collider.isTrigger ||
                    !(hit.distance >= 0f && hit.distance <= reach) || hit.distance >= closest ||
                    collider.transform.IsChildOf(vehicle.transform) || collider.GetComponentInParent<Player>() != null ||
                    MotorcycleService.IsRiderPart(collider)) continue;
                closest = hit.distance; normal = hit.normal; found = true;
            }
            // The closest physical surface must be suitable; never look through
            // a steep obstruction and treat a more distant floor as wheel contact.
            return found && normal.y > 0.45f;
        }
    }
}
