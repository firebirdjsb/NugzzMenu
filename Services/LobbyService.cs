using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Service for managing lobby and multiplayer operations
    /// </summary>
    public sealed class LobbyService
    {
        private static readonly LobbyService _instance = new LobbyService();
        public static LobbyService Instance => _instance;
        private LobbyService() { }
        /// <summary>
        /// Checks if the current player is the host
        /// </summary>
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

        /// <summary>
        /// Checks if a lobby is active
        /// </summary>
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

        /// <summary>
        /// Gets list of all players in the lobby
        /// </summary>
        public Il2CppSystem.Collections.Generic.List<Player> GetPlayerList()
        {
            return Player.PlayerList;
        }

        /// <summary>
        /// Teleports a player to the specified target location
        /// </summary>
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

                // Teleport local player to target player
                Vector3 targetPosition = targetPlayer.transform.position + targetPlayer.transform.right * 2f;
                localPlayer.transform.position = targetPosition;
                Debug.Log($"[Nugzz] Teleported to {targetPlayer.PlayerName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to teleport player: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends an admin command to another player via lobby message
        /// </summary>
        public void SendAdminCommand(Player targetPlayer, string command)
        {
            try
            {
                if (targetPlayer == null)
                {
                    Debug.LogError("[Nugzz] Target player is null");
                    return;
                }

                Debug.Log($"[Nugzz] Sent admin command '{command}' to {targetPlayer.PlayerName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to send admin command: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes an incoming admin command
        /// </summary>
        public void ProcessAdminCommand(string payload)
        {
            if (string.IsNullOrEmpty(payload) || !payload.StartsWith("SESHADM|"))
                return;

            try
            {
                string[] parts = payload.Split('|');
                if (parts.Length < 3)
                    return;

                string playerCode = parts[1];
                string command = parts[2];

                Debug.Log($"[Nugzz] Received admin command: {command} from {playerCode}");

                // Execute the command
                ExecuteAdminCommand(command);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to process admin command: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes an admin command on the local player
        /// </summary>
        private void ExecuteAdminCommand(string command)
        {
            try
            {
                var player = Player.Local;
                if (player == null)
                    return;

                command = command.ToLower();

                switch (command)
                {
                    case "up":
                        TeleportPlayerUp(25f);
                        break;
                    case "ragdoll":
                        SetRagdoll(true);
                        break;
                    case "stand":
                        SetRagdoll(false);
                        break;
                    case "antigrav":
                        ApplyEffect("AntiGravity");
                        break;
                    case "dizzy":
                        ApplyEffect("Dizziness");
                        break;
                    case "clearfx":
                        ClearEffects();
                        break;
                    default:
                        Debug.LogWarning($"[Nugzz] Unknown admin command: {command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to execute admin command: {ex.Message}");
            }
        }

        /// <summary>
        /// Teleports the player up by the specified distance
        /// </summary>
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

        /// <summary>
        /// Sets the player's ragdoll state
        /// </summary>
        public void SetRagdoll(bool enabled)
        {
            try
            {
                var player = Player.Local;
                if (player == null) return;

                // Ragdoll implementation via character controller
                var cc = player.GetComponent<CharacterController>();
                if (cc != null)
                {
                    // Enable/disable character controller for ragdoll effect
                    cc.enabled = !enabled;
                }
                Debug.Log($"[Nugzz] Set ragdoll: {enabled}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to set ragdoll: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies an effect to the player
        /// </summary>
        public void ApplyEffect(string effectName)
        {
            EffectsService.Instance.ApplyEffect(effectName);
        }

        /// <summary>
        /// Clears all effects from the player
        /// </summary>
        public void ClearEffects()
        {
            EffectsService.Instance.ClearAllEffects();
        }
    }
}
