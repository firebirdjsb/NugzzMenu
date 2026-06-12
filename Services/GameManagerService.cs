using System;
using System.Reflection;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;

namespace NugzzMenu.Services
{
    public sealed class GameManagerService
    {
        private static readonly GameManagerService _instance = new GameManagerService();
        public static GameManagerService Instance => _instance;
        private GameManagerService() { }
        public Player GetLocalPlayer()
        {
            try
            {
                return Player.Local;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to get local player: " + ex.Message);
                return null;
            }
        }

        public PlayerHealth GetPlayerHealth()
        {
            try
            {
                var player = GetLocalPlayer();
                return player != null ? player.Health : null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to get player health: " + ex.Message);
                return null;
            }
        }

        public PlayerCrimeData GetPlayerCrimeData()
        {
            try
            {
                var player = GetLocalPlayer();
                return player != null ? player.CrimeData : null;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to get player crime data: " + ex.Message);
                return null;
            }
        }

        public void AddXP(int amount)
        {
            try
            {
                var player = GetLocalPlayer();
                if (player != null)
                {
                    try
                    {
                        var statsField = typeof(Player).GetField("Stats", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        var stats = statsField?.GetValue(player);
                        if (stats != null)
                        {
                            stats.GetType().GetMethod("AddXP")?.Invoke(stats, new object[] { amount, "cheat" });
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("[Nugzz] Player.Stats field is null - XP addition may require host in multiplayer");
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning("[Nugzz] XP addition method not found via reflection: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to add XP: " + ex.Message);
            }
        }
    }
}
