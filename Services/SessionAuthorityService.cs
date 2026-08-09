using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HarmonyLib;
using Il2CppFishNet.Connection;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Variables;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Negotiates Nugzz access inside the current lobby. Clients advertise their
    /// build through Player.SendValue; the host explicitly approves matching builds.
    /// Approval is session-only and is cleared when the lobby changes.
    /// </summary>
    public sealed class SessionAuthorityService
    {
        public sealed class ClientStatus
        {
            public bool Detected { get; internal set; }
            public bool VersionMatches { get; internal set; }
            public bool Approved { get; internal set; }
            public bool IsLocal { get; internal set; }
            public string BuildLabel { get; internal set; } = "Not detected";
        }

        private sealed class ClientRecord
        {
            public int ClientId;
            public int BuildToken;
            public bool Approved;
            public float LastSeen;
            public bool HasTransportResult;
            public bool LastTransportQueued;
            public string LastTransportDiagnostic = string.Empty;
        }

        private const string Prefix = "Nugzz.Session.";
        private const string HostHelloVariable = Prefix + "HostHello";
        private const string ClientHelloVariable = Prefix + "ClientHello";
        private const string DecisionVariable = Prefix + "Decision";
        private const string DecisionPrefix = Prefix + "Decision.";
        private const string LegacyAuthorityVariable = "Nugzz.HostAuthority";
        private const string BuildVersion = "0.9.9R4";
        private const float PulseInterval = 1.5f;
        private const float PulseTimeout = 5f;
        private const float AssemblyScanInterval = 3f;
        private const float ClientExpiry = 8f;

        private static readonly SessionAuthorityService _instance = new SessionAuthorityService();
        private static readonly int BuildToken = ComputeBuildToken();
        private readonly Dictionary<int, ClientRecord> _clients = new Dictionary<int, ClientRecord>();

        private bool _initialized;
        private bool _rpModBlocked;
        private string _rpModLocation = string.Empty;
        private ulong _lobbyId;
        private int _hostClientId = -1;
        private bool _hostApproved;
        private float _lastHostMessage = -100f;
        private float _nextPulse;
        private float _nextAssemblyScan;
        private float _nextWaitingDiagnostic;
        private bool? _reportedClientAccess;
        private bool _clientHelloTransportKnown;
        private bool _lastClientHelloQueued;
        private int _clientHelloSends;
        private int _hostHelloReceives;
        private int _decisionReceives;

        public static SessionAuthorityService Instance => _instance;
        public bool FeaturesAllowed { get; private set; } = true;
        public bool IsRpModBlocked => _rpModBlocked;
        public string BlockReason { get; private set; } = string.Empty;

        private SessionAuthorityService() { }

        public void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;
            ScanInstallLocations();
            ScanLoadedAssemblies();
            LogReceiverHookState();
            Update();
        }

        public void Update()
        {
            if (!_initialized)
                Initialize();

            if (!_rpModBlocked && Time.unscaledTime >= _nextAssemblyScan)
            {
                _nextAssemblyScan = Time.unscaledTime + AssemblyScanInterval;
                ScanLoadedAssemblies();
            }

            if (_rpModBlocked)
            {
                Deny("Nugzz is disabled because S.I.A.K - Imperium was detected" +
                    (string.IsNullOrEmpty(_rpModLocation) ? "." : " at " + _rpModLocation + ".") +
                    " Cheating is disabled on roleplay mods.");
                return;
            }

            Lobby lobby = null;
            try { lobby = Lobby.Instance; } catch { }

            bool inLobby = false;
            bool isHost = false;
            ulong lobbyId = 0;
            try
            {
                inLobby = lobby != null && lobby.IsInLobby;
                isHost = inLobby && lobby.IsHost;
                lobbyId = inLobby ? lobby.LobbyID : 0;
            }
            catch { }

            if (lobbyId != _lobbyId)
            {
                ulong previousLobbyId = _lobbyId;
                _lobbyId = lobbyId;
                ResetLobbyState();
                ShapePrefabService.Instance.ResetForScene();
                DebugLogService.Instance.Session("Lobby changed " + previousLobbyId + " -> " +
                    lobbyId + "; role=" + (isHost ? "host" : inLobby ? "client" : "solo") +
                    "; localClientId=" + GetClientId(Player.Local) + "; buildToken=" + BuildToken);
            }

            if (!inLobby)
            {
                Allow();
                return;
            }

            if (isHost)
            {
                Allow();
                BroadcastHostState();
                ExpireClients();
                return;
            }

            BroadcastClientHello();
            if (_hostApproved && Time.unscaledTime - _lastHostMessage <= PulseTimeout)
            {
                Allow();
                ReportClientAccess(true, "host approval received");
                return;
            }

            string reason = Time.unscaledTime - _lastHostMessage > PulseTimeout
                ? "Waiting for this lobby's Nugzz host to respond."
                : "The host has not allowed NugzzMenu access for this player.";
            Deny(reason);
            ReportClientAccess(false, reason);

            if (Time.unscaledTime >= _nextWaitingDiagnostic)
            {
                _nextWaitingDiagnostic = Time.unscaledTime + 10f;
                DebugLogService.Instance.Session("CLIENT waiting: localClientId=" +
                    GetClientId(Player.Local) + "; helloTx=" + _clientHelloSends +
                    "; hostHelloRx=" + _hostHelloReceives + "; decisionRx=" +
                    _decisionReceives + "; hostClientId=" + _hostClientId +
                    "; lastHostMessageAge=" +
                    (Time.unscaledTime - _lastHostMessage).ToString("0.0", CultureInfo.InvariantCulture) +
                    "s; reason=" + reason);
            }
        }

        public ClientStatus GetClientStatus(Player player)
        {
            var status = new ClientStatus { IsLocal = player != null && player.IsLocalPlayer };
            if (player == null)
                return status;

            if (status.IsLocal && IsLocalHost())
            {
                status.Detected = true;
                status.VersionMatches = true;
                status.Approved = true;
                status.BuildLabel = BuildVersion;
                return status;
            }

            int clientId = GetClientId(player);
            if (clientId < 0 || !_clients.TryGetValue(clientId, out ClientRecord record))
                return status;

            status.Detected = Time.unscaledTime - record.LastSeen <= ClientExpiry;
            status.VersionMatches = record.BuildToken == BuildToken;
            status.Approved = record.Approved && status.VersionMatches;
            status.BuildLabel = status.VersionMatches ? BuildVersion : "Different build";
            return status;
        }

        public bool SetClientApproval(Player player, bool approved)
        {
            if (!IsLocalHost() || player == null || player.IsLocalPlayer)
            {
                DebugLogService.Instance.SessionWarning(
                    "HOST approval rejected: caller is not host or selected player is invalid/local");
                return false;
            }

            int clientId = GetClientId(player);
            if (clientId < 0 || !_clients.TryGetValue(clientId, out ClientRecord record) ||
                record.BuildToken != BuildToken)
            {
                DebugLogService.Instance.SessionWarning("HOST approval rejected: player=" +
                    PlayerLabel(player) + "; clientId=" + clientId +
                    "; detected=" + _clients.ContainsKey(clientId));
                return false;
            }

            record.Approved = approved;
            DebugLogService.Instance.Session("HOST approval changed: player=" + PlayerLabel(player) +
                "; clientId=" + clientId + "; approved=" + approved +
                "; tokenMatch=" + (record.BuildToken == BuildToken));
            BroadcastDecision(record, true);
            return true;
        }

        internal bool IsClientApproved(Player player)
        {
            if (player == null)
                return false;
            if (player.IsLocalPlayer)
                return IsLocalHost() || FeaturesAllowed;

            int clientId = GetClientId(player);
            return clientId >= 0 && _clients.TryGetValue(clientId, out ClientRecord record) &&
                record.Approved && record.BuildToken == BuildToken &&
                Time.unscaledTime - record.LastSeen <= ClientExpiry;
        }

        internal bool TryReceiveNetworkValue(Player source, string variableName, string value,
            string receiveHook = "unknown")
        {
            if (!IsNugzzSessionVariable(variableName))
                return false;

            bool knownSessionValue =
                string.Equals(variableName, ClientHelloVariable, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(variableName, HostHelloVariable, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(variableName, DecisionVariable, StringComparison.OrdinalIgnoreCase) ||
                variableName.StartsWith(DecisionPrefix, StringComparison.OrdinalIgnoreCase);
            if (!knownSessionValue)
                return false;

            if (source == null || !int.TryParse(value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int payload))
            {
                DebugLogService.Instance.SessionWarning("RX invalid session value: hook=" + receiveHook +
                    "; source=" + PlayerLabel(source) + "; name=" + variableName +
                    "; value=" + (value ?? "<null>"));
                return true;
            }

            if (string.Equals(variableName, ClientHelloVariable, StringComparison.OrdinalIgnoreCase))
                ReceiveClientHello(source, payload, receiveHook);
            else if (string.Equals(variableName, HostHelloVariable, StringComparison.OrdinalIgnoreCase))
                ReceiveHostHello(source, payload, receiveHook);
            else if (string.Equals(variableName, DecisionVariable, StringComparison.OrdinalIgnoreCase) ||
                variableName.StartsWith(DecisionPrefix, StringComparison.OrdinalIgnoreCase))
                ReceiveDecision(source, variableName, payload, receiveHook);
            return true;
        }

        internal static bool IsHostPlayer(Player player)
        {
            if (player == null)
                return false;
            if (player.IsLocalPlayer)
                return Instance.IsLocalHost();

            int clientId = GetClientId(player);
            return clientId >= 0 && clientId == Instance._hostClientId;
        }

        internal static bool IsNugzzControlVariable(string variableName)
        {
            return IsNugzzSessionVariable(variableName) ||
                string.Equals(variableName, LegacyAuthorityVariable, StringComparison.OrdinalIgnoreCase) ||
                ShapePrefabService.IsNetworkVariable(variableName) ||
                PlayerCheatService.IsNetworkScaleVariable(variableName) ||
                string.Equals(variableName, RelationshipService.RegionUnlockVariable,
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNugzzSessionVariable(string variableName)
        {
            return !string.IsNullOrEmpty(variableName) &&
                variableName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        private void ReceiveClientHello(Player source, int token, string receiveHook)
        {
            if (!IsLocalHost() || source.IsLocalPlayer)
                return;

            int clientId = GetClientId(source);
            if (clientId < 0)
                return;

            bool isNew = !_clients.TryGetValue(clientId, out ClientRecord record);
            if (isNew)
            {
                record = new ClientRecord { ClientId = clientId };
                _clients[clientId] = record;
            }

            bool tokenChanged = record.BuildToken != token;
            if (tokenChanged)
                record.Approved = false;
            record.BuildToken = token;
            record.LastSeen = Time.unscaledTime;

            if (isNew || tokenChanged)
            {
                DebugLogService.Instance.Session("HOST RX ClientHello: hook=" + receiveHook +
                    "; source=" + PlayerLabel(source) + "; clientId=" + clientId +
                    "; token=" + token + "; expected=" + BuildToken +
                    "; match=" + (token == BuildToken));
            }
            else
            {
                DebugLogService.Instance.Verbose("Session HOST RX ClientHello pulse: clientId=" +
                    clientId);
            }
            BroadcastDecision(record, isNew || tokenChanged);
        }

        private void ReceiveHostHello(Player source, int token, string receiveHook)
        {
            if (IsLocalHost())
                return;

            if (token != BuildToken)
            {
                DebugLogService.Instance.SessionWarning("CLIENT RX HostHello build mismatch: hook=" +
                    receiveHook + "; source=" + PlayerLabel(source) + "; token=" + token +
                    "; expected=" + BuildToken);
                return;
            }

            int clientId = GetClientId(source);
            if (clientId < 0)
            {
                DebugLogService.Instance.SessionWarning("CLIENT RX HostHello without source owner: hook=" +
                    receiveHook + "; source=" + PlayerLabel(source));
                return;
            }

            bool first = _hostHelloReceives == 0 || _hostClientId != clientId;
            _hostHelloReceives++;
            _hostClientId = clientId;
            _lastHostMessage = Time.unscaledTime;
            if (first)
            {
                DebugLogService.Instance.Session("CLIENT RX HostHello: hook=" + receiveHook +
                    "; source=" + PlayerLabel(source) + "; hostClientId=" + clientId +
                    "; tokenMatch=true");
            }
        }

        private void ReceiveDecision(Player source, string variableName, int decision,
            string receiveHook)
        {
            if (IsLocalHost())
                return;

            if (!IsHostPlayer(source))
            {
                DebugLogService.Instance.SessionWarning("CLIENT rejected Decision from non-host: hook=" +
                    receiveHook + "; source=" + PlayerLabel(source) + "; sourceClientId=" +
                    GetClientId(source) + "; expectedHostClientId=" + _hostClientId);
                return;
            }

            if (!string.Equals(variableName, DecisionVariable, StringComparison.OrdinalIgnoreCase))
            {
                string targetText = variableName.Substring(DecisionPrefix.Length);
                if (!int.TryParse(targetText, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out int targetId) || targetId != GetClientId(Player.Local))
                {
                    DebugLogService.Instance.SessionWarning("CLIENT ignored Decision for another client: " +
                        "target=" + targetText + "; localClientId=" + GetClientId(Player.Local));
                    return;
                }
            }

            bool previousApproval = _hostApproved;
            _hostApproved = decision == BuildToken;
            _decisionReceives++;
            _lastHostMessage = Time.unscaledTime;
            if (_decisionReceives == 1 || previousApproval != _hostApproved)
            {
                DebugLogService.Instance.Session("CLIENT RX Decision: hook=" + receiveHook +
                    "; source=" + PlayerLabel(source) + "; decision=" + decision +
                    "; expected=" + BuildToken + "; approved=" + _hostApproved +
                    "; localClientId=" + GetClientId(Player.Local));
            }
            if (_hostApproved)
                ShapePrefabService.Instance.RequestHostSnapshot();
        }

        private void BroadcastHostState()
        {
            if (Time.unscaledTime < _nextPulse)
                return;
            _nextPulse = Time.unscaledTime + PulseInterval;

            Player host = Player.Local;
            if (host == null)
                return;

            foreach (ClientRecord client in _clients.Values)
            {
                SendHostValue(client, HostHelloVariable, BuildToken, false);
                BroadcastDecision(client, false);
            }
        }

        private void BroadcastClientHello()
        {
            if (Time.unscaledTime < _nextPulse)
                return;
            _nextPulse = Time.unscaledTime + PulseInterval;
            bool queued = SendValue(Player.Local, ClientHelloVariable, BuildToken,
                out string diagnostic);
            _clientHelloSends++;
            if (!_clientHelloTransportKnown || queued != _lastClientHelloQueued)
            {
                _clientHelloTransportKnown = true;
                _lastClientHelloQueued = queued;
                string message = "CLIENT TX ClientHello: localClientId=" + GetClientId(Player.Local) +
                    "; token=" + BuildToken + "; queued=" + queued + "; " + diagnostic;
                if (queued)
                    DebugLogService.Instance.Session(message);
                else
                    DebugLogService.Instance.SessionWarning(message);
            }
        }

        private void BroadcastDecision(ClientRecord record, bool forceLog)
        {
            if (record == null)
                return;

            int payload = record.Approved && record.BuildToken == BuildToken ? BuildToken : -BuildToken;
            SendHostValue(record, DecisionVariable, payload, forceLog);
        }

        private void SendHostValue(ClientRecord record, string name, int value, bool forceLog)
        {
            Player host = Player.Local;
            Player recipient = PlayerValueRpcService.FindRemotePlayer(record.ClientId);
            bool queued = PlayerValueRpcService.SendToClient(host, recipient, name,
                value.ToString(CultureInfo.InvariantCulture), out string diagnostic);
            bool changed = !record.HasTransportResult ||
                record.LastTransportQueued != queued ||
                !string.Equals(record.LastTransportDiagnostic, diagnostic, StringComparison.Ordinal);

            if (forceLog || changed)
            {
                string message = "HOST TX " + ShortVariableName(name) + ": target=" +
                    PlayerLabel(recipient) + "; clientId=" + record.ClientId +
                    "; approved=" + record.Approved + "; queued=" + queued + "; " + diagnostic;
                if (queued)
                    DebugLogService.Instance.Session(message);
                else
                    DebugLogService.Instance.SessionWarning(message);
            }

            record.HasTransportResult = true;
            record.LastTransportQueued = queued;
            record.LastTransportDiagnostic = diagnostic;
        }

        private static bool SendValue(Player player, string name, int value, out string diagnostic)
        {
            if (player == null)
            {
                diagnostic = "local player is null";
                return false;
            }

            try
            {
                if (player.GetVariable(name) == null)
                {
                    player.AddVariable(new NumberVariable(name,
                        EVariableReplicationMode.Networked, false, EVariableMode.Player, player, value));
                }

                string text = value.ToString(CultureInfo.InvariantCulture);
                player.SetVariableValue(name, text, false);
                player.SendValue(name, text, true);
                diagnostic = "queued through Player.SendValue";
                return true;
            }
            catch (Exception ex)
            {
                diagnostic = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private void ExpireClients()
        {
            if (_clients.Count == 0)
                return;

            var expired = new List<int>();
            foreach (KeyValuePair<int, ClientRecord> pair in _clients)
            {
                if (Time.unscaledTime - pair.Value.LastSeen > ClientExpiry)
                    expired.Add(pair.Key);
            }
            for (int i = 0; i < expired.Count; i++)
                _clients.Remove(expired[i]);
        }

        private void ResetLobbyState()
        {
            _clients.Clear();
            _hostClientId = -1;
            _hostApproved = false;
            _lastHostMessage = -100f;
            _nextPulse = 0f;
            _nextWaitingDiagnostic = 0f;
            _reportedClientAccess = null;
            _clientHelloTransportKnown = false;
            _lastClientHelloQueued = false;
            _clientHelloSends = 0;
            _hostHelloReceives = 0;
            _decisionReceives = 0;
        }

        private bool IsLocalHost()
        {
            try { return Lobby.Instance != null && Lobby.Instance.IsInLobby && Lobby.Instance.IsHost; }
            catch { return false; }
        }

        private static int GetClientId(Player player)
        {
            try { return player?.Owner?.ClientId ?? -1; }
            catch { return -1; }
        }

        private void ReportClientAccess(bool allowed, string reason)
        {
            if (_reportedClientAccess.HasValue && _reportedClientAccess.Value == allowed)
                return;

            _reportedClientAccess = allowed;
            DebugLogService.Instance.Session("CLIENT access " + (allowed ? "ENABLED" : "DISABLED") +
                ": localClientId=" + GetClientId(Player.Local) + "; hostClientId=" +
                _hostClientId + "; reason=" + reason);
        }

        private static string PlayerLabel(Player player)
        {
            if (player == null)
                return "<null>";
            string label = player.PlayerName;
            if (string.IsNullOrEmpty(label))
                label = player.name;
            return string.IsNullOrEmpty(label) ? "<unnamed>" : label;
        }

        private static string ShortVariableName(string name)
        {
            return string.IsNullOrEmpty(name) ? "<empty>" :
                name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
                    ? name.Substring(Prefix.Length)
                    : name;
        }

        private static int ComputeBuildToken()
        {
            unchecked
            {
                string identity = BuildVersion + "|" + typeof(SessionAuthorityService).Module.ModuleVersionId;
                int hash = (int)2166136261;
                for (int i = 0; i < identity.Length; i++)
                    hash = (hash ^ identity[i]) * 16777619;
                return (hash & 0x3FFFFF) + 1;
            }
        }

        private static void LogReceiverHookState()
        {
            try
            {
                var target = AccessTools.Method(typeof(Player),
                    "RpcLogic___ReceiveValue_3895153758",
                    new[] { typeof(NetworkConnection), typeof(string), typeof(string) });
                bool installed = false;
                Patches patchInfo = target == null ? null : HarmonyLib.Harmony.GetPatchInfo(target);
                if (patchInfo != null)
                {
                    foreach (Patch prefix in patchInfo.Prefixes)
                    {
                        if (prefix?.PatchMethod?.DeclaringType == typeof(PlayerReceiveValueRpcLogicPatch))
                        {
                            installed = true;
                            break;
                        }
                    }
                }

                DebugLogService.Instance.Session("Receiver hook check: targetResolved=" +
                    (target != null) + "; rpcLogicPatchInstalled=" + installed +
                    "; version=" + BuildVersion + "; buildToken=" + BuildToken);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.SessionWarning("Receiver hook check failed: " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void Allow()
        {
            FeaturesAllowed = true;
            BlockReason = string.Empty;
        }

        private void Deny(string reason)
        {
            FeaturesAllowed = false;
            BlockReason = reason;
        }

        private void ScanLoadedAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    string name = assemblies[i]?.GetName()?.Name ?? string.Empty;
                    if (!LooksLikeImperium(name))
                        continue;
                    MarkRpMod(name);
                    return;
                }
            }
            catch { }
        }

        private void ScanInstallLocations()
        {
            try
            {
                string gameRoot = Path.GetDirectoryName(Application.dataPath);
                if (string.IsNullOrEmpty(gameRoot))
                    return;

                string[] roots =
                {
                    Path.Combine(gameRoot, "Mods"),
                    Path.Combine(gameRoot, "BepInEx", "plugins"),
                    Path.Combine(gameRoot, "MelonLoader", "Mods")
                };

                for (int i = 0; i < roots.Length; i++)
                {
                    string root = roots[i];
                    if (!Directory.Exists(root))
                        continue;

                    string[] files;
                    try { files = Directory.GetFiles(root, "*.dll", SearchOption.AllDirectories); }
                    catch { continue; }

                    for (int j = 0; j < files.Length; j++)
                    {
                        if (!LooksLikeImperium(Path.GetFileNameWithoutExtension(files[j])))
                            continue;
                        MarkRpMod(files[j]);
                        return;
                    }
                }
            }
            catch { }
        }

        private void MarkRpMod(string location)
        {
            _rpModBlocked = true;
            _rpModLocation = location ?? string.Empty;
        }

        private static bool LooksLikeImperium(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string normalized = string.Empty;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetterOrDigit(value[i]))
                    normalized += char.ToLowerInvariant(value[i]);
            }
            return normalized.Contains("siak") || normalized.Contains("imperium");
        }
    }
}
