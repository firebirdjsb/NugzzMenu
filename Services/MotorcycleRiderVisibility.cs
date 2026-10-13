using System;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    // Open vehicles need the actual dressed avatar, including rigid hair and
    // attachments. Activating Avatar alone doesn't undo BodyContainer culling
    // or Renderer.enabled=false. No appearance/material rebuilding is involved.
    internal sealed class MotorcycleRiderVisibility
    {
        private readonly Player _player;
        private Renderer[] _renderers = Array.Empty<Renderer>();
        private float _nextRefresh;
        private bool _reported;

        internal MotorcycleRiderVisibility(Player player) { _player = player; }

        internal bool Apply()
        {
            var avatar = _player.Avatar;
            if (avatar == null || avatar.Animation?._animator == null) return false;
            if (!avatar.gameObject.activeSelf) avatar.SetVisible(true);
            bool refresh = Time.unscaledTime >= _nextRefresh;
            if (_player.IsLocalPlayer)
            {
                // Native car entry and the first-person transition hide layers;
                // newly loaded clothing must receive the visible layer as well.
                if (refresh || !_player.ThirdPersonMeshesVisibleToLocalPlayer)
                    _player.SetThirdPersonMeshesVisibility(true);
                if (avatar.IsCulled)
                {
                    avatar._IsCulled_k__BackingField = false;
                    try { avatar.OnAvatarCullingChange?.Invoke(false); } catch { }
                }
                if (avatar.BodyContainer != null && !avatar.BodyContainer.gameObject.activeSelf)
                    avatar.BodyContainer.gameObject.SetActive(true);
                avatar._impostor?.Disable();
            }
            else
            {
                // Remote car entry also hides the avatar. Re-evaluate normal
                // distance/frustum culling, rather than displaying distant riders.
                if (refresh) avatar.UpdateAnimationActive();
                if (avatar.IsCulled) return false;
            }
            if (refresh)
            {
                _renderers = avatar.GetComponentsInChildren<Renderer>(true);
                _nextRefresh = Time.unscaledTime + 0.5f;
            }
            foreach (Renderer renderer in _renderers)
                if (renderer != null && renderer.gameObject.activeInHierarchy &&
                    (avatar._impostor == null || !renderer.transform.IsChildOf(avatar._impostor.transform)))
                    renderer.enabled = true;
            avatar.Animation._animator.enabled = true;
            if (!_reported)
            {
                _reported = true;
                DebugLogService.Instance.Verbose("Motorcycle rider visible: local=" + _player.IsLocalPlayer +
                    ", body=" + (avatar.BodyContainer?.gameObject.activeInHierarchy ?? avatar.gameObject.activeInHierarchy) +
                    ", renderers=" + _renderers.Length);
            }
            return true;
        }

        internal void Release()
        {
            // Native exit restores walking placement/layers. Resume its body
            // culling too, including scene reset while an occupant is still seated.
            try { _player?.Avatar?.UpdateAnimationActive(); } catch { }
            _renderers = Array.Empty<Renderer>();
        }
    }
}
