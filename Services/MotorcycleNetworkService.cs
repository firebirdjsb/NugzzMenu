using System;
using System.Collections.Generic;
using System.Globalization;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal sealed class MotorcycleNetworkService
    {
        internal static readonly MotorcycleNetworkService Instance = new();
        internal const string State = "Nugzz.Motorcycle.State";
        private sealed class HostBike
        {
            internal LandVehicle Vehicle;
            internal readonly HashSet<int> Informed = new();
        }
        private readonly List<HostBike> _host = new();
        private readonly Dictionary<int, float> _pending = new();
        private readonly List<int> _expired = new();
        private float _nextUpdate;
        internal void Announce(LandVehicle vehicle)
        {
            if (vehicle == null || !LocalAnimationNetworkService.IsLocalAuthority) return;
            _host.Add(new HostBike { Vehicle = vehicle }); _nextUpdate = 0f;
        }
        internal void Remove(LandVehicle vehicle)
        {
            if (!LocalAnimationNetworkService.IsLocalAuthority || vehicle == null) return;
            int id = ObjectId(vehicle);
            if (id >= 0) PlayerValueRpcService.BroadcastToApprovedClients(Player.Local, State, id.ToString(CultureInfo.InvariantCulture) + "|0");
            _host.RemoveAll(b => b.Vehicle == vehicle);
        }
        internal static bool TryParse(string value, out int id, out bool present)
        {
            id = -1; present = false;
            if (value == null || value.Length > 13) return false;
            string[] parts = value.Split('|');
            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out id) || id < 0 ||
                (parts[1] != "0" && parts[1] != "1")) return false;
            present = parts[1] == "1"; return true;
        }
        internal bool TryReceiveNetworkValue(Player source, string name, string value)
        {
            if (!string.Equals(name, State, StringComparison.Ordinal)) return false;
            if (LocalAnimationNetworkService.IsLocalAuthority || !SessionAuthorityService.IsHostPlayer(source) ||
                !SessionAuthorityService.Instance.FeaturesAllowed || !LocalAnimationNetworkService.Instance.CanSynchronizeBody ||
                !TryParse(value, out int id, out bool present)) return true;
            if (!present)
            {
                _pending.Remove(id);
                LandVehicle vehicle = Find(id);
                if (vehicle != null) MotorcycleService.Instance.Detach(vehicle);
            }
            else if (!_pending.ContainsKey(id) && _pending.Count < MotorcycleService.Limit)
                _pending[id] = Time.unscaledTime + 15f;
            return true;
        }
        internal void Update()
        {
            if (Time.unscaledTime < _nextUpdate) return;
            _nextUpdate = Time.unscaledTime + 0.5f;
            if (LocalAnimationNetworkService.IsLocalAuthority)
            {
                _host.RemoveAll(b => b.Vehicle == null || !MotorcycleService.Instance.IsMotorcycle(b.Vehicle));
                foreach (HostBike bike in _host)
                {
                    int id = ObjectId(bike.Vehicle); if (id < 0) continue;
                    foreach (Player peer in Player.PlayerList)
                    {
                        if (peer == null || peer.IsLocalPlayer || peer.Owner == null) continue;
                        int client = peer.Owner.ClientId;
                        if (!LocalAnimationNetworkService.Instance.MatchingPeer(peer)) { bike.Informed.Remove(client); continue; }
                        if (bike.Informed.Contains(client)) continue;
                        if (PlayerValueRpcService.SendToClient(Player.Local, peer, State, id.ToString(CultureInfo.InvariantCulture) + "|1")) bike.Informed.Add(client);
                    }
                }
            }
            _expired.Clear();
            foreach (var pending in _pending)
            {
                if (Time.unscaledTime >= pending.Value || !SessionAuthorityService.Instance.FeaturesAllowed) { _expired.Add(pending.Key); continue; }
                LandVehicle vehicle = Find(pending.Key);
                if (vehicle != null && MotorcycleService.Instance.Attach(vehicle)) _expired.Add(pending.Key);
            }
            foreach (int id in _expired) _pending.Remove(id);
        }
        private static int ObjectId(LandVehicle vehicle)
        { try { return vehicle?.NetworkObject?.ObjectId ?? -1; } catch { return -1; } }
        private static LandVehicle Find(int id)
        {
            VehicleManager manager = UnityEngine.Object.FindObjectOfType<VehicleManager>();
            if (manager?.AllVehicles != null) foreach (LandVehicle vehicle in manager.AllVehicles)
                if (vehicle != null && ObjectId(vehicle) == id) return vehicle;
            return null;
        }
        internal void Reset() { _host.Clear(); _pending.Clear(); _expired.Clear(); _nextUpdate = 0f; }
    }
}
