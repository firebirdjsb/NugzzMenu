using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Prevents player physics capsules from adding force to vehicles while retaining
    /// character-controller blocking against each client's networked vehicle colliders.
    /// </summary>
    public sealed class VehicleCollisionService
    {
        private static readonly VehicleCollisionService _instance = new VehicleCollisionService();
        public static VehicleCollisionService Instance => _instance;

        private readonly Dictionary<int, VehicleEntry> _vehicles =
            new Dictionary<int, VehicleEntry>();
        private readonly Dictionary<long, CollisionPair> _pairs =
            new Dictionary<long, CollisionPair>();
        private readonly Dictionary<int, int> _playerVehicleStates =
            new Dictionary<int, int>();
        private readonly HashSet<int> _seenVehicleKeys = new HashSet<int>();
        private readonly HashSet<int> _seenPlayerKeys = new HashSet<int>();
        private readonly List<int> _deadVehicleKeys = new List<int>();
        private readonly List<int> _deadPlayerKeys = new List<int>();
        private readonly List<long> _deadPairKeys = new List<long>();
        private readonly List<LandVehicle> _parkedSyncQueue = new List<LandVehicle>();

        private bool _initialized;
        private float _readyTime;
        private float _nextRefreshTime;
        private float _nextOccupancyCheckTime;
        private float _nextParkedSyncTime;
        private int _parkedSyncIndex;

        private sealed class VehicleEntry
        {
            public LandVehicle Vehicle;
            public Collider[] Colliders;
            public int NetworkObjectId;
        }

        private sealed class CollisionPair
        {
            public Player Player;
            public Collider PlayerCollider;
            public Collider VehicleCollider;
            public int PlayerKey;
            public int VehicleKey;
            public int VehicleNetworkObjectId;
            public bool FollowsOccupancy;
            public bool Ignored;
        }

        private VehicleCollisionService() { }

        public void Initialize()
        {
            _initialized = true;
            _readyTime = Time.unscaledTime + 1f;
            _nextRefreshTime = _readyTime;
            _nextOccupancyCheckTime = _readyTime;
            _nextParkedSyncTime = _readyTime + 1f;
        }

        public void Reset()
        {
            _initialized = false;

            foreach (CollisionPair pair in _pairs.Values)
                RestorePair(pair);

            _pairs.Clear();
            _vehicles.Clear();
            _playerVehicleStates.Clear();
            _seenVehicleKeys.Clear();
            _seenPlayerKeys.Clear();
            _deadVehicleKeys.Clear();
            _deadPlayerKeys.Clear();
            _deadPairKeys.Clear();
            _parkedSyncQueue.Clear();
            _parkedSyncIndex = 0;
        }

        public void Update()
        {
            if (!_initialized || Time.unscaledTime < _readyTime)
                return;

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + 5f;
                RefreshAll();
            }

            if (Time.unscaledTime >= _nextOccupancyCheckTime)
            {
                _nextOccupancyCheckTime = Time.unscaledTime + 0.2f;
                RefreshOccupancyStates();
            }

            UpdateParkedVehicleSync();
        }

        public void RefreshAll()
        {
            if (!_initialized)
                return;

            _seenVehicleKeys.Clear();
            try
            {
                var vehicles = ManagerCacheService.Instance.VehicleManager?.AllVehicles;
                if (vehicles != null)
                {
                    for (int i = 0; i < vehicles.Count; i++)
                    {
                        LandVehicle vehicle = vehicles[i];
                        if (vehicle == null)
                            continue;

                        int key = vehicle.GetInstanceID();
                        _seenVehicleKeys.Add(key);
                        ApplyVehicle(vehicle);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Vehicle collision refresh failed", ex);
            }

            _deadVehicleKeys.Clear();
            foreach (KeyValuePair<int, VehicleEntry> entry in _vehicles)
            {
                if (!_seenVehicleKeys.Contains(entry.Key) || !IsVehicleUsable(entry.Value?.Vehicle))
                    _deadVehicleKeys.Add(entry.Key);
            }

            for (int i = 0; i < _deadVehicleKeys.Count; i++)
                RemoveVehicle(_deadVehicleKeys[i]);

            RemoveDeadPairs();
        }

        public void ApplyVehicle(LandVehicle vehicle)
        {
            if (!_initialized || !IsVehicleUsable(vehicle) || Time.unscaledTime < _readyTime)
                return;

            try
            {
                int vehicleKey = vehicle.GetInstanceID();
                if (!_vehicles.TryGetValue(vehicleKey, out VehicleEntry entry) ||
                    entry == null || entry.Vehicle != vehicle || entry.Colliders == null)
                {
                    entry = new VehicleEntry
                    {
                        Vehicle = vehicle,
                        Colliders = vehicle.GetComponentsInChildren<Collider>(true),
                        NetworkObjectId = GetVehicleNetworkObjectId(vehicle)
                    };
                    _vehicles[vehicleKey] = entry;
                }

                var players = Player.PlayerList;
                if (players == null)
                    return;

                for (int i = 0; i < players.Count; i++)
                {
                    Player player = players[i];
                    if (player != null)
                        ConfigurePlayerAgainstVehicle(player, vehicleKey, entry);
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Vehicle collision setup failed", ex);
            }
        }

        public void ApplyPlayer(Player player)
        {
            if (!_initialized || player == null || Time.unscaledTime < _readyTime)
                return;

            try
            {
                foreach (KeyValuePair<int, VehicleEntry> entry in _vehicles)
                    ConfigurePlayerAgainstVehicle(player, entry.Key, entry.Value);

                _playerVehicleStates[player.GetInstanceID()] = GetCurrentVehicleObjectId(player);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Player collision setup failed", ex);
            }
        }

        private void ConfigurePlayerAgainstVehicle(
            Player player, int vehicleKey, VehicleEntry entry)
        {
            if (player == null || entry?.Vehicle == null || entry.Colliders == null)
                return;

            Collider capsule = player.CapCol;
            Collider controller = player.CharacterController;
            int playerKey = player.GetInstanceID();
            int currentVehicleId = GetCurrentVehicleObjectId(player);
            bool seatedInThisVehicle = currentVehicleId >= 0 &&
                currentVehicleId == entry.NetworkObjectId;

            for (int i = 0; i < entry.Colliders.Length; i++)
            {
                Collider vehicleCollider = entry.Colliders[i];
                if (vehicleCollider == null || vehicleCollider.isTrigger)
                    continue;

                SetPair(player, capsule, vehicleCollider, playerKey, vehicleKey,
                    entry.NetworkObjectId, false, true);

                if (controller != null && controller != capsule)
                {
                    SetPair(player, controller, vehicleCollider, playerKey, vehicleKey,
                        entry.NetworkObjectId, true, seatedInThisVehicle);
                }
            }
        }

        private void RefreshOccupancyStates()
        {
            var players = Player.PlayerList;
            if (players == null)
                return;

            _seenPlayerKeys.Clear();
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null)
                    continue;

                int playerKey = player.GetInstanceID();
                int currentVehicleId = GetCurrentVehicleObjectId(player);
                _seenPlayerKeys.Add(playerKey);
                if (_playerVehicleStates.TryGetValue(playerKey, out int previousVehicleId) &&
                    previousVehicleId == currentVehicleId)
                {
                    continue;
                }

                _playerVehicleStates[playerKey] = currentVehicleId;
                foreach (CollisionPair pair in _pairs.Values)
                {
                    if (pair == null || !pair.FollowsOccupancy || pair.PlayerKey != playerKey)
                        continue;
                    SetIgnored(pair, currentVehicleId >= 0 &&
                        currentVehicleId == pair.VehicleNetworkObjectId);
                }
            }

            _deadPlayerKeys.Clear();
            foreach (int playerKey in _playerVehicleStates.Keys)
            {
                if (!_seenPlayerKeys.Contains(playerKey))
                    _deadPlayerKeys.Add(playerKey);
            }
            for (int i = 0; i < _deadPlayerKeys.Count; i++)
                _playerVehicleStates.Remove(_deadPlayerKeys[i]);
        }

        private void UpdateParkedVehicleSync()
        {
            if (!LobbyService.Instance.IsHost())
            {
                _parkedSyncQueue.Clear();
                _parkedSyncIndex = 0;
                return;
            }

            if (Time.unscaledTime >= _nextParkedSyncTime)
            {
                _nextParkedSyncTime = Time.unscaledTime + 5f;
                _parkedSyncQueue.Clear();
                _parkedSyncIndex = 0;
                foreach (VehicleEntry entry in _vehicles.Values)
                {
                    if (IsStableParkedVehicle(entry?.Vehicle))
                        _parkedSyncQueue.Add(entry.Vehicle);
                }
            }

            if (_parkedSyncIndex >= _parkedSyncQueue.Count)
                return;

            LandVehicle vehicle = _parkedSyncQueue[_parkedSyncIndex++];
            if (!IsStableParkedVehicle(vehicle))
                return;

            try
            {
                vehicle.SetTransform_Server(vehicle.transform.position, vehicle.transform.rotation);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Parked vehicle transform sync failed: " + ex.Message);
            }
        }

        private void SetPair(
            Player player,
            Collider playerCollider,
            Collider vehicleCollider,
            int playerKey,
            int vehicleKey,
            int vehicleNetworkObjectId,
            bool followsOccupancy,
            bool ignored)
        {
            if (playerCollider == null || vehicleCollider == null)
                return;

            long key = PairKey(playerCollider, vehicleCollider);
            if (!_pairs.TryGetValue(key, out CollisionPair pair) || pair == null)
            {
                pair = new CollisionPair
                {
                    Player = player,
                    PlayerCollider = playerCollider,
                    VehicleCollider = vehicleCollider,
                    PlayerKey = playerKey,
                    VehicleKey = vehicleKey,
                    VehicleNetworkObjectId = vehicleNetworkObjectId,
                    FollowsOccupancy = followsOccupancy,
                    Ignored = !ignored
                };
                _pairs[key] = pair;
            }

            SetIgnored(pair, ignored);
        }

        private static void SetIgnored(CollisionPair pair, bool ignored)
        {
            if (pair == null || pair.Ignored == ignored || pair.PlayerCollider == null ||
                pair.VehicleCollider == null)
            {
                return;
            }

            try
            {
                Physics.IgnoreCollision(pair.PlayerCollider, pair.VehicleCollider, ignored);
                pair.Ignored = ignored;
            }
            catch { }
        }

        private void RemoveDeadPairs()
        {
            _deadPairKeys.Clear();
            foreach (KeyValuePair<long, CollisionPair> entry in _pairs)
            {
                CollisionPair pair = entry.Value;
                if (pair == null || pair.Player == null || pair.PlayerCollider == null ||
                    pair.VehicleCollider == null)
                {
                    RestorePair(pair);
                    _deadPairKeys.Add(entry.Key);
                }
            }

            for (int i = 0; i < _deadPairKeys.Count; i++)
                _pairs.Remove(_deadPairKeys[i]);
        }

        private void RemoveVehicle(int vehicleKey)
        {
            _deadPairKeys.Clear();
            foreach (KeyValuePair<long, CollisionPair> entry in _pairs)
            {
                CollisionPair pair = entry.Value;
                if (pair == null || pair.VehicleKey != vehicleKey)
                    continue;

                RestorePair(pair);
                _deadPairKeys.Add(entry.Key);
            }

            for (int i = 0; i < _deadPairKeys.Count; i++)
                _pairs.Remove(_deadPairKeys[i]);
            _vehicles.Remove(vehicleKey);
        }

        private static void RestorePair(CollisionPair pair)
        {
            if (pair == null || !pair.Ignored || pair.PlayerCollider == null ||
                pair.VehicleCollider == null)
            {
                return;
            }

            try
            {
                Physics.IgnoreCollision(pair.PlayerCollider, pair.VehicleCollider, false);
                pair.Ignored = false;
            }
            catch { }
        }

        private static bool IsVehicleUsable(LandVehicle vehicle)
        {
            return vehicle != null && vehicle.gameObject != null &&
                vehicle.gameObject.activeInHierarchy;
        }

        private static bool IsStableParkedVehicle(LandVehicle vehicle)
        {
            if (!IsVehicleUsable(vehicle))
                return false;

            try
            {
                if (vehicle.IsOccupied)
                    return false;

                Rigidbody body = vehicle.Rb;
                return body == null || body.IsSleeping() ||
                    (body.velocity.sqrMagnitude <= 0.25f &&
                     body.angularVelocity.sqrMagnitude <= 0.25f);
            }
            catch { return false; }
        }

        private static int GetVehicleNetworkObjectId(LandVehicle vehicle)
        {
            try { return vehicle?.NetworkObject?.GetInstanceID() ?? -1; }
            catch { return -1; }
        }

        private static int GetCurrentVehicleObjectId(Player player)
        {
            try
            {
                if (player == null || !player.IsInVehicle || player.CurrentVehicle == null)
                    return -1;
                return player.CurrentVehicle.GetInstanceID();
            }
            catch { return -1; }
        }

        private static long PairKey(Collider playerCollider, Collider vehicleCollider)
        {
            return ((long)(uint)playerCollider.GetInstanceID() << 32) |
                (uint)vehicleCollider.GetInstanceID();
        }
    }

    [HarmonyPatch(typeof(LandVehicle), nameof(LandVehicle.Awake))]
    internal static class LandVehicleCollisionPatch
    {
        private static void Postfix(LandVehicle __instance)
        {
            VehicleCollisionService.Instance.ApplyVehicle(__instance);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    internal static class PlayerVehicleCollisionPatch
    {
        private static void Postfix(Player __instance)
        {
            VehicleCollisionService.Instance.ApplyPlayer(__instance);
        }
    }
}
