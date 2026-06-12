using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Police;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Service for interacting with game managers and systems
    /// </summary>
    public sealed class GameManagerService
    {
        private static readonly GameManagerService _instance = new GameManagerService();
        public static GameManagerService Instance => _instance;
        private GameManagerService() { }
        // Player-related methods
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

        public PlayerMovement GetPlayerMovement()
        {
            try
            {
                return PlayerMovement.Instance ?? UnityEngine.Object.FindObjectOfType<PlayerMovement>();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to get player movement: " + ex.Message);
                return null;
            }
        }

        public PlayerInventory GetPlayerInventory()
        {
            try
            {
                return ManagerCacheService.Instance.PlayerInventory;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to get player inventory: " + ex.Message);
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

        // Registry methods
        public ItemDefinition GetItemDefinition(string itemId)
        {
            try
            {
                var registry = ManagerCacheService.Instance.Registry;
                if (registry == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] Registry not found");
                    return null;
                }

                return registry._GetItem(itemId, false);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to get item definition: " + ex.Message);
                return null;
            }
        }

        // Product discovery
        public int DiscoverAllProducts()
        {
            try
            {
                var productManager = ManagerCacheService.Instance.ProductManager;
                if (productManager == null)
                {
                    return 0;
                }

                var allProducts = productManager.AllProducts;
                int count = 0;
                for (int i = 0; i < allProducts.Count; i++)
                {
                    try
                    {
                        productManager.DiscoverProduct(allProducts[i].ID);
                        count++;
                    }
                    catch { }
                }

                return count;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to discover products: " + ex.Message);
                return 0;
            }
        }

        // Drying rack operations removed - not currently used by UI

// Property methods
        public int UnlockAllProperties()
        {
            var lobbyService = LobbyService.Instance;
            if (lobbyService.IsInLobby() && !lobbyService.IsHost())
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Property unlock is host-only in multiplayer");
                return 0;
            }

            try
            {
                var properties = Property.Properties;
                if (properties == null)
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < properties.Count; i++)
                {
                    try
                    {
                        var property = properties[i];
                        if (!property.IsOwned)
                        {
                            property.SetOwned();
                            try { property.ReceiveOwned_Networked(); } catch { }
                            try { property.RecieveOwned(); } catch { }
                            property.IsOwned = true;
                            count++;
                        }
                    }
                    catch { }
                }

                return count;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to unlock properties: " + ex.Message);
                return 0;
            }
        }

        public bool TryUnlockManor(out string message)
        {
            message = "Not found";
            try
            {
                var properties = Property.Properties;
                if (properties == null)
                {
                    message = "No properties found";
                    return false;
                }

                for (int i = 0; i < properties.Count; i++)
                {
                    try
                    {
                        var manor = properties[i].TryCast<Manor>();
                        if (manor != null)
                        {
                            if (manor.IsOwned)
                            {
                                message = "Already owned";
                                return false;
                            }

                            manor.SetOwned();
                            try { manor.ReceiveOwned_Networked(); } catch { }
                            try { manor.RecieveOwned(); } catch { }
                            manor.IsOwned = true;
                            message = "Manor! SAVE+RELOAD";
                            return true;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                message = "Error: " + ex.Message;
                UnityEngine.Debug.LogError("[Nugzz] Failed to unlock manor: " + ex.Message);
            }

            return false;
        }

        // Crime data methods
        public void ClearCrimesAndResetWanted()
        {
            try
            {
                var crimeData = GetPlayerCrimeData();
                if (crimeData != null)
                {
                    crimeData.ClearCrimes();
                    for (int i = 0; i < 5; i++)
                    {
                        crimeData.Deescalate();
                    }
                    crimeData.SetArrestProgress(0f);
                    crimeData.SetBodySearchProgress(0f);
                    try { crimeData.TimeoutPursuit(); } catch { }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to clear crimes: " + ex.Message);
            }
        }

        public void EscalateCrime()
        {
            try
            {
                var crimeData = GetPlayerCrimeData();
                if (crimeData != null)
                {
                    crimeData.Escalate();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to escalate crime: " + ex.Message);
            }
        }

        public void DeescalateCrime()
        {
            try
            {
                var crimeData = GetPlayerCrimeData();
                if (crimeData != null)
                {
                    crimeData.Deescalate();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to deescalate crime: " + ex.Message);
            }
        }

        public int GetDispatchOfficerCount()
        {
            try
            {
                var officers = UnityEngine.Object.FindObjectsOfType<PoliceOfficer>();
                return officers != null ? officers.Length : 0;
            }
            catch { return 0; }
        }

        public void SetDispatchOfficerCount(int count)
        {
            // LawManager.DISPATCH_OFFICER_COUNT is const - cannot be changed at runtime
        }

        public int GetDispatchOfficerCountSettable(int count)
        {
            var lobbyService = LobbyService.Instance;
            if (lobbyService.IsInLobby() && !lobbyService.IsHost())
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Police count is host-only in multiplayer");
                return 0;
            }
            SetDispatchOfficerCount(count);
            return count;
        }

        public void SetMaxHealth()
        {
            try
            {
                var health = GetPlayerHealth();
                if (health != null)
                {
                    health.SetHealth(PlayerHealth.MAX_HEALTH);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to set max health: " + ex.Message);
            }
        }

        public void SetMaxStamina()
        {
            try
            {
                var movement = GetPlayerMovement();
                if (movement != null)
                {
                    movement.CurrentStaminaReserve = PlayerMovement.StaminaReserveMax;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to set max stamina: " + ex.Message);
            }
        }

        public void SetMaxEnergy()
        {
            try
            {
                var player = GetLocalPlayer();
                if (player != null)
                {
                    player.Energy.SetEnergy(100f);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to set max energy: " + ex.Message);
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

        public void SetInfiniteAmmo()
        {
            try
            {
                var player = GetLocalPlayer();
                if (player != null)
                {
                    var inventory = GetPlayerInventory();
                    if (inventory != null)
                    {
                        var equippedItem = inventory.EquippedItem;
                        if (equippedItem != null)
                        {
                            var integerItemInstance = equippedItem.TryCast<Il2CppScheduleOne.ItemFramework.IntegerItemInstance>();
                            if (integerItemInstance != null && integerItemInstance.Value < 90)
                            {
                                integerItemInstance.SetValue(99);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to set infinite ammo: " + ex.Message);
            }
        }
    }
}
