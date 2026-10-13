using System;
using Il2CppScheduleOne.Weather;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class WeatherService
    {
        public static WeatherService Instance { get; } = new WeatherService();

        private const float LightningConfirmationSeconds = 5f;
        private float _lightningConfirmationUntil;

        private WeatherService() { }

        public void SetWeather(string sequenceId)
        {
            if (!CanControlWeather()) return;

            string label;
            switch (sequenceId)
            {
                case "clear": label = "Clear"; break;
                case "foggy": label = "Fog"; break;
                case "lightrain": label = "Light rain"; break;
                case "heavyrain": label = "Storm (heavy rain)"; break;
                default:
                    NotificationService.Instance.Error("Unknown weather preset");
                    return;
            }

            try
            {
                var manager = EnvironmentManager.Instance;
                if (manager == null)
                {
                    NotificationService.Instance.Warning("Weather is unavailable outside gameplay");
                    return;
                }

                var sequences = manager.WeatherSequences;
                // S1API exposes weather discovery; changes still use the native
                // manager because S1API's weather API is read-only.
                bool available = S1ApiService.Instance.GetWeatherSequenceIds().Exists(
                    id => string.Equals(id, sequenceId, StringComparison.OrdinalIgnoreCase));
                if (!available && sequences != null)
                {
                    for (int i = 0; i < sequences.Count; i++)
                    {
                        var sequence = sequences[i];
                        if (sequence != null && string.Equals(sequence.Id, sequenceId, StringComparison.OrdinalIgnoreCase))
                        {
                            available = true;
                            break;
                        }
                    }
                }

                if (!available)
                {
                    NotificationService.Instance.Warning("Weather preset is unavailable in this scene");
                    return;
                }

                manager.SetWeather(sequenceId);
                _lightningConfirmationUntil = 0f;
                NotificationService.Instance.Status("Weather: " + label);
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error("Weather change failed");
                Debug.LogError("[Nugzz] Failed to change weather: " + ex);
            }
        }

        public void TriggerLightning()
        {
            if (!CanControlWeather()) return;

            try
            {
                var manager = EnvironmentManager.Instance;
                if (manager == null)
                {
                    NotificationService.Instance.Warning("Weather is unavailable outside gameplay");
                    return;
                }

                float now = Time.realtimeSinceStartup;
                if (_lightningConfirmationUntil <= now)
                {
                    _lightningConfirmationUntil = now + LightningConfirmationSeconds;
                    NotificationService.Instance.Warning("Select Trigger Lightning again to confirm the strike");
                    return;
                }

                _lightningConfirmationUntil = 0f;
                manager.TriggerLightningEvent();
                NotificationService.Instance.Status("Lightning triggered");
            }
            catch (Exception ex)
            {
                _lightningConfirmationUntil = 0f;
                NotificationService.Instance.Error("Lightning failed");
                Debug.LogError("[Nugzz] Failed to trigger lightning: " + ex);
            }
        }

        private static bool CanControlWeather()
        {
            var lobby = LobbyService.Instance;
            if (!lobby.IsInLobby() || lobby.IsHost()) return true;

            NotificationService.Instance.Warning("Weather controls are host-only");
            return false;
        }
    }
}
