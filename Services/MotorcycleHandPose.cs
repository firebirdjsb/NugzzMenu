using System;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    // Render-only two-bone IK on the current appearance rig. No player/network
    // root, bone positions, bone scales or shared animation assets are changed.
    internal sealed class MotorcycleHandPose
    {
        private sealed class Arm
        {
            internal Transform Upper, Lower, Hand, Palm, Shoulder;
            internal Quaternion UpperRotation, LowerRotation, HandRotation, ShoulderRotation;
            internal MotorcycleFingerPose Fingers;
            internal bool Valid(Transform root) => Upper != null && Lower != null && Hand != null &&
                Upper != root && Lower != root && Hand != root && Upper != Lower && Lower != Hand &&
                Upper.IsChildOf(root) && Lower.IsChildOf(Upper) && Hand.IsChildOf(Lower);
            internal void Capture()
            {
                UpperRotation = Upper.localRotation; LowerRotation = Lower.localRotation; HandRotation = Hand.localRotation;
                if (Shoulder != null) ShoulderRotation = Shoulder.localRotation;
                Fingers.Capture();
            }
            internal void Restore()
            {
                if (Shoulder != null) Shoulder.localRotation = ShoulderRotation;
                Upper.localRotation = UpperRotation; Lower.localRotation = LowerRotation; Hand.localRotation = HandRotation;
                Fingers.Restore();
            }
        }
        private readonly Player _player;
        private readonly Transform _root;
        private readonly Arm _left, _right;
        private readonly Transform _spine;
        private Quaternion _spineRotation;
        private int _frame = -1;

        private MotorcycleHandPose(Player player, Transform root, Arm left, Arm right, Transform spine)
        { _player = player; _root = root; _left = left; _right = right; _spine = spine; }

        internal static MotorcycleHandPose Create(Player player, Transform root)
        {
            var provider = player?.Avatar?.AttachmentAnchorProvider;
            if (provider == null || root == null) return null;
            var left = new Arm { Upper = provider.LeftUpperArm, Lower = provider.LeftLowerArm, Hand = provider.LeftHand,
                Palm = player.Avatar.LeftHandAlignmentPoint, Shoulder = provider.LeftShoulder };
            var right = new Arm { Upper = provider.RightUpperArm, Lower = provider.RightLowerArm, Hand = provider.RightHand,
                Palm = player.Avatar.RightHandAlignmentPoint, Shoulder = provider.RightShoulder };
            if (!left.Valid(root) || !right.Valid(root)) return null;
            foreach (Arm arm in new[] { left, right })
            {
                if (arm.Shoulder == root || arm.Shoulder == arm.Upper || arm.Shoulder == null ||
                    !arm.Shoulder.IsChildOf(root) || !arm.Upper.IsChildOf(arm.Shoulder)) arm.Shoulder = null;
                arm.Fingers = new MotorcycleFingerPose(arm.Hand);
            }
            Transform spine = provider.LowerSpine;
            if (spine == null || spine == root || !spine.IsChildOf(root) ||
                !left.Upper.IsChildOf(spine) || !right.Upper.IsChildOf(spine)) spine = null;
            return new MotorcycleHandPose(player, root, left, right, spine);
        }

        internal bool IsCurrent
        {
            get
            {
                var provider = _player?.Avatar?.AttachmentAnchorProvider;
                return provider != null && _player.Avatar.transform == _root && _left.Valid(_root) && _right.Valid(_root) &&
                    provider.LeftUpperArm == _left.Upper && provider.LeftLowerArm == _left.Lower && provider.LeftHand == _left.Hand &&
                    provider.RightUpperArm == _right.Upper && provider.RightLowerArm == _right.Lower && provider.RightHand == _right.Hand &&
                    (_spine == null || provider.LowerSpine == _spine) &&
                    (_left.Shoulder == null || provider.LeftShoulder == _left.Shoulder) &&
                    (_right.Shoulder == null || provider.RightShoulder == _right.Shoulder) &&
                    _player.Avatar.LeftHandAlignmentPoint == _left.Palm && _player.Avatar.RightHandAlignmentPoint == _right.Palm;
            }
        }

        internal void Apply(Transform leftGrip, Transform rightGrip, Quaternion steeringDelta)
        {
            if (!IsCurrent || leftGrip == null || rightGrip == null) return;
            if (_frame == Time.frameCount) RestoreBones();
            else
            {
                _left.Capture(); _right.Capture(); _frame = Time.frameCount;
                if (_spine != null) _spineRotation = _spine.localRotation;
            }
            Quaternion leftRotation = WristRotation(_left, leftGrip, steeringDelta), rightRotation = WristRotation(_right, rightGrip, steeringDelta);
            Vector3 leftTarget = WristTarget(_left, leftGrip.position, leftRotation), rightTarget = WristTarget(_right, rightGrip.position, rightRotation);
            Vector3 gripAxis = _root.InverseTransformDirection(rightGrip.position - leftGrip.position);
            float torsoTurn = Mathf.Clamp(-(float)Math.Atan2(gripAxis.z, gripAxis.x) * Mathf.Rad2Deg, -20f, 20f);
            PrepareReach(leftTarget, rightTarget, torsoTurn);
            Solve(_left, leftTarget, leftRotation, -1f);
            Solve(_right, rightTarget, rightRotation, 1f);
            _left.Fingers.Apply(); _right.Fingers.Apply();
        }

        private static Quaternion WristRotation(Arm arm, Transform grip, Quaternion steeringDelta)
        {
            return arm.Palm != null && arm.Palm.IsChildOf(arm.Hand) ?
                grip.rotation * Quaternion.Inverse(Quaternion.Inverse(arm.Hand.rotation) * arm.Palm.rotation) :
                steeringDelta * arm.Hand.rotation;
        }

        private static Vector3 WristTarget(Arm arm, Vector3 grip, Quaternion handRotation)
        {
            Vector3 palmOffset = arm.Palm != null && arm.Palm.IsChildOf(arm.Hand) ?
                Vector3.Scale(arm.Hand.InverseTransformPoint(arm.Palm.position), arm.Hand.lossyScale) : Vector3.zero;
            return grip - handRotation * palmOffset;
        }

        private void RestoreBones()
        { if (_spine != null) _spine.localRotation = _spineRotation; _left.Restore(); _right.Restore(); }

        private static float ReachError(Arm arm, Vector3 target)
        {
            float length = (arm.Lower.position - arm.Upper.position).magnitude + (arm.Hand.position - arm.Lower.position).magnitude;
            // Reserve a small elbow bend instead of solving at full extension.
            return Math.Max(0f, (target - arm.Upper.position).magnitude - length * 0.95f);
        }

        private static void AimShoulder(Arm arm, Vector3 target)
        {
            if (arm.Shoulder == null) return;
            Vector3 from = (arm.Upper.position - arm.Shoulder.position).normalized, to = (target - arm.Shoulder.position).normalized;
            Vector3 axis = Vector3.Cross(from, to);
            if (axis.sqrMagnitude < 0.000001f) return;
            float angle = (float)Math.Acos(Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f)) * Mathf.Rad2Deg;
            arm.Shoulder.rotation = Quaternion.AngleAxis(Math.Min(angle, 35f), axis.normalized) * arm.Shoulder.rotation;
        }

        private void PrepareReach(Vector3 left, Vector3 right, float torsoTurn)
        {
            // Real arms are about 0.51m, not the long arms of the original test
            // fixture. Use the clavicles and a bounded seated torso lean before
            // the two-bone solve; hips/legs remain anchored to the saddle.
            if (Math.Max(ReachError(_left, left), ReachError(_right, right)) <= 0.0001f) return;
            float bestLean = 0f, bestError = float.MaxValue;
            for (float lean = 0f; lean <= (_spine != null ? 30f : 0f); lean += 5f)
            {
                RestoreBones();
                if (_spine != null) _spine.rotation = Quaternion.AngleAxis(lean, _root.right) * _spine.rotation * Quaternion.AngleAxis(torsoTurn, Vector3.up);
                AimShoulder(_left, left); AimShoulder(_right, right);
                float error = Math.Max(ReachError(_left, left), ReachError(_right, right));
                if (error < bestError) { bestError = error; bestLean = lean; }
                if (error <= 0.0001f) break;
            }
            RestoreBones();
            if (_spine != null) _spine.rotation = Quaternion.AngleAxis(bestLean, _root.right) * _spine.rotation * Quaternion.AngleAxis(torsoTurn, Vector3.up);
            AimShoulder(_left, left); AimShoulder(_right, right);
        }

        private void Solve(Arm arm, Vector3 target, Quaternion handRotation, float side)
        {
            // A stable down/back pole keeps elbows relaxed. The previous solve
            // inherited the native revving pose's sideways/upward elbow plane.
            if (MotorcycleLimbSolver.Solve(arm.Upper, arm.Lower, arm.Hand, target,
                _root.rotation * new Vector3(side * 0.2f, -1f, -0.25f))) arm.Hand.rotation = handRotation;
        }

        internal void Release(bool stillRiding)
        {
            // Native exit/ragdoll/appearance replacement owns its new pose. Only
            // undo a pose we applied this frame while still in the original seat.
            if (stillRiding && !_player.IsRagdolled && _frame == Time.frameCount && IsCurrent)
                RestoreBones();
            _frame = -1;
        }
    }
}
