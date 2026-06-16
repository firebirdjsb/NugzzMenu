using System;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class LobbyService
    {
        private static readonly LobbyService _instance = new LobbyService();
        public static LobbyService Instance => _instance;
        private LobbyService() { }
        public bool IsHost()
        {
            try
            {
                var lobby = Lobby.Instance;
                return lobby != null && lobby.IsInLobby && lobby.IsHost;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to check host status: {ex.Message}");
                return false;
            }
        }

        public bool IsInLobby()
        {
            try
            {
                var lobby = Lobby.Instance;
                return lobby != null && lobby.IsInLobby;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to check lobby status: {ex.Message}");
                return false;
            }
        }

        public Il2CppSystem.Collections.Generic.List<Player> GetPlayerList()
        {
            return Player.PlayerList;
        }

        public void TeleportPlayer(Player targetPlayer)
        {
            try
            {
                var localPlayer = Player.Local;
                if (localPlayer == null || targetPlayer == null)
                {
                    Debug.LogError("[Nugzz] Local or target player is null");
                    return;
                }

                Vector3 targetPosition = targetPlayer.transform.position + targetPlayer.transform.right * 2f;
                localPlayer.transform.position = targetPosition;
                Debug.Log($"[Nugzz] Teleported to {targetPlayer.PlayerName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to teleport player: {ex.Message}");
            }
        }

        public void TeleportPlayerUp(float distance)
        {
            try
            {
                var player = Player.Local;
                if (player == null)
                    return;

                Vector3 currentPos = player.transform.position;
                player.transform.position = currentPos + Vector3.up * distance;
                Debug.Log($"[Nugzz] Teleported up {distance}m");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to teleport up: {ex.Message}");
            }
        }

        public void SetRagdoll(bool enabled)
        {
            try
            {
                var player = Player.Local;
                if (player == null) return;

                if (enabled)
                {
                    TryCallNetworkedPassOut(player);
                    ForceLocalRagdollState(player, true);
                }
                else
                {
                    TryCallNetworkedPassOutRecovery(player);
                    ForceLocalRagdollState(player, false);
                }

                NotificationService.Instance.Status(enabled ? "Ragdolled" : "Standing");
                Debug.Log($"[Nugzz] Set networked ragdoll: {enabled}");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Ragdoll failed: {ex.Message}");
                Debug.LogError($"[Nugzz] Failed to set ragdoll: {ex.Message}");
            }
        }

        private static void TryCallNetworkedPassOut(Player player)
        {
            try { player.SendPassOut(); }
            catch (Exception ex) { Debug.LogWarning("[Nugzz] SendPassOut failed: " + ex.Message); }

            try { player.PassOut(); }
            catch { }
        }

        private static void TryCallNetworkedPassOutRecovery(Player player)
        {
            try { player.SendPassOutRecovery(); }
            catch (Exception ex) { Debug.LogWarning("[Nugzz] SendPassOutRecovery failed: " + ex.Message); }

            try { player.PassOutRecovery(); }
            catch { }
        }

        private static void ForceLocalRagdollState(Player player, bool enabled)
        {
            try { player.SetRagdolled(enabled); } catch { }

            try
            {
                PlayerMovement movement = PlayerMovement.Instance;
                if (movement != null)
                {
                    movement.IsRagdolled = enabled;
                    if (!enabled)
                    {
                        movement.CanMove = true;
                        movement.CanJump = true;
                    }
                }
            }
            catch { }

            try
            {
                CharacterController controller = player.CharacterController;
                if (controller != null)
                    controller.enabled = !enabled;
            }
            catch { }

            if (!enabled)
            {
                try
                {
                    CharacterController controller = player.GetComponent<CharacterController>();
                    if (controller != null)
                        controller.enabled = true;
                }
                catch { }
            }
        }
    }
}
