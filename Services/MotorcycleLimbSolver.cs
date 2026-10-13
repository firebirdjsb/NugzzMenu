using System;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class MotorcycleLimbSolver
    {
        // Two-bone solve with an authored bend direction. Rotations only: no
        // stretching, bone translation, Rigidbody or network-root changes.
        internal static bool Solve(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 bendDirection)
        {
            Vector3 origin = upper.position, joint = lower.position;
            float a = (joint - origin).magnitude, b = (end.position - joint).magnitude;
            float reach = (target - origin).magnitude;
            if (a < 0.001f || b < 0.001f || reach < 0.001f || !float.IsFinite(a + b + reach)) return false;
            float minimum = Math.Abs(a - b) + 0.0001f, maximum = (a + b) * 0.9999f;
            if (minimum > maximum) return false;
            Vector3 direction = (target - origin).normalized;
            float distance = Mathf.Clamp(reach, minimum, maximum);
            Vector3 bend = bendDirection - direction * Vector3.Dot(bendDirection, direction);
            if (bend.sqrMagnitude < 0.000001f) bend = Vector3.Cross(direction, Vector3.right);
            if (bend.sqrMagnitude < 0.000001f) bend = Vector3.Cross(direction, Vector3.up);
            if (bend.sqrMagnitude < 0.000001f) return false;
            float along = (a * a - b * b + distance * distance) / (2f * distance);
            float height = (float)Math.Sqrt(Math.Max(0f, a * a - along * along));
            Vector3 desiredJoint = origin + direction * along + bend.normalized * height;
            upper.rotation = Quaternion.FromToRotation(joint - origin, desiredJoint - origin) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(end.position - lower.position,
                origin + direction * distance - lower.position) * lower.rotation;
            return true;
        }
    }
}
