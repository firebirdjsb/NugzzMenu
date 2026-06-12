using System;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Provides multiplayer-safe execution checks and host authority validation.
    /// </summary>
    public sealed class MultiplayerGuardService
    {
        public static MultiplayerGuardService Instance { get; } = new MultiplayerGuardService();
        private MultiplayerGuardService() { }

        /// <summary>
        /// Checks if the current player can spawn vehicles (host-only in multiplayer).
        /// </summary>
        public bool CanSpawnVehicle()
        {
            return !LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost();
        }

        /// <summary>
        /// Checks if the current player can modify economy (host-only in multiplayer).
        /// </summary>
        public bool CanModifyEconomy()
        {
            return !LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost();
        }

        /// <summary>
        /// Checks if the current player can edit the world (host-only in multiplayer).
        /// </summary>
        public bool CanEditWorld()
        {
            return !LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost();
        }

        /// <summary>
        /// Checks if the current player can modify time (host-only in multiplayer).
        /// </summary>
        public bool CanModifyTime()
        {
            return !LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost();
        }

        /// <summary>
        /// Executes an action only if allowed in current network context.
        /// </summary>
        /// <param name="action">The action to execute if allowed.</param>
        /// <param name="requirement">The permission requirement check.</param>
        /// <returns>True if action was executed, false otherwise.</returns>
        public bool TryExecute(Action action, Func<bool> requirement)
        {
            if (requirement == null || !requirement())
                return false;

            try
            {
                action?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error($"Action failed: {ex.Message}");
                return false;
            }
        }
    }
}