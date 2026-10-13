using System;
using UnityEngine;
using Il2CppScheduleOne.Vehicles;

namespace NugzzMenu.Services
{
    internal static class MotorcycleGripTargets
    {
        internal static void Create(Transform model, LandVehicle vehicle, out Transform left, out Transform right)
        {
            Transform handlebars = null;
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
                if (child.name == "Chopper_Handlebars01_LOD0") { handlebars = child; break; }
            if (handlebars == null) throw new InvalidOperationException("Motorcycle handlebar mesh is unavailable");
            // Measured from the installed Chopper_Handlebars01_LOD0 mesh, in
            // metres in that mesh's local space. These points lie inside the
            // rubber grips, slightly inward from the +/-0.56m end caps.
            // At the inner grip rings, the tube centre is higher and farther
            // forward than at the outer end caps used by the previous revision.
            left = Make(handlebars, "Left grip", new Vector3(-0.44f, 1.285f, 0.0484f), -90f);
            right = Make(handlebars, "Right grip", new Vector3(0.44f, 1.285f, 0.0484f), 90f);
            if (vehicle.transform.InverseTransformPoint(left.position).x >
                vehicle.transform.InverseTransformPoint(right.position).x)
            { Transform swap = left; left = right; right = swap; }
        }

        private static Transform Make(Transform parent, string name, Vector3 position, float handRoll)
        {
            Transform target = new GameObject("Nugzz motorcycle " + name).transform;
            target.SetParent(parent, false); target.localPosition = position;
            // Native AlignmentPoint uses opposite +/-90 degree wrist rolls.
            // Its forward axis faces the palm; face it down onto the bar, with
            // mirrored finger direction and the grip axis across the palm.
            target.localRotation = Quaternion.AngleAxis(-Math.Sign(handRoll) * 10f, Vector3.forward) * Quaternion.Euler(90f, 0f, handRoll);
            return target;
        }
    }
}
