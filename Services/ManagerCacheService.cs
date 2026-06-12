using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Il2CppScheduleOne;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Vehicles;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Centralizes ALL Unity manager lookups with caching, lazy loading, and IL2CPP safety.
    /// Never call FindObjectOfType outside this service.
    /// </summary>
    public sealed class ManagerCacheService
    {
        private static readonly ManagerCacheService _instance = new ManagerCacheService();
        public static ManagerCacheService Instance => _instance;

        private TimeManager _timeManager;
        private MoneyManager _moneyManager;
        private VehicleManager _vehicleManager;
        private ProductManager _productManager;
        private NPCManager _npcManager;
        private Registry _registry;
        private Player _localPlayer;
        private PlayerInventory _playerInventory;
        private Lobby _lobby;

        private ManagerCacheService() { }

        /// <summary>
        /// Marks all cached references as dirty, forcing re-lookup on next access.
        /// Call this after scene changes.
        /// </summary>
        public void Invalidate()
        {
            _timeManager = null;
            _moneyManager = null;
            _vehicleManager = null;
            _productManager = null;
            _npcManager = null;
            _registry = null;
            _localPlayer = null;
            _playerInventory = null;
            _lobby = null;
        }

        private T SafeFind<T>() where T : UnityEngine.Object
        {
            var obj = UnityEngine.Object.FindObjectOfType<T>();
            if (obj != null && obj.Pointer != IntPtr.Zero)
                return obj;
            return null;
        }

        public TimeManager TimeManager
        {
            get
            {
                if (_timeManager == null)
                    _timeManager = SafeFind<TimeManager>();
                return _timeManager;
            }
        }

        public MoneyManager MoneyManager
        {
            get
            {
                if (_moneyManager == null)
                    _moneyManager = SafeFind<MoneyManager>();
                return _moneyManager;
            }
        }

        public VehicleManager VehicleManager
        {
            get
            {
                if (_vehicleManager == null)
                    _vehicleManager = SafeFind<VehicleManager>();
                return _vehicleManager;
            }
        }

        public ProductManager ProductManager
        {
            get
            {
                if (_productManager == null)
                    _productManager = SafeFind<ProductManager>();
                return _productManager;
            }
        }

        public NPCManager NPCManager
        {
            get
            {
                if (_npcManager == null)
                    _npcManager = SafeFind<NPCManager>();
                return _npcManager;
            }
        }

        public Registry Registry
        {
            get
            {
                if (_registry == null)
                    _registry = SafeFind<Registry>();
                return _registry;
            }
        }

        public Player LocalPlayer
        {
            get
            {
                if (_localPlayer == null)
                    _localPlayer = Player.Local ?? SafeFind<Player>();
                return _localPlayer;
            }
        }

        public PlayerInventory PlayerInventory
        {
            get
            {
                if (_playerInventory == null)
                    _playerInventory = SafeFind<PlayerInventory>();
                return _playerInventory;
            }
        }

        public Lobby Lobby
        {
            get
            {
                if (_lobby == null)
                    _lobby = SafeFind<Lobby>();
                return _lobby;
            }
        }

        public Il2CppSystem.Collections.Generic.List<NPC> NPCRegistry => NPCManager.NPCRegistry;
    }
}
