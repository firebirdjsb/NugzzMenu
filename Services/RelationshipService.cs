using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using Il2CppScheduleOne.Product;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class RelationshipService
    {
        private sealed class Entry
        {
            public NPC Npc;
            public Customer Customer;
            public string Name;
        }

        public const int PageSize = 8;
        private static readonly RelationshipService _instance = new RelationshipService();
        public static RelationshipService Instance => _instance;

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly FieldInfo _currentAffinityField;
        private string _searchText = string.Empty;
        private string _status = "Load a save, then refresh the people list.";
        private int _pageIndex;
        private int _selectedIndex = -1;
        private float _nextRefreshAttempt;
        private EDrugType _selectedDrugType = EDrugType.Marijuana;

        private RelationshipService()
        {
            _currentAffinityField = typeof(Customer).GetField(
                "currentAffinityData",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public string SearchText => _searchText;
        public string Status => _status;
        public int PageIndex => _pageIndex;
        public int Count => _entries.Count;
        public EDrugType SelectedDrugType => _selectedDrugType;
        public bool HasSelection => GetSelected() != null;
        public bool SelectedIsCustomer => GetSelected()?.Customer != null;

        public void EnsureFresh()
        {
            if (_entries.Count > 0 || Time.unscaledTime < _nextRefreshAttempt)
                return;
            _nextRefreshAttempt = Time.unscaledTime + 2f;
            Refresh();
        }

        public void Refresh()
        {
            string selectedId = GetNpcId(GetSelected()?.Npc);
            _entries.Clear();
            try
            {
                var registry = ManagerCacheService.Instance.NPCRegistry;
                if (registry == null)
                {
                    _status = "NPC registry is not available in this scene.";
                    return;
                }

                Dictionary<int, Customer> customers = BuildCustomerMap();
                for (int i = 0; i < registry.Count; i++)
                {
                    NPC npc = registry[i];
                    if (npc == null)
                        continue;

                    string name = GetNpcName(npc);
                    Customer customer = null;
                    try { customers.TryGetValue(npc.GetInstanceID(), out customer); }
                    catch { }
                    if (customer == null)
                        customer = GetCustomer(npc);
                    string type = customer != null ? "client" : "npc";
                    if (!MatchesFilter(name, type))
                        continue;

                    _entries.Add(new Entry { Npc = npc, Customer = customer, Name = name });
                }

                _entries.Sort((a, b) => string.Compare(a.Name, b.Name,
                    StringComparison.OrdinalIgnoreCase));
                _selectedIndex = FindIndexById(selectedId);
                ClampPage();
                _status = "Found " + _entries.Count + " NPCs and clients.";
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
            NPCRelationData relation = GetRelation(entry.Npc);
            string kind = entry.Customer != null ? "CLIENT" : "NPC";
            string state = relation != null && relation.Unlocked ? "UNLOCKED" : "LOCKED";
            float value = relation?.RelationDelta ?? 0f;
            return (index == _selectedIndex ? "> " : string.Empty) + entry.Name +
                " | " + kind + " | " + value.ToString("0.00") + "/5 | " + state;
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

            NPCRelationData relation = GetRelation(entry.Npc);
            string details = entry.Name + "\n" +
                "Type: " + (entry.Customer != null ? "Client / customer" : "NPC") +
                " | ID: " + ShortId(GetNpcId(entry.Npc)) + "\n";

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

            if (LobbyService.Instance.IsInLobby() && !LobbyService.Instance.IsHost())
            {
                SetStatus("Relationship editing is host-only in multiplayer");
                return null;
            }

            return entry;
        }

        private Entry GetSelected()
        {
            return _selectedIndex >= 0 && _selectedIndex < _entries.Count
                ? _entries[_selectedIndex]
                : null;
        }

        private bool MatchesFilter(string name, string type)
        {
            return string.IsNullOrWhiteSpace(_searchText) ||
                name.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                type.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int FindIndexById(string id)
        {
            if (string.IsNullOrEmpty(id))
                return -1;
            for (int i = 0; i < _entries.Count; i++)
            {
                if (GetNpcId(_entries[i].Npc) == id)
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
                var data = _currentAffinityField?.GetValue(customer) as CustomerAffinityData;
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
                if (!string.IsNullOrWhiteSpace(npc.fullName))
                    return npc.fullName.Trim();
                string combined = ((npc.FirstName ?? string.Empty) + " " +
                    (npc.LastName ?? string.Empty)).Trim();
                return string.IsNullOrWhiteSpace(combined) ? npc.name : combined;
            }
            catch { return "Unknown NPC"; }
        }

        private static string GetNpcId(NPC npc)
        {
            try { return npc == null ? string.Empty : npc.GUID.ToString(); }
            catch { return string.Empty; }
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
    }
}
