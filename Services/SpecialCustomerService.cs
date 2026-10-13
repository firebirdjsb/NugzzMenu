using System;
using System.Collections.Generic;
using Il2CppScheduleOne.NPCs.Framework;
using Il2CppScheduleOne.SpecialCustomers;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class SpecialCustomerService
    {
        public sealed class Member
        {
            public string Id;
            public string Name;
            public string GroupId;
            public string GroupName;
        }

        public sealed class Group
        {
            public string Id;
            public string Name;
            public readonly List<Member> Members = new List<Member>();
        }

        public static SpecialCustomerService Instance { get; } = new SpecialCustomerService();
        private readonly List<Group> _groups = new List<Group>();
        private float _nextRefresh;
        public IReadOnlyList<Group> Groups => _groups;
        public string Status { get; private set; } = "Load a save to discover visiting groups.";

        private SpecialCustomerService() { }

        public void Refresh()
        {
            _nextRefresh = Time.unscaledTime + 2f;
            _groups.Clear();
            try
            {
                var manager = SpecialCustomerManager.Instance;
                var definitions = manager?._specialCustomerGroups;
                if (definitions == null) return;
                for (int i = 0; i < definitions.Count; i++)
                {
                    SpecialCustomerData data = definitions[i];
                    if (data == null || string.IsNullOrWhiteSpace(data.GroupId)) continue;
                    var group = new Group { Id = data.GroupId, Name = data.GroupName };
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    AddMember(group, data.Leader?.Data, seen);
                    for (int j = 0; data.Customers != null && j < data.Customers.Count; j++)
                        AddMember(group, data.Customers[j]?.Data, seen);
                    _groups.Add(group);
                }
                _groups.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Special customer catalog", ex);
            }
        }

        public void EnsureFresh() { if (Time.unscaledTime >= _nextRefresh) Refresh(); }

        private static void AddMember(Group group, BaseNPCDataObject definition, HashSet<string> seen)
        {
            if (definition == null) return;
            BasicInfo info = definition.GetOriginalData()?.BasicInfo;
            if (info == null || string.IsNullOrWhiteSpace(info.ID) || !seen.Add(info.ID)) return;
            // 0.4.7f12 spells this native name "Vince Knuckes". Correct the
            // menu label by confirmed stable ID without modifying NPC data.
            string name = info.ID == "vince_knuckles" ? "Vince Knuckles" : info.FullName;
            group.Members.Add(new Member { Id = info.ID, Name = name,
                GroupId = group.Id, GroupName = group.Name });
        }

        public void QueueForTomorrow(string groupId)
        {
            if (!SessionAuthorityService.Instance.FeaturesAllowed ||
                (LobbyService.Instance.IsInLobby() && !LobbyService.Instance.IsHost()))
            {
                Report("Queuing visiting groups is host-only.");
                return;
            }
            try
            {
                var manager = SpecialCustomerManager.Instance;
                if (manager == null || manager._currentData == null)
                {
                    Report("Load a save before queuing a visiting group.");
                    return;
                }
                SpecialCustomerData group = manager.GetSpecialCustomerData(groupId);
                if (group == null) { Report("Select an available visiting group first."); return; }

                // This is the same native path as queuecustomergroup. It selects
                // WaitingForArrival with one day remaining and performs native
                // cleanup of the current camp instead of constructing NPCs.
                manager.QueueCustomerGroup(group.GroupId);
                var queued = manager._currentData;
                if (queued.GroupId != group.GroupId ||
                    queued.Phase != SpecialCustomerPhase.WaitingForArrival || queued.DaysLeftInPhase != 1)
                {
                    Report("The game did not confirm the requested group schedule.");
                    return;
                }
                Report(group.GroupName + " queued for tomorrow. Sleep to the next day; this replaces the current group schedule.");
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Queue visiting group", ex);
                Report("Could not queue the group: " + ex.GetType().Name);
            }
        }

        private void Report(string message)
        {
            Status = message;
            NotificationService.Instance.Notify(message);
        }

        public void ResetForScene() { _groups.Clear(); _nextRefresh = 0f;
            Status = "Load a save to discover visiting groups."; }
    }
}
