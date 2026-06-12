using System;
using Il2CppScheduleOne.GameTime;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class TimeManagerService
    {
        public static TimeManagerService Instance { get; } = new TimeManagerService();
        private const float MinTimeSpeed = 0f;
        private const float MaxTimeSpeed = 10f;

        private TimeManagerService() { }

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

        public void SetTimeSpeed(float speedMultiplier)
        {
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

                float clampedSpeed = Mathf.Clamp(speedMultiplier, MinTimeSpeed, MaxTimeSpeed);
                timeManager.SetTimeSpeedMultiplier(clampedSpeed);
                Debug.Log($"[Nugzz] Set time speed to {clampedSpeed}x");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to set time speed: {ex.Message}");
            }
        }
        public void SetTimeOfDay(int hour)
        {
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

                int clampedHour = Mathf.Clamp(hour, 0, 23);
                int minuteValue = clampedHour * 60;
                timeManager.SetTimeAndSync(minuteValue);
                Debug.Log($"[Nugzz] Set time to {clampedHour:D2}:00");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to set time of day: {ex.Message}");
            }
        }
    }
}
