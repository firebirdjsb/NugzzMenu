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
                NotificationService.Instance.Warning("Time speed is host-only");
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
                NotificationService.Instance.Status($"Time speed: {clampedSpeed:0.##}x");
                Debug.Log($"[Nugzz] Set time speed to {clampedSpeed}x");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to set time speed: {ex.Message}");
            }
        }
        public void SetTimeOfDay(int minuteOfDay)
        {
            var lobbyService = LobbyService.Instance;
            if (lobbyService.IsInLobby() && !lobbyService.IsHost())
            {
                NotificationService.Instance.Warning("Time of day is host-only");
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

                int minuteValue = Mathf.Clamp(minuteOfDay, 0, 1439);
                timeManager.SetTimeAndSync(minuteValue);
                int hour = minuteValue / 60;
                int minute = minuteValue % 60;
                NotificationService.Instance.Status($"Time set: {hour:D2}:{minute:D2}");
                Debug.Log($"[Nugzz] Set time to {hour:D2}:{minute:D2}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Nugzz] Failed to set time of day: {ex.Message}");
            }
        }
    }
}
