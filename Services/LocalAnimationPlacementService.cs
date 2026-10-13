using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class LocalAnimationPlacementService
    {
        internal static Transform Hip(Player player)
        {
            var anchors = player?.Avatar?.AttachmentAnchorProvider;
            if (anchors?.Hips != null) return anchors.Hips;
            if (player?.Avatar?.HipBone != null) return player.Avatar.HipBone;
            var bones = player?.Avatar?.Animation?._bones;
            if (bones != null) foreach (Transform bone in bones)
                if (bone != null && (bone.name == "Hips" || bone.name.EndsWith(":Hips", System.StringComparison.Ordinal))) return bone;
            return null;
        }
        internal static Transform PoseRoot(Player player)
        {
            Transform root = player?.Avatar?.transform;
            Transform hip = Hip(player);
            // Only move the complete child avatar, never the movement/network root.
            return root != null && root != player.transform && root.IsChildOf(player.transform) &&
                hip != null && hip.IsChildOf(root) ? root : null;
        }
        internal static void Align(Transform root, Transform hip, Transform seat)
        {
            if (root == null || hip == null || seat == null) return;
            Vector3 offset = seat.position - hip.position;
            if (offset.sqrMagnitude > 0.00000001f) root.position += offset;
        }
        internal static bool Ground(Player player, Vector3 origin, out Vector3 point)
        {
            point = origin; float closest = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(origin + Vector3.up * 2f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(player.transform) || hit.normal.y < 0.65f || hit.distance >= closest) continue;
                closest = hit.distance; point = hit.point;
            }
            return closest < float.MaxValue;
        }
        internal static Quaternion Facing(Transform model, Transform sittingPoint, Quaternion playerRotation)
            => playerRotation * Quaternion.Inverse(Quaternion.Inverse(model.rotation) * sittingPoint.rotation);
    }
}
