using System;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class MotorcycleRiderTargets
    {
        internal const float PelvisLift = 0.20f, PelvisForward = 0.08f;
        internal static void CreateFeet(Transform model, out Transform left, out Transform right)
        {
            Transform frame = null;
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true))
                if (child.name == "Chopper_Frame01_LOD0") { frame = child; break; }
            if (frame == null) throw new InvalidOperationException("Motorcycle frame is unavailable");
            // Authored ankle targets, in metres in the copied frame's local
            // space. Outside the 0.22m half-width engine/frame, with 8cm ankle
            // clearance above the 0.35m foot-rest level. Follow bank/hills and
            // suspension; no new colliders or vehicle controls are introduced.
            left = Foot(frame, -0.27f); right = Foot(frame, 0.27f);
        }
        private static Transform Foot(Transform frame, float x)
        {
            Transform target = new GameObject("Nugzz motorcycle foot rest").transform;
            target.SetParent(frame, false); target.localPosition = new Vector3(x, 0.43f, 0.06f);
            return target;
        }
    }
}
