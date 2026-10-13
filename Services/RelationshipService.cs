using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.Cartel;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Map;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Variables;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class RelationshipService
    {
        internal const string RegionUnlockVariable = "Nugzz.Region.Unlock";
        private sealed class Entry
        {
            public NPC Npc;
            public Customer Customer;
            public string Name;
            public string Id;
            public EMapRegion Region;
            public string GroupId;
            public string GroupName;
        }

        public const int PageSize = 8;
        private static readonly RelationshipService _instance = new RelationshipService();
        public static RelationshipService Instance => _instance;

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly FieldInfo _currentAffinityField;
        private readonly PropertyInfo _currentAffinityProperty;
        private string _searchText = string.Empty;
        private string _status = "Load a save, then refresh the people list.";
        private int _pageIndex;
        private int _selectedIndex = -1;
        private float _nextRefreshAttempt;
        private EDrugType _selectedDrugType = EDrugType.Marijuana;
        private int _regionFilter;
        private string _groupFilter = string.Empty;

        private RelationshipService()
        {
            _currentAffinityField = typeof(Customer).GetField(
                "currentAffinityData",
                BindingFlags.Instance | BindingFlags.NonPublic);
            _currentAffinityProperty = typeof(Customer).GetProperty(
                "currentAffinityData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public string SearchText => _searchText;
        public string Status => _status;
        public int PageIndex => _pageIndex;
        public int Count => _entries.Count;
        public EDrugType SelectedDrugType => _selectedDrugType;
        public bool HasSelection => GetSelected() != null;
        public bool SelectedIsCustomer => GetSelected()?.Customer != null;
        public int RegionFilter => _regionFilter;
        public string GroupFilter => _groupFilter;
        public void SetGroupFilter(string id)
        {
            _groupFilter = id ?? string.Empty;
            _pageIndex = 0;
            Refresh(false);
        }

        public void EnsureFresh()
        {
            if (Time.unscaledTime >= _nextRefreshAttempt)
                Refresh(false);
        }

        public void Refresh() => Refresh(true);

        private void Refresh(bool showStatus)
        {
            _nextRefreshAttempt = Time.unscaledTime + 2f;
            showStatus = showStatus || _entries.Count == 0;
            string selectedId = GetSelected()?.Id;
            _selectedIndex = -1;
            _entries.Clear();
            try
            {
                var registry = ManagerCacheService.Instance.NPCRegistry;
                List<S1ApiService.NpcInfo> apiNpcs = S1ApiService.Instance.GetNpcs();
                SpecialCustomerService.Instance.EnsureFresh();
                if (registry == null && apiNpcs.Count == 0 && SpecialCustomerService.Instance.Groups.Count == 0)
                {
                    _status = "NPC registry is not available in this scene.";
                    return;
                }

                Dictionary<int, Customer> customers = BuildCustomerMap();
                var members = new Dictionary<string, SpecialCustomerService.Member>(StringComparer.OrdinalIgnoreCase);
                foreach (var group in SpecialCustomerService.Instance.Groups)
                    foreach (var member in group.Members) members[member.Id] = member;
                var liveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var npcs = new List<NPC>();
                var apiInfo = new Dictionary<int, S1ApiService.NpcInfo>();
                var seen = new HashSet<int>();
                foreach (var info in apiNpcs)
                {
                    try
                    {
                        NPC npc = info.GameObject?.GetComponent<NPC>();
                        if (npc == null) continue;
                        int instanceId = npc.GetInstanceID();
                        apiInfo[instanceId] = info;
                        if (seen.Add(instanceId)) npcs.Add(npc);
                    }
                    catch { }
                }
                // Native registration includes visiting customers that do not yet
                // have a typed S1API wrapper. Do not use a fixed list of NPC names.
                for (int i = 0; registry != null && i < registry.Count; i++)
                {
                    NPC npc = registry[i];
                    if (npc != null && seen.Add(npc.GetInstanceID())) npcs.Add(npc);
                }

                for (int i = 0; i < npcs.Count; i++)
                {
                    NPC npc = npcs[i];
                    if (npc == null)
                        continue;
                    if (npc.TryCast<SpecialCustomer>() != null && !npc.gameObject.activeInHierarchy)
                        continue;

                    try
                    {
                        // Cartel goons are temporary combat actors, not contacts.
                        if (npc.TryCast<CartelGoon>() != null)
                            continue;
                    }
                    catch { }

                    apiInfo.TryGetValue(npc.GetInstanceID(), out var info);
                    string name = string.IsNullOrWhiteSpace(info.Name) ? GetNpcName(npc) : info.Name;
                    string id = string.IsNullOrWhiteSpace(info.Id) ? GetNpcId(npc) : info.Id;
                    liveIds.Add(id);
                    members.TryGetValue(id, out var member);
                    if (member != null) name = member.Name;
                    Customer customer = null;
                    try { customers.TryGetValue(npc.GetInstanceID(), out customer); }
                    catch { }
                    if (customer == null)
                        customer = GetCustomer(npc);
                    string type = customer != null ? "client" : "npc";
                    EMapRegion region = EMapRegion.Northtown;
                    try { region = npc.Region; } catch { }
                    if (!MatchesFilter(name, type, region, member?.GroupId, member?.GroupName))
                        continue;

                    _entries.Add(new Entry { Npc = npc, Customer = customer, Name = name, Id = id, Region = region,
                        GroupId = member?.GroupId, GroupName = member?.GroupName });
                }

                // Visiting NPCs are pooled and only instantiated while in town.
                // Show their native roster while absent; editing requires a live
                // instance and never changes the source NPC definition asset.
                foreach (var member in members.Values)
                {
                    if (liveIds.Contains(member.Id) ||
                        !MatchesFilter(member.Name, "visiting customer", EMapRegion.Northtown,
                            member.GroupId, member.GroupName, false)) continue;
                    _entries.Add(new Entry { Name = member.Name, Id = member.Id,
                        GroupId = member.GroupId, GroupName = member.GroupName });
                }

                _entries.Sort((a, b) =>
                {
                    int regionOrder = a.Region.CompareTo(b.Region);
                    return regionOrder != 0 ? regionOrder : string.Compare(a.Name, b.Name,
                        StringComparison.OrdinalIgnoreCase);
                });
                _selectedIndex = FindIndexById(selectedId);
                ClampPage();
                if (showStatus) _status = "Found " + _entries.Count + " NPCs and clients.";
            }
            catch (Exception ex)
            {
                _status = "Relationship scan failed: " + ex.GetType().Name;
                DebugLogService.Instance.VerboseException("Relationship scan failed", ex);
            }
        }

        public void SetSearchText(string value)
        {
            value = value ?? string.Empty;
            if (value == _searchText)
                return;
            _searchText = value;
            _pageIndex = 0;
            Refresh();
        }

        public void SetRegionFilter(int index)
        {
            int clamped = Mathf.Clamp(index, 0, Regions.Length);
            if (_regionFilter == clamped)
                return;
            _regionFilter = clamped;
            _pageIndex = 0;
            Refresh();
        }

        public string GetRegionFilterLabel(int index)
        {
            if (index <= 0)
                return (_regionFilter == 0 ? "> " : string.Empty) + "All";
            EMapRegion region = Regions[Mathf.Clamp(index - 1, 0, Regions.Length - 1)];
            return (_regionFilter == index ? "> " : string.Empty) + RegionName(region);
        }

        public string GetSelectedRegionUnlockLabel()
        {
            if (_regionFilter <= 0)
                return "Select a city section above";
            EMapRegion region = Regions[_regionFilter - 1];
            return (IsRegionUnlocked(region) ? "OPEN: " : "Unlock: ") + RegionName(region);
        }

        public void UnlockSelectedRegion()
        {
            if (_regionFilter <= 0)
            {
                SetStatus("Select one city section first");
                return;
            }
            if (LobbyService.Instance.IsInLobby() && !LobbyService.Instance.IsHost())
            {
                SendRegionUnlockRequest(Regions[_regionFilter - 1]);
                SetStatus("Requested city section unlock from the host");
                return;
            }

            UnlockRegion(Regions[_regionFilter - 1]);
        }

        internal bool TryReceiveNetworkValue(Player source, string variableName, string value)
        {
            if (!string.Equals(variableName, RegionUnlockVariable,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            if (!LobbyService.Instance.IsHost() || source == null || source.IsLocalPlayer ||
                !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out int rawRegion))
            {
                return true;
            }

            EMapRegion region = (EMapRegion)rawRegion;
            for (int i = 0; i < Regions.Length; i++)
            {
                if (Regions[i].Equals(region))
                {
                    UnlockRegion(region);
                    break;
                }
            }
            return true;
        }

        private void UnlockRegion(EMapRegion region)
        {
            try
            {
                var map = Il2CppScheduleOne.Map.Map.Instance;
                MapRegionData data = map?.GetRegionData(region);
                if (data == null)
                {
                    SetStatus("City section data is unavailable");
                    return;
                }

                data.SetUnlocked();
                var unlocked = map.GetUnlockedRegions();
                if (LevelManager.Instance != null && unlocked != null)
                    LevelManager.Instance.SetUnlockedRegions(null, unlocked);
                SetStatus(RegionName(region) + " unlocked");
                Refresh();
            }
            catch (Exception ex) { ReportFailure("City section unlock", ex); }
        }

        private static void SendRegionUnlockRequest(EMapRegion region)
        {
            Player player = Player.Local;
            if (player == null)
                return;

            try
            {
                if (player.GetVariable(RegionUnlockVariable) == null)
                {
                    player.AddVariable(new NumberVariable(RegionUnlockVariable,
                        EVariableReplicationMode.Networked, false,
                        EVariableMode.Player, player, 0f));
                }

                string value = ((int)region).ToString(CultureInfo.InvariantCulture);
                player.SetVariableValue(RegionUnlockVariable, value, false);
                player.SendValue(RegionUnlockVariable, value, true);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "City section unlock request failed: " + ex.Message);
            }
        }

        public int GetPageCount()
        {
            return Mathf.Max(1, Mathf.CeilToInt(_entries.Count / (float)PageSize));
        }

        public int GetPageItemCount()
        {
            return Mathf.Clamp(_entries.Count - _pageIndex * PageSize, 0, PageSize);
        }

        public string GetPageLabel(int row)
        {
            int index = _pageIndex * PageSize + row;
            if (index < 0 || index >= _entries.Count)
                return string.Empty;

            Entry entry = _entries[index];
            if (entry.Npc == null)
                return (index == _selectedIndex ? "> " : string.Empty) + entry.Name +
                    " | " + entry.GroupName + " | Not in town";
            NPCRelationData relation = GetRelation(entry.Npc);
            string kind = entry.Customer != null ? "CLIENT" : "NPC";
            string state = relation != null && relation.Unlocked ? "UNLOCKED" : "LOCKED";
            float value = relation?.RelationDelta ?? 0f;
            return (index == _selectedIndex ? "> " : string.Empty) + entry.Name +
                " | " + (entry.GroupName ?? RegionName(entry.Region)) + " | " + kind + " | " +
                value.ToString("0.00") + "/5 | " + state;
        }

        public void SelectPageRow(int row)
        {
            int index = _pageIndex * PageSize + row;
            if (index >= 0 && index < _entries.Count)
                _selectedIndex = index;
        }

        public void PreviousPage()
        {
            _pageIndex = Mathf.Max(0, _pageIndex - 1);
        }

        public void NextPage()
        {
            _pageIndex = Mathf.Min(GetPageCount() - 1, _pageIndex + 1);
        }

        public string GetSelectedDetails()
        {
            Entry entry = GetSelected();
            if (entry == null)
                return "No NPC or client selected.";
            if (entry.Npc == null)
                return entry.Name + "\nGroup: " + entry.GroupName + " | ID: " + entry.Id +
                    "\nNot currently in town. Queue this group for tomorrow to meet them.";

            NPCRelationData relation = GetRelation(entry.Npc);
            string details = entry.Name + "\n" +
                (string.IsNullOrEmpty(entry.GroupName) ? string.Empty : "Group: " + entry.GroupName + " | ") +
                "Type: " + (entry.Customer != null ? "Client / customer" : "NPC") +
                " | City section: " + RegionName(entry.Region) +
                " | ID: " + ShortId(entry.Id) + "\n";

            if (relation != null)
            {
                details += "Relationship: " + relation.RelationDelta.ToString("0.00") +
                    "/5 (" + (relation.NormalizedRelationDelta * 100f).ToString("0") + "%)" +
                    " | " + (relation.Unlocked ? "Unlocked" : "Locked") +
                    " | Known: " + (relation.IsKnown() ? "Yes" : "No") + "\n";
            }

            if (entry.Customer != null)
            {
                Customer customer = entry.Customer;
                details += "Addiction: " + (customer.CurrentAddiction * 100f).ToString("0") + "%" +
                    " | Deals offered: " + customer.OfferedDeals +
                    " | Deliveries: " + customer.CompletedDeliveries +
                    " | Awaiting delivery: " + (customer.IsAwaitingDelivery ? "Yes" : "No") + "\n";
                details += DrugName(_selectedDrugType) + " affinity: " +
                    (GetAffinity(customer, _selectedDrugType) * 100f).ToString("0") + "%";
            }

            return details;
        }

        public void SetRelationship(float value)
        {
            Entry entry = RequireEditableSelection();
            NPCRelationData relation = GetRelation(entry?.Npc);
            if (relation == null)
                return;

            try
            {
                relation.SetRelationship(Mathf.Clamp(value, 0f, 5f), true);
                SetStatus("Relationship updated for " + entry.Name);
            }
            catch (Exception ex) { ReportFailure("Relationship update", ex); }
        }

        public void ChangeRelationship(float amount)
        {
            Entry entry = GetSelected();
            NPCRelationData relation = GetRelation(entry?.Npc);
            if (relation != null)
                SetRelationship(relation.RelationDelta + amount);
        }

        public void UnlockSelected()
        {
            Entry entry = RequireEditableSelection();
            NPCRelationData relation = GetRelation(entry?.Npc);
            if (relation == null)
                return;
            try
            {
                relation.Unlock(NPCRelationData.EUnlockType.DirectApproach, true);
                SetStatus("Unlocked " + entry.Name);
            }
            catch (Exception ex) { ReportFailure("NPC unlock", ex); }
        }

        public void UnlockConnections()
        {
            Entry entry = RequireEditableSelection();
            NPCRelationData relation = GetRelation(entry?.Npc);
            if (relation == null)
                return;
            try
            {
                relation.UnlockConnections();
                SetStatus("Unlocked connections for " + entry.Name);
            }
            catch (Exception ex) { ReportFailure("Connection unlock", ex); }
        }

        public void SetDrugType(int index)
        {
            EDrugType[] values = DrugTypes;
            _selectedDrugType = values[Mathf.Clamp(index, 0, values.Length - 1)];
        }

        public void SetAddiction(float value)
        {
            Entry entry = RequireEditableSelection();
            Customer customer = entry?.Customer;
            if (customer == null)
                return;
            try
            {
                customer.ChangeAddiction(Mathf.Clamp01(value) - customer.CurrentAddiction);
                SetStatus("Addiction updated for " + entry.Name);
            }
            catch (Exception ex) { ReportFailure("Addiction update", ex); }
        }

        public void ChangeAddiction(float amount)
        {
            Customer customer = GetSelected()?.Customer;
            if (customer != null)
                SetAddiction(customer.CurrentAddiction + amount);
        }

        public void SetAffinity(float value)
        {
            Entry entry = RequireEditableSelection();
            Customer customer = entry?.Customer;
            if (customer == null)
                return;
            try
            {
                float current = GetAffinity(customer, _selectedDrugType);
                customer.AdjustAffinity(_selectedDrugType, Mathf.Clamp01(value) - current);
                SetStatus(DrugName(_selectedDrugType) + " affinity updated for " + entry.Name);
            }
            catch (Exception ex) { ReportFailure("Affinity update", ex); }
        }

        public void ChangeAffinity(float amount)
        {
            Customer customer = GetSelected()?.Customer;
            if (customer != null)
                SetAffinity(GetAffinity(customer, _selectedDrugType) + amount);
        }

        public void MarkRecommended()
        {
            Entry entry = RequireEditableSelection();
            if (entry?.Customer == null)
                return;
            try
            {
                entry.Customer.SetHasBeenRecommended();
                SetStatus(entry.Name + " marked as recommended");
            }
            catch (Exception ex) { ReportFailure("Recommendation update", ex); }
        }

        public void ForceDealOffer()
        {
            Entry entry = RequireEditableSelection();
            if (entry?.Customer == null)
                return;
            try
            {
                entry.Customer.ForceDealOffer();
                SetStatus("Requested a deal offer from " + entry.Name);
            }
            catch (Exception ex) { ReportFailure("Deal offer", ex); }
        }

        public string GetDrugLabel(int index)
        {
            EDrugType[] values = DrugTypes;
            EDrugType type = values[Mathf.Clamp(index, 0, values.Length - 1)];
            return (type == _selectedDrugType ? "> " : string.Empty) + DrugName(type);
        }

        private Entry RequireEditableSelection()
        {
            Entry entry = GetSelected();
            if (entry == null)
            {
                SetStatus("Select an NPC or client first");
                return null;
            }

            if (entry.Npc == null)
            {
                SetStatus("This visitor is not in town. Queue their group before editing them.");
                return null;
            }

            if (LobbyService.Instance.IsInLobby() && !LobbyService.Instance.IsHost())
            {
                SetStatus("Relationship editing is host-only in multiplayer");
                return null;
            }

            return entry;
        }

        private Entry GetSelected()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _entries.Count) return null;
            Entry entry = _entries[_selectedIndex];
            return entry;
        }

        private bool MatchesFilter(string name, string type, EMapRegion region,
            string groupId = null, string groupName = null, bool hasRegion = true)
        {
            if (!string.IsNullOrEmpty(_groupFilter) && !string.Equals(groupId, _groupFilter, StringComparison.OrdinalIgnoreCase))
                return false;
            if (_regionFilter > 0 && (!hasRegion || region != Regions[_regionFilter - 1]))
                return false;
            return string.IsNullOrWhiteSpace(_searchText) ||
                name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                type.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (groupName ?? string.Empty).IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                RegionName(region).IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsRegionUnlocked(EMapRegion region)
        {
            try { return Il2CppScheduleOne.Map.Map.Instance?.GetRegionData(region)?.IsUnlocked == true; }
            catch { return false; }
        }

        private static string RegionName(EMapRegion region)
        {
            return region == EMapRegion.Northtown ? "North Town" : region.ToString();
        }

        private int FindIndexById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return -1;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (string.Equals(_entries[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void ClampPage()
        {
            _pageIndex = Mathf.Clamp(_pageIndex, 0, GetPageCount() - 1);
        }

        private float GetAffinity(Customer customer, EDrugType type)
        {
            if (customer == null)
                return 0f;
            try
            {
                var data = (_currentAffinityProperty?.GetValue(customer) ??
                    _currentAffinityField?.GetValue(customer)) as CustomerAffinityData;
                if (data != null)
                    return Mathf.Clamp01(data.GetAffinity(type));
            }
            catch { }
            try
            {
                return Mathf.Clamp01(customer.CustomerData.DefaultAffinityData.GetAffinity(type));
            }
            catch { return 0f; }
        }

        private static NPCRelationData GetRelation(NPC npc)
        {
            try { return npc?.RelationData; }
            catch { return null; }
        }

        private static Customer GetCustomer(NPC npc)
        {
            try { return npc?.GetComponent<Customer>(); }
            catch { return null; }
        }

        private static Dictionary<int, Customer> BuildCustomerMap()
        {
            var result = new Dictionary<int, Customer>();
            AddCustomers(result, Customer.LockedCustomers);
            AddCustomers(result, Customer.UnlockedCustomers);
            return result;
        }

        private static void AddCustomers(
            Dictionary<int, Customer> result,
            Il2CppSystem.Collections.Generic.List<Customer> customers)
        {
            if (customers == null)
                return;

            for (int i = 0; i < customers.Count; i++)
            {
                try
                {
                    Customer customer = customers[i];
                    NPC npc = customer?.NPC;
                    if (npc != null)
                        result[npc.GetInstanceID()] = customer;
                }
                catch { }
            }
        }

        private static string GetNpcName(NPC npc)
        {
            try
            {
                string fullName = S1ApiService.ReadPublicMember(npc, "FullName") as string;
                if (!string.IsNullOrWhiteSpace(fullName)) return fullName.Trim();
                object identity = GetBasicInfo(npc) ?? npc;
                string combined = ((S1ApiService.ReadPublicMember(identity, "FirstName") as string ?? string.Empty) + " " +
                    (S1ApiService.ReadPublicMember(identity, "LastName") as string ?? string.Empty)).Trim();
                return string.IsNullOrWhiteSpace(combined) ? npc.name : combined;
            }
            catch { return "Unknown NPC"; }
        }

        private static string GetNpcId(NPC npc)
        {
            try
            {
                if (npc == null) return string.Empty;
                string id = S1ApiService.ReadPublicMember(GetBasicInfo(npc) ?? npc, "ID") as string;
                return string.IsNullOrWhiteSpace(id) ? npc.GUID.ToString() : id;
            }
            catch { return string.Empty; }
        }

        private static object GetBasicInfo(NPC npc)
        {
            return S1ApiService.ReadPublicMember(S1ApiService.ReadPublicMember(npc, "NPCData"), "BasicInfo");
        }

        public void ResetForScene()
        {
            _entries.Clear();
            _selectedIndex = -1;
            _pageIndex = 0;
            _nextRefreshAttempt = 0f;
            _status = "Load a save, then refresh the people list.";
        }

        private static string ShortId(string id)
        {
            return string.IsNullOrEmpty(id) ? "unknown" : id.Substring(0, Mathf.Min(12, id.Length));
        }

        private void SetStatus(string message)
        {
            _status = message;
            NotificationService.Instance.Status(message);
        }

        private void ReportFailure(string action, Exception ex)
        {
            _status = action + " failed: " + ex.GetType().Name;
            NotificationService.Instance.Error(_status);
            DebugLogService.Instance.VerboseException(_status, ex);
        }

        private static string DrugName(EDrugType type)
        {
            switch (type)
            {
                case EDrugType.Marijuana: return "Weed";
                case EDrugType.Methamphetamine: return "Meth";
                default: return type.ToString();
            }
        }

        private static readonly EDrugType[] DrugTypes =
        {
            EDrugType.Marijuana,
            EDrugType.Methamphetamine,
            EDrugType.Cocaine,
            EDrugType.MDMA,
            EDrugType.Shrooms,
            EDrugType.Heroin
        };

        private static readonly EMapRegion[] Regions =
        {
            EMapRegion.Northtown,
            EMapRegion.Westville,
            EMapRegion.Downtown,
            EMapRegion.Docks,
            EMapRegion.Suburbia,
            EMapRegion.Uptown
        };
    }
}
