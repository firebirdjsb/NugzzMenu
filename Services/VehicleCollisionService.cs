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
        private bool _initialized;
        private float _nextRefreshTime;

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
        }

        private VehicleCollisionService() { }

        public void Initialize()
        {
            _initialized = true;
            _nextRefreshTime = 0f;
            RefreshAll();
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
            if (!_initialized)
                return;

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + 1f;
                RefreshAll();
            }

            UpdateProxies();
        }

        public void FixedUpdate()
        {
            if (_initialized)
                UpdateProxies();
        }

        public void RefreshAll()
        {
            if (!_initialized)
                return;

            try
            {
                var vehicles = ManagerCacheService.Instance.VehicleManager?.AllVehicles;
                if (vehicles == null)
                    return;

                for (int i = 0; i < vehicles.Count; i++)
                {
                    LandVehicle vehicle = vehicles[i];
                    if (vehicle != null)
                        ApplyVehicle(vehicle);
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Vehicle collision refresh failed", ex);
            }
        }

        public void ApplyVehicle(LandVehicle vehicle)
        {
            if (!_initialized || vehicle == null)
                return;

            try
            {
                VehicleProxy proxy = EnsureProxy(vehicle);
                if (proxy == null)
                    return;

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
                        ConfigurePlayerAgainstVehicle(
                            player, vehicle, proxy, vehicle.GetComponentsInChildren<Collider>(true));
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Player collision setup failed", ex);
            }
        }

        private VehicleProxy EnsureProxy(LandVehicle vehicle)
        {
            int key = vehicle.GetInstanceID();
            if (_proxies.TryGetValue(key, out VehicleProxy existing) &&
                existing?.Object != null && existing.Collider != null)
                return existing;

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
                return null;
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

        private void UpdateProxies()
        {
            var dead = new List<int>();
            foreach (KeyValuePair<int, VehicleProxy> entry in _proxies)
            {
                VehicleProxy proxy = entry.Value;
                if (proxy?.Vehicle == null || proxy.Source == null || proxy.Object == null)
                {
                    dead.Add(entry.Key);
                    continue;
                }

                SyncProxy(proxy);
                UpdatePlayerProxyPairs(proxy);
            }

            for (int i = 0; i < dead.Count; i++)
            {
                if (_proxies.TryGetValue(dead[i], out VehicleProxy proxy))
                {
                    try
                    {
                        if (proxy?.Object != null)
                            UnityEngine.Object.Destroy(proxy.Object);
                    }
                    catch { }
                }
                _proxies.Remove(dead[i]);
            }
        }

        private static void SyncProxy(VehicleProxy proxy)
        {
            Transform source = proxy.Source.transform;
            Transform target = proxy.Object.transform;
            target.position = source.position;
            target.rotation = source.rotation;
            target.localScale = source.lossyScale;
            proxy.Object.layer = proxy.Source.gameObject.layer;
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

                    IgnorePair(capsule, vehicleCollider);
                    if (controller != capsule)
                        IgnorePair(controller, vehicleCollider);
                }
            }

            UpdatePlayerProxyPairs(proxy);
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

        private static void SetProxyCollision(Collider playerCollider, Collider proxyCollider, bool collide)
        {
            if (playerCollider == null || proxyCollider == null)
                return;
            try { Physics.IgnoreCollision(playerCollider, proxyCollider, !collide); } catch { }
        }

        private void IgnorePair(Collider playerCollider, Collider vehicleCollider)
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
                VehicleCollider = vehicleCollider
            };
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
