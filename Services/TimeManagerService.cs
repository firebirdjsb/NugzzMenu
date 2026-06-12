using System;
using Il2CppScheduleOne.GameTime;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Service for managing game time and time-related operations
    /// </summary>
    public sealed class TimeManagerService
    {
        public static TimeManagerService Instance { get; } = new TimeManagerService();
        private TimeManagerService() { }
        private const float MIN_TIME_SPEED = 0f;
        private const float MAX_TIME_SPEED = 10f;
        private const float DEFAULT_TIME_SPEED = 1f;

        /// <summary>
        /// Gets the current time manager instance
        /// </summary>
        private TimeManager GetTimeManager()
        {
            try
            {
                return UnityEngine.Object.FindObjectOfType<TimeManager>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to get TimeManager: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the game time speed multiplier (host-only in multiplayer)
        /// </summary>
        public void SetTimeSpeed(float speedMultiplier)
        {
            // Host-only check for multiplayer
            var lobbyService = LobbyService.Instance;
            if (lobbyService.IsInLobby() && !lobbyService.IsHost())
            {
                Debug.LogWarning("[Nugzz] Time control is host-only in multiplayer");
                return;
            }
            
            try
            {
                var timeManager = GetTimeManager();
                if (timeManager == null)
                {
                    Debug.LogError("[Nugzz] TimeManager not found");
                    return;
                }

                // Clamp speed to valid range
                float clampedSpeed = Mathf.Clamp(speedMultiplier, MIN_TIME_SPEED, MAX_TIME_SPEED);
                timeManager.SetTimeSpeedMultiplier(clampedSpeed);
                Debug.Log($"[Nugzz] Set time speed to {clampedSpeed}x");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to set time speed: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current time speed multiplier
        /// </summary>
        public float GetTimeSpeed()
        {
            try
            {
                var timeManager = GetTimeManager();
                if (timeManager == null)
                    return DEFAULT_TIME_SPEED;

                return timeManager.TimeSpeedMultiplier;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to get time speed: {ex.Message}");
                return DEFAULT_TIME_SPEED;
            }
        }

        /// <summary>
        /// Sets the game time to a specific hour (host-only in multiplayer)
        /// </summary>
        public void SetTimeOfDay(int hour)
        {
            // Host-only check for multiplayer
            var lobbyService = LobbyService.Instance;
            if (lobbyService.IsInLobby() && !lobbyService.IsHost())
            {
                Debug.LogWarning("[Nugzz] Time control is host-only in multiplayer");
                return;
            }
            
            try
            {
                var timeManager = GetTimeManager();
                if (timeManager == null)
                {
                    Debug.LogError("[Nugzz] TimeManager not found");
                    return;
                }

                // Clamp hour to valid range (0-23)
                int clampedHour = Mathf.Clamp(hour, 0, 23);
                int minuteValue = clampedHour * 60; // Convert hours to minutes
                timeManager.SetTimeAndSync(minuteValue);
                Debug.Log($"[Nugzz] Set time to {clampedHour:D2}:00");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to set time of day: {ex.Message}");
            }
        }

        /// <summary>
        /// Pauses the game (sets time speed to 0)
        /// </summary>
        public void PauseGame()
        {
            SetTimeSpeed(0f);
        }

        /// <summary>
        /// Resumes the game (sets time speed to 1)
        /// </summary>
        public void ResumeGame()
        {
            SetTimeSpeed(DEFAULT_TIME_SPEED);
        }
    }
}
