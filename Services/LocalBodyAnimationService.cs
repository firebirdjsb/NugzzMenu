using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.AvatarFramework.Animation;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    // An authored, clothed solo emote. It changes render-skeleton joints only;
    // the player root, movement capsule, outfit and native controller stay intact.
    internal sealed class LocalBodyAnimationService
    {
        internal static readonly LocalBodyAnimationService Instance = new();
        internal static readonly string[] JointNames =
        { "Hips", "Spine", "Spine1", "LeftArm", "RightArm", "LeftForeArm", "RightForeArm", "LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg" };
        private sealed class Joint
        {
            internal Transform Bone;
            internal Vector3 Position, PitchAxis;
            internal Quaternion Rotation;
        }
        private sealed class Pose
        {
            internal Player Player;
            internal AvatarAnimation Rig;
            internal int RigId;
            internal Joint[] Joints;
            internal Vector3 Forward, Up, Origin;
            internal float StartedAt, EndsAt;
        }
        private readonly Dictionary<int, Pose> _poses = new();
        private readonly List<int> _expired = new();
        internal int ActiveCount => _poses.Count;

        internal bool CanAnimate(Player player)
        {
            try { return TryGetJoints(player, out _); }
            catch { return false; }
        }

        private static bool TryGetJoints(Player player, out Transform[] joints)
        {
            joints = new Transform[JointNames.Length];
            if (player?.Avatar?.Animation == null) return false;
            // 0.4.7 attachments bind to this provider. Use a coherent current rig
            // rather than mixing cached/body/clothing bones with equal names.
            var anchors = player.Avatar.AttachmentAnchorProvider;
            if (anchors != null)
            {
                joints = new[] { anchors.Hips, anchors.LowerSpine, anchors.MiddleSpine,
                    anchors.LeftUpperArm, anchors.RightUpperArm, anchors.LeftLowerArm, anchors.RightLowerArm,
                    anchors.LeftUpperLeg, anchors.RightUpperLeg, anchors.LeftLowerLeg, anchors.RightLowerLeg };
                foreach (Transform joint in joints) if (joint == null || joint.parent == null) return false;
                return true;
            }
            var bones = new List<Transform>();
            // Appearance rebuilds can bind a new render skeleton. Prefer the actual
            // mesh bones over an outdated serialized ragdoll/animation bone cache.
            foreach (SkinnedMeshRenderer skin in player.Avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (skin != null && skin.bones != null) foreach (Transform bone in skin.bones) bones.Add(bone);
            var cached = player.Avatar.Animation._bones;
            if (cached != null) foreach (Transform bone in cached) bones.Add(bone);
            foreach (Transform bone in bones)
            {
                if (bone == null) continue;
                string name = bone.name;
                int colon = name.LastIndexOf(':');
                if (colon >= 0) name = name.Substring(colon + 1);
                for (int i = 0; i < JointNames.Length; i++)
                    if (joints[i] == null && string.Equals(name, JointNames[i], StringComparison.Ordinal)) joints[i] = bone;
            }
            foreach (Transform joint in joints) if (joint == null || joint.parent == null) return false;
            return true;
        }

        internal bool Start(Player player, float duration, float elapsed = 0f)
        {
            try
            {
                if (duration <= 0f || elapsed < 0f || elapsed >= LocalAnimationCatalog.Duration ||
                    !LocalAnimationRenderService.EnsureInitialized() || !TryGetJoints(player, out Transform[] bones)) return false;
                Stop(player);
                var joints = new Joint[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                    joints[i] = new Joint { Bone = bones[i], Position = bones[i].localPosition,
                        Rotation = bones[i].localRotation,
                        PitchAxis = bones[i].parent.InverseTransformDirection(player.transform.right) };
                _poses[player.GetInstanceID()] = new Pose { Player = player, Rig = player.Avatar.Animation,
                    RigId = player.Avatar.Animation.GetInstanceID(),
                    Joints = joints, Origin = player.transform.position, StartedAt = Time.unscaledTime - elapsed,
                    EndsAt = Time.unscaledTime + Math.Min(duration, LocalAnimationCatalog.Duration - elapsed),
                    Forward = bones[0].parent.InverseTransformVector(player.transform.forward),
                    Up = bones[0].parent.InverseTransformVector(player.transform.up) };
                return true;
            }
            catch { return false; }
        }

        internal void Synchronize(Player player, float duration, float elapsed)
        {
            if (player == null || elapsed < 0f || elapsed >= LocalAnimationCatalog.Duration || duration <= 0f) return;
            if (_poses.TryGetValue(player.GetInstanceID(), out Pose pose))
            {
                // Keep the original joint snapshots. Resnapshotting an already animated
                // skeleton would bake the previous offset into the next loop.
                pose.StartedAt = Time.unscaledTime - elapsed;
                pose.EndsAt = Time.unscaledTime + Math.Min(duration, LocalAnimationCatalog.Duration - elapsed);
            }
        }

        internal static void Sample(float elapsed, float remaining, out float wave, out float bounce, out float weight)
        {
            const double period = 1.25; // Authored loop length in seconds, independent of FPS.
            double phase = Math.Max(0, elapsed) * (Math.PI * 2 / period);
            wave = (float)Math.Sin(phase);
            bounce = (float)(1 - Math.Cos(phase * 2)) * 0.5f;
            float fade = Math.Max(0f, Math.Min(1f, Math.Min(elapsed, remaining) / 0.2f));
            weight = fade * fade * (3f - 2f * fade); // Smooth entry/exit over 0.2 seconds.
        }

        internal void Apply(AvatarAnimation rig)
        {
            if (_poses.Count == 0 || rig == null) return;
            int rigId = rig.GetInstanceID();
            foreach (Pose pose in _poses.Values)
                if (pose.RigId == rigId) { ApplyPose(pose); break; }
        }

        internal void LateUpdate()
        {
            // Also covers locally hidden/cull-disabled native animation components.
            // Applying twice in a frame is idempotent: every sample uses fixed snapshots.
            foreach (Pose pose in _poses.Values)
            {
                try { ApplyPose(pose); }
                catch { /* Update discards a replaced or destroyed rig without a log loop. */ }
            }
        }

        private static bool IsValid(Pose pose) => pose.Player != null && pose.Rig != null &&
            pose.Player.Avatar?.Animation == pose.Rig && !pose.Player.IsRagdolled &&
            pose.Player.CurrentVehicle == null && pose.Player.Health != null && pose.Player.Health.IsAlive &&
            (pose.Player.Avatar.AttachmentAnchorProvider == null || pose.Player.Avatar.AttachmentAnchorProvider.Hips == pose.Joints[0].Bone) &&
            SessionAuthorityService.Instance.FeaturesAllowed &&
            (pose.Player.transform.position - pose.Origin).sqrMagnitude <= 0.36f;

        private static void ApplyPose(Pose pose)
        {
            if (!IsValid(pose) || Time.unscaledTime >= pose.EndsAt) return;
            foreach (Joint joint in pose.Joints) if (joint.Bone == null) return;
            Sample(Time.unscaledTime - pose.StartedAt, pose.EndsAt - Time.unscaledTime,
                out float wave, out float bounce, out float weight);
            // Angles are authored degrees around the avatar's pitch axis, transformed
            // into each joint parent's coordinate frame. No rig-memory offsets are used.
            for (int i = 0; i < pose.Joints.Length; i++)
            {
                Joint joint = pose.Joints[i];
                float angle = i switch
                {
                    0 => 10f * wave, 1 => -7f * wave, 2 => -4f * wave,
                    3 or 4 => -20f + 4f * wave, 5 or 6 => -30f - 5f * wave,
                    7 or 8 => -10f - 3f * bounce, _ => 18f + 4f * bounce
                };
                joint.Bone.localRotation = Quaternion.AngleAxis(angle * weight, joint.PitchAxis) * joint.Rotation;
            }
            Joint hips = pose.Joints[0];
            // Small cosmetic displacement in metres, compensated for parent scale.
            hips.Bone.localPosition = hips.Position +
                (pose.Forward * (0.055f * wave) + pose.Up * (0.02f * bounce)) * weight;
        }

        internal void Update()
        {
            _expired.Clear();
            foreach (var entry in _poses)
            {
                try
                {
                    bool valid = IsValid(entry.Value) && Time.unscaledTime < entry.Value.EndsAt;
                    foreach (Joint joint in entry.Value.Joints) valid &= joint.Bone != null;
                    if (!valid) _expired.Add(entry.Key);
                }
                catch { _expired.Add(entry.Key); }
            }
            foreach (int key in _expired) { Restore(_poses[key]); _poses.Remove(key); }
        }

        internal void Stop(Player player)
        {
            if (player != null && _poses.Remove(player.GetInstanceID(), out Pose pose)) Restore(pose);
        }

        private static void Restore(Pose pose)
        {
            // Ragdoll/death and replacement rigs already own their transforms. Restoring
            // an old standing pose over them would snap the native physics/appearance.
            try
            {
                if (pose.Player == null || pose.Player.IsRagdolled || pose.Player.Health == null ||
                    !pose.Player.Health.IsAlive || pose.Player.Avatar?.Animation != pose.Rig ||
                    (pose.Player.Avatar.AttachmentAnchorProvider != null && pose.Player.Avatar.AttachmentAnchorProvider.Hips != pose.Joints[0].Bone)) return;
                foreach (Joint joint in pose.Joints)
                    if (joint.Bone != null) { joint.Bone.localPosition = joint.Position; joint.Bone.localRotation = joint.Rotation; }
            }
            catch { }
        }

        internal void Reset()
        {
            foreach (Pose pose in _poses.Values) Restore(pose);
            _poses.Clear(); _expired.Clear();
        }
    }

    [HarmonyPatch(typeof(AvatarAnimation), "LateUpdate")]
    internal static class LocalBodyAnimationLatePatch
    {
        private static void Postfix(AvatarAnimation __instance)
        {
            try { LocalBodyAnimationService.Instance.Apply(__instance); }
            catch { /* A native scene/rig teardown must not throw through the game loop. */ }
        }
    }
}
