using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal sealed class MotorcycleLegPose
    {
        private sealed class Leg
        {
            internal Transform Upper, Lower, Foot;
            internal Quaternion UpperRotation, LowerRotation, FootRotation;
            internal bool Valid(Transform root) => Upper != null && Lower != null && Foot != null &&
                Upper != root && Upper != Lower && Lower != Foot && Upper.IsChildOf(root) &&
                Lower.IsChildOf(Upper) && Foot.IsChildOf(Lower);
            internal void Capture()
            { UpperRotation = Upper.localRotation; LowerRotation = Lower.localRotation; FootRotation = Foot.localRotation; }
            internal void Restore()
            { Upper.localRotation = UpperRotation; Lower.localRotation = LowerRotation; Foot.localRotation = FootRotation; }
        }
        private readonly Player _player;
        private readonly Transform _root;
        private readonly Leg _left, _right;
        private int _frame = -1;
        private MotorcycleLegPose(Player player, Transform root, Leg left, Leg right)
        { _player = player; _root = root; _left = left; _right = right; }

        internal static MotorcycleLegPose Create(Player player, Transform root)
        {
            var provider = player?.Avatar?.AttachmentAnchorProvider;
            if (provider == null || root == null) return null;
            var left = new Leg { Upper = provider.LeftUpperLeg, Lower = provider.LeftLowerLeg, Foot = provider.LeftFoot };
            var right = new Leg { Upper = provider.RightUpperLeg, Lower = provider.RightLowerLeg, Foot = provider.RightFoot };
            return left.Valid(root) && right.Valid(root) ? new MotorcycleLegPose(player, root, left, right) : null;
        }
        internal bool IsCurrent
        {
            get
            {
                var p = _player?.Avatar?.AttachmentAnchorProvider;
                return p != null && _player.Avatar.transform == _root && _left.Valid(_root) && _right.Valid(_root) &&
                    p.LeftUpperLeg == _left.Upper && p.LeftLowerLeg == _left.Lower && p.LeftFoot == _left.Foot &&
                    p.RightUpperLeg == _right.Upper && p.RightLowerLeg == _right.Lower && p.RightFoot == _right.Foot;
            }
        }
        internal void Apply(Transform leftFoot, Transform rightFoot)
        {
            if (!IsCurrent || leftFoot == null || rightFoot == null) return;
            if (_frame == Time.frameCount) { _left.Restore(); _right.Restore(); }
            else { _left.Capture(); _right.Capture(); _frame = Time.frameCount; }
            Solve(_left, leftFoot.position, -1f); Solve(_right, rightFoot.position, 1f);
        }
        private void Solve(Leg leg, Vector3 target, float side)
        {
            Quaternion footRotation = leg.Foot.rotation;
            // Knees face forward and slightly outward around the engine, rather
            // than retaining the stationary revving pose's hanging shins.
            if (MotorcycleLimbSolver.Solve(leg.Upper, leg.Lower, leg.Foot, target,
                _root.rotation * new Vector3(side * 0.4f, 0f, 1f))) leg.Foot.rotation = footRotation;
        }
        internal void Release(bool stillRiding)
        {
            if (stillRiding && !_player.IsRagdolled && _frame == Time.frameCount && IsCurrent)
            { _left.Restore(); _right.Restore(); }
            _frame = -1;
        }
    }
}
