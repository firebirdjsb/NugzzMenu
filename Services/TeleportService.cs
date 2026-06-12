using System;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.DevUtilities;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Manages teleport position save/load functionality.
    /// </summary>
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

        /// <summary>
        /// Teleports player to tutorial spawn point.
        /// </summary>
        public void TeleportToTutorialTown()
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                var gameManager = GameManager.Instance;
                
                if (player == null || gameManager == null)
                {
                    NotificationService.Instance.Notify("Cannot teleport: player or game manager not found");
                    return;
                }

                if (gameManager.SpawnPoint != null)
                {
                    player.transform.position = gameManager.SpawnPoint.position;
                    NotificationService.Instance.Notify("Teleported to tutorial spawn");
                }
                else
                {
                    // Fallback: use NoHomeRespawnPoint
                    if (gameManager.NoHomeRespawnPoint != null)
                    {
                        player.transform.position = gameManager.NoHomeRespawnPoint.position;
                        NotificationService.Instance.Notify("Teleported to no-home respawn point");
                    }
                    else
                    {
                        NotificationService.Instance.Notify("No spawn point available");
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Tutorial teleport failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Ends tutorial mode and loads the main game.
        /// </summary>
        public void EndTutorialMode()
        {
            try
            {
                var gameManager = GameManager.Instance;
                if (gameManager != null)
                {
                    // EndTutorial ends tutorial mode and loads main game
                    gameManager.EndTutorial(true);
                    NotificationService.Instance.Notify("Tutorial ended");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"End tutorial failed: {ex.Message}");
            }
        }

        public Vector3 GetSavedPosition() => _savedPosition;
        public bool HasSavedPosition() => _hasSavedPosition;
    }
}