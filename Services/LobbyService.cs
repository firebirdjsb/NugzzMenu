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

                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                    cc.enabled = !enabled;
                Debug.Log($"[Nugzz] Set ragdoll: {enabled}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to set ragdoll: {ex.Message}");
            }
        }
    }
}
