using System;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class TeleportService
    {
        private static readonly TeleportService _instance = new TeleportService();
        public static TeleportService Instance => _instance;

        private Vector3 _savedPosition = Vector3.zero;
        private bool _hasSavedPosition = false;

        private TeleportService() { }

        public void SavePosition()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player != null)
                {
                    _savedPosition = player.transform.position;
                    _hasSavedPosition = true;
                    NotificationService.Instance.Notify($"Position saved: {_savedPosition.x:F1}, {_savedPosition.y:F1}, {_savedPosition.z:F1}");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Save position failed: {ex.Message}");
            }
        }

        public void LoadPosition()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null) return;

                if (!_hasSavedPosition)
                {
                    NotificationService.Instance.Notify("No position saved");
                    return;
                }

                player.transform.position = _savedPosition;
                NotificationService.Instance.Notify($"Teleported to saved position");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Load position failed: {ex.Message}");
            }
        }

    }
}
