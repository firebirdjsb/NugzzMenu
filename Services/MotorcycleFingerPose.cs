using System;
using System.Collections.Generic;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal sealed class MotorcycleFingerPose
    {
        private sealed class Finger
        { internal Transform Bone; internal Quaternion Rotation; internal float Curl; }
        private readonly List<Finger> _fingers = new();

        internal MotorcycleFingerPose(Transform hand)
        {
            foreach (Transform bone in hand.GetComponentsInChildren<Transform>(true))
            {
                string name = bone.name;
                int colon = name.LastIndexOf(':'); if (colon >= 0) name = name.Substring(colon + 1);
                float curl;
                // The installed player rig uses Index1..4 for the four-finger
                // cluster and Thumb1..4 for the thumb. Local +X closes each
                // cluster toward the palm. End markers are never rotated.
                if (name.EndsWith("HandIndex1", StringComparison.Ordinal)) curl = 55f;
                else if (name.EndsWith("HandIndex2", StringComparison.Ordinal)) curl = 65f;
                else if (name.EndsWith("HandIndex3", StringComparison.Ordinal)) curl = 45f;
                else if (name.EndsWith("HandIndex4", StringComparison.Ordinal)) curl = 10f;
                else if (name.EndsWith("HandThumb1", StringComparison.Ordinal)) curl = 15f;
                else if (name.EndsWith("HandThumb2", StringComparison.Ordinal)) curl = 30f;
                else if (name.EndsWith("HandThumb3", StringComparison.Ordinal)) curl = 20f;
                else continue;
                _fingers.Add(new Finger { Bone = bone, Curl = curl });
            }
        }

        internal void Capture()
        { foreach (Finger finger in _fingers) if (finger.Bone != null) finger.Rotation = finger.Bone.localRotation; }
        internal void Restore()
        { foreach (Finger finger in _fingers) if (finger.Bone != null) finger.Bone.localRotation = finger.Rotation; }
        internal void Apply()
        {
            foreach (Finger finger in _fingers)
                if (finger.Bone != null)
                {
                    Quaternion q = finger.Rotation;
                    // Replace the native local X curl rather than adding another
                    // curl every frame; retain its existing twist/thumb splay.
                    float pitch = (float)Math.Atan2(2f * (q.w * q.x + q.y * q.z),
                        1f - 2f * (q.x * q.x + q.y * q.y)) * Mathf.Rad2Deg;
                    finger.Bone.localRotation = Quaternion.AngleAxis(finger.Curl - pitch, Vector3.right) * q;
                }
        }
    }
}
