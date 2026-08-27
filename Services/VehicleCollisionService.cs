using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Blocks players with detached kinematic vehicle shells. The real vehicle colliders are
    /// ignored only for players, so player movement cannot add force or weight to the car.
    /// </summary>
    public sealed class VehicleCollisionService
    {
        private static readonly VehicleCollisionService _instance = new VehicleCollisionService();
        public static VehicleCollisionService Instance => _instance;

        private readonly Dictionary<int, VehicleProxy> _proxies = new Dictionary<int, VehicleProxy>();
        private readonly Dictionary<long, CollisionPair> _ignoredPairs = new Dictionary<long, CollisionPair>();
        private readonly Dictionary<long, bool> _proxyPairStates = new Dictionary<long, bool>();
        private readonly HashSet<int> _seenVehicleKeys = new HashSet<int>();
        private readonly List<int> _deadProxyKeys = new List<int>();
        private readonly List<long> _deadPairKeys = new List<long>();
        private bool _initialized;
        private float _readyTime;
        private float _nextRefreshTime;
        private float _nextStateCheckTime;

        private sealed class VehicleProxy
        {
            public LandVehicle Vehicle;
            public BoxCollider Source;
            public GameObject Object;
            public BoxCollider Collider;
        }

        private sealed class CollisionPair
        {
            public Collider PlayerCollider;
            public Collider VehicleCollider;
            public int VehicleKey;
        }

        private VehicleCollisionService() { }

        public void Initialize()
        {
            _initialized = true;
            _readyTime = Time.unscaledTime + 1f;
            _nextRefreshTime = _readyTime;
            _nextStateCheckTime = _readyTime;
        }

        public void Reset()
        {
            _initialized = false;

            foreach (CollisionPair pair in _ignoredPairs.Values)
            {
                try
                {
                    if (pair?.PlayerCollider != null && pair.VehicleCollider != null)
                        Physics.IgnoreCollision(pair.PlayerCollider, pair.VehicleCollider, false);
                }
                catch { }
            }
            _ignoredPairs.Clear();
            _proxyPairStates.Clear();
            _seenVehicleKeys.Clear();
            _deadProxyKeys.Clear();
            _deadPairKeys.Clear();

            foreach (VehicleProxy proxy in _proxies.Values)
            {
                try
                {
                    if (proxy?.Object != null)
                        UnityEngine.Object.Destroy(proxy.Object);
                }
                catch { }
            }
            _proxies.Clear();
        }

        public void Update()
        {
            if (!_initialized || Time.unscaledTime < _readyTime)
                return;

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + 2f;
                RefreshAll();
            }

            if (Time.unscaledTime >= _nextStateCheckTime)
            {
                _nextStateCheckTime = Time.unscaledTime + 0.25f;
                RefreshProxyStates();
            }
        }

        public void FixedUpdate()
        {
            if (_initialized)
                SyncProxies();
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

                        _seenVehicleKeys.Add(vehicle.GetInstanceID());
                        ApplyVehicle(vehicle);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Vehicle collision refresh failed", ex);
            }

            _deadProxyKeys.Clear();
            foreach (KeyValuePair<int, VehicleProxy> entry in _proxies)
            {
                if (!_seenVehicleKeys.Contains(entry.Key))
                    _deadProxyKeys.Add(entry.Key);
            }

            for (int i = 0; i < _deadProxyKeys.Count; i++)
                RemoveProxy(_deadProxyKeys[i]);
        }

        public void ApplyVehicle(LandVehicle vehicle)
        {
            if (!_initialized || vehicle == null)
                return;

            try
            {
                int vehicleKey = vehicle.GetInstanceID();
                VehicleProxy proxy = EnsureProxy(vehicle);
                if (proxy == null)
                {
                    RestoreVehiclePairs(vehicleKey);
                    return;
                }

                SyncProxy(proxy);
                proxy.Collider.enabled = true;

                Collider[] vehicleColliders = vehicle.GetComponentsInChildren<Collider>(true);
                IgnoreProxyAgainstVehicle(proxy.Collider, vehicleColliders);

                var players = Player.PlayerList;
                if (players == null)
                    return;

                for (int i = 0; i < players.Count; i++)
                {
                    Player player = players[i];
                    if (player != null)
                        ConfigurePlayerAgainstVehicle(player, vehicle, proxy, vehicleColliders);
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Vehicle collision setup failed", ex);
            }
        }

        public void ApplyPlayer(Player player)
        {
            if (!_initialized || player == null)
                return;

            try
            {
                var vehicles = ManagerCacheService.Instance.VehicleManager?.AllVehicles;
                if (vehicles == null)
                    return;

                for (int i = 0; i < vehicles.Count; i++)
                {
                    LandVehicle vehicle = vehicles[i];
                    if (vehicle == null)
                        continue;

                    VehicleProxy proxy = EnsureProxy(vehicle);
                    if (proxy != null)
                    {
                        Collider[] vehicleColliders = vehicle.GetComponentsInChildren<Collider>(true);
                        ConfigurePlayerAgainstVehicle(
                            player, vehicle, proxy, vehicleColliders);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Player collision setup failed", ex);
            }
        }

        private VehicleProxy EnsureProxy(LandVehicle vehicle)
        {
            if (vehicle == null || Time.unscaledTime < _readyTime)
                return null;

            int key = vehicle.GetInstanceID();
            if (!IsVehicleUsable(vehicle))
            {
                RemoveProxy(key);
                return null;
            }

            BoxCollider source = vehicle.boundingBox;
            if (source == null)
            {
                BoxCollider[] boxes = vehicle.GetComponentsInChildren<BoxCollider>(true);
                if (boxes != null && boxes.Length > 0)
                    source = boxes[0];
            }
            if (source == null)
            {
                DebugLogService.Instance.VerboseWarning("No vehicle bounding box available for " + vehicle.name);
                RemoveProxy(key);
                return null;
            }

            if (_proxies.TryGetValue(key, out VehicleProxy existing) && existing != null)
            {
                if (existing.Vehicle == vehicle && existing.Source == source && existing.Object != null &&
                    existing.Collider != null)
                {
                    return existing;
                }

                RemoveProxy(key);
            }

            var proxyObject = new GameObject("Nugzz_VehiclePlayerBlocker_" + key);
            proxyObject.layer = source.gameObject.layer;
            var rigidbody = proxyObject.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.detectCollisions = true;

            var proxyCollider = proxyObject.AddComponent<BoxCollider>();
            proxyCollider.center = source.center;
            proxyCollider.size = source.size;
            proxyCollider.isTrigger = false;
            proxyCollider.enabled = true;

            var proxy = new VehicleProxy
            {
                Vehicle = vehicle,
                Source = source,
                Object = proxyObject,
                Collider = proxyCollider
            };
            _proxies[key] = proxy;
            SyncProxy(proxy);
            DebugLogService.Instance.Verbose("Created force-isolated vehicle blocker for " + vehicle.name);
            return proxy;
        }

        private void RefreshProxyStates()
        {
            _deadProxyKeys.Clear();
            foreach (KeyValuePair<int, VehicleProxy> entry in _proxies)
            {
                VehicleProxy proxy = entry.Value;
                if (!IsProxyUsable(proxy))
                {
                    _deadProxyKeys.Add(entry.Key);
                    continue;
                }

                proxy.Collider.enabled = true;
                SyncProxy(proxy);
                UpdatePlayerProxyPairs(proxy);
            }

            for (int i = 0; i < _deadProxyKeys.Count; i++)
                RemoveProxy(_deadProxyKeys[i]);
        }

        private static bool IsVehicleUsable(LandVehicle vehicle)
        {
            if (vehicle == null || vehicle.gameObject == null || !vehicle.gameObject.activeInHierarchy)
                return false;

            try
            {
                if (!vehicle.IsVisible)
                    return false;
            }
            catch { }

            return true;
        }

        private static bool IsProxyUsable(VehicleProxy proxy)
        {
            return proxy != null && IsVehicleUsable(proxy.Vehicle) && proxy.Source != null &&
                   proxy.Source.gameObject != null && proxy.Source.gameObject.activeInHierarchy &&
                   proxy.Object != null && proxy.Collider != null;
        }

        private void SyncProxies()
        {
            foreach (VehicleProxy proxy in _proxies.Values)
            {
                if (IsProxyUsable(proxy) && proxy.Collider.enabled)
                    SyncProxy(proxy);
            }
        }

        private static void SyncProxy(VehicleProxy proxy)
        {
            Transform source = proxy.Source.transform;
            Transform target = proxy.Object.transform;
            Vector3 position = source.position;
            Quaternion rotation = source.rotation;
            Vector3 scale = source.lossyScale;

            if ((target.position - position).sqrMagnitude > 0.000001f)
                target.position = position;
            if (1f - Mathf.Abs(Quaternion.Dot(target.rotation, rotation)) > 0.000001f)
                target.rotation = rotation;
            if ((target.localScale - scale).sqrMagnitude > 0.000001f)
                target.localScale = scale;

            int layer = proxy.Source.gameObject.layer;
            if (proxy.Object.layer != layer)
                proxy.Object.layer = layer;
        }

        private void UpdatePlayerProxyPairs(VehicleProxy proxy)
        {
            var players = Player.PlayerList;
            if (players == null)
                return;

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null)
                    continue;

                UpdatePlayerProxyPair(player, proxy);
            }
        }

        private void ConfigurePlayerAgainstVehicle(
            Player player,
            LandVehicle vehicle,
            VehicleProxy proxy,
            Collider[] vehicleColliders)
        {
            Collider capsule = player.CapCol;
            Collider controller = player.CharacterController;

            if (vehicleColliders != null)
            {
                for (int i = 0; i < vehicleColliders.Length; i++)
                {
                    Collider vehicleCollider = vehicleColliders[i];
                    if (vehicleCollider == null || vehicleCollider.isTrigger ||
                        vehicleCollider == proxy.Collider)
                        continue;

                    IgnorePair(capsule, vehicleCollider, vehicle.GetInstanceID());
                    if (controller != capsule)
                        IgnorePair(controller, vehicleCollider, vehicle.GetInstanceID());
                }
            }

            UpdatePlayerProxyPair(player, proxy);
        }

        private static void IgnoreProxyAgainstVehicle(Collider proxy, Collider[] vehicleColliders)
        {
            if (proxy == null || vehicleColliders == null)
                return;

            for (int i = 0; i < vehicleColliders.Length; i++)
            {
                Collider vehicleCollider = vehicleColliders[i];
                if (vehicleCollider == null || vehicleCollider == proxy)
                    continue;
                try { Physics.IgnoreCollision(proxy, vehicleCollider, true); } catch { }
            }
        }

        private void UpdatePlayerProxyPair(Player player, VehicleProxy proxy)
        {
            bool seatedInVehicle = false;
            try
            {
                seatedInVehicle = player.IsInVehicle &&
                    player.CurrentVehicle != null &&
                    proxy.Vehicle.NetworkObject != null &&
                    player.CurrentVehicle == proxy.Vehicle.NetworkObject;
            }
            catch { }

            SetProxyCollision(player.CapCol, proxy.Collider, !seatedInVehicle);
            if (player.CharacterController != player.CapCol)
                SetProxyCollision(player.CharacterController, proxy.Collider, !seatedInVehicle);
        }

        private void SetProxyCollision(Collider playerCollider, Collider proxyCollider, bool collide)
        {
            if (playerCollider == null || proxyCollider == null)
                return;

            long key = ((long)(uint)playerCollider.GetInstanceID() << 32) |
                (uint)proxyCollider.GetInstanceID();
            if (_proxyPairStates.TryGetValue(key, out bool previous) && previous == collide)
                return;

            try { Physics.IgnoreCollision(playerCollider, proxyCollider, !collide); } catch { }
            _proxyPairStates[key] = collide;
        }

        private void IgnorePair(Collider playerCollider, Collider vehicleCollider, int vehicleKey)
        {
            if (playerCollider == null || vehicleCollider == null)
                return;

            long key = ((long)(uint)playerCollider.GetInstanceID() << 32) |
                (uint)vehicleCollider.GetInstanceID();
            if (_ignoredPairs.ContainsKey(key))
                return;

            Physics.IgnoreCollision(playerCollider, vehicleCollider, true);
            _ignoredPairs[key] = new CollisionPair
            {
                PlayerCollider = playerCollider,
                VehicleCollider = vehicleCollider,
                VehicleKey = vehicleKey
            };
        }

        private void RestoreVehiclePairs(int vehicleKey)
        {
            _deadPairKeys.Clear();
            foreach (KeyValuePair<long, CollisionPair> entry in _ignoredPairs)
            {
                CollisionPair pair = entry.Value;
                if (pair == null || pair.VehicleKey != vehicleKey)
                    continue;

                try
                {
                    if (pair.PlayerCollider != null && pair.VehicleCollider != null)
                        Physics.IgnoreCollision(pair.PlayerCollider, pair.VehicleCollider, false);
                }
                catch { }

                _deadPairKeys.Add(entry.Key);
            }

            for (int i = 0; i < _deadPairKeys.Count; i++)
                _ignoredPairs.Remove(_deadPairKeys[i]);
        }

        private void RemoveProxy(int vehicleKey)
        {
            RestoreVehiclePairs(vehicleKey);

            if (_proxies.TryGetValue(vehicleKey, out VehicleProxy proxy) && proxy?.Object != null)
            {
                try { UnityEngine.Object.Destroy(proxy.Object); } catch { }
            }

            _proxies.Remove(vehicleKey);
            _proxyPairStates.Clear();
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
