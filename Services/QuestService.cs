using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Il2CppScheduleOne.Quests;
using Il2CppScheduleOne.Variables;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NugzzMenu.Services
{
    public sealed class QuestService
    {
        private const int MaxQuestItems = 300;
        private const int MaxCollectionItems = 240;

        private static readonly QuestService _instance = new QuestService();
        public static QuestService Instance => _instance;

        private readonly List<object> _quests = new List<object>();
        private readonly List<string> _labels = new List<string>();
        private readonly HashSet<string> _seen = new HashSet<string>();

        private bool _hasScanned;
        private bool _refreshing;
        private string _selectedDetails = "No quest selected.";
        private int _lastHudObjectsInspected;
        private int _lastHudTextCandidates;
        private int _lastHudSnapshots;

        public int SelectedIndex { get; private set; }
        public int PageIndex { get; private set; }
        public string LastStatus { get; private set; } = "Open a save, then press Refresh Quest List.";
        public int QuestCount => _quests.Count;

        private sealed class QuestDisplaySnapshot
        {
            public string Label;
            public string State;
            public string Source;
            public object ActionTarget;
        }

        private sealed class PendingHudText
        {
            public string Text;
            public object ActionTarget;
        }

        private struct QuestSortItem
        {
            public object Quest;
            public string Label;
            public int OriginalIndex;
        }

        private QuestService() { }

        public void EnsureFresh()
        {
            if (!_hasScanned && !_refreshing)
                Refresh();
        }

        public void Refresh()
        {
            if (_refreshing)
                return;

            _refreshing = true;
            float start = Time.realtimeSinceStartup;

            _quests.Clear();
            _labels.Clear();
            _seen.Clear();
            _lastHudObjectsInspected = 0;
            _lastHudTextCandidates = 0;
            _lastHudSnapshots = 0;

            try
            {
                // Quest.Quests is the authoritative runtime catalogue. Scanning HUD objects
                // also discovers every individual objective, which made one quest appear many times.
                if (!AddNativeQuestCatalog())
                {
                    AddQuestManagerObjects();
                    AddLoadedQuestResources();
                    AddQuestHudObjects();
                    if (_quests.Count == 0)
                        AddVisibleHudTextSnapshots();
                }
                SortQuestRecords();

                if (SelectedIndex >= _quests.Count)
                    SelectedIndex = Math.Max(0, _quests.Count - 1);
                if (_quests.Count == 0)
                    SelectedIndex = 0;

                PageIndex = Mathf.Clamp(PageIndex, 0, GetPageCount(8) - 1);
                RebuildSelectedDetails();

                float ms = (Time.realtimeSinceStartup - start) * 1000f;
                LastStatus = _quests.Count == 0
                    ? "No quests found. HUD scan checked " + _lastHudObjectsInspected +
                      " objects and " + _lastHudTextCandidates + " text labels in " +
                      ms.ToString("0.0") + "ms."
                    : "Found " + _quests.Count + " quests/contracts in " + ms.ToString("0.0") + "ms";
            }
            catch (Exception ex)
            {
                LastStatus = "Quest refresh failed: " + ex.Message;
                RebuildSelectedDetails();
            }
            finally
            {
                _hasScanned = true;
                _refreshing = false;
            }
        }

        public int GetPageCount(int pageSize)
        {
            if (pageSize <= 0)
                return 1;
            return Math.Max(1, (_quests.Count + pageSize - 1) / pageSize);
        }

        public int GetPageItemCount(int pageSize)
        {
            if (pageSize <= 0)
                return 0;

            int start = PageIndex * pageSize;
            if (start >= _quests.Count)
                return 0;

            return Math.Min(pageSize, _quests.Count - start);
        }

        public int GetPageQuestIndex(int localIndex, int pageSize)
        {
            return PageIndex * pageSize + localIndex;
        }

        public string GetQuestLabel(int index)
        {
            if (index < 0 || index >= _labels.Count)
                return "Unknown quest";
            return _labels[index];
        }

        public string GetSelectedDetails()
        {
            return _selectedDetails;
        }

        public float GetSelectedDetailsHeight(float width)
        {
            int availableCharacters = Math.Max(24, Mathf.FloorToInt(width / 7.2f));
            int lines = 0;
            string[] sourceLines = (_selectedDetails ?? string.Empty).Split('\n');
            for (int i = 0; i < sourceLines.Length; i++)
            {
                int length = Math.Max(1, sourceLines[i].Length);
                lines += Math.Max(1, Mathf.CeilToInt((float)length / availableCharacters));
            }

            return Mathf.Clamp(lines * 16f + 8f, 52f, 320f);
        }

        public void Select(int index)
        {
            if (_quests.Count == 0)
            {
                SelectedIndex = 0;
                RebuildSelectedDetails();
                return;
            }

            SelectedIndex = Mathf.Clamp(index, 0, _quests.Count - 1);
            RebuildSelectedDetails();
        }

        public void NextPage(int pageSize)
        {
            PageIndex = Math.Min(PageIndex + 1, GetPageCount(pageSize) - 1);
        }

        public void PreviousPage()
        {
            PageIndex = Math.Max(0, PageIndex - 1);
        }

        public void CompleteSelected()
        {
            object quest = GetSelectedActionTarget();
            if (quest == null)
            {
                LastStatus = _quests.Count == 0 ? "No quest objects found. Refresh after loading into a save." : "No quest selected";
                return;
            }

            LastStatus = CompleteQuestObject(quest)
                ? "Completed: " + GetQuestLabel(SelectedIndex)
                : "No complete method found for selected quest/entry";
            Refresh();
        }

        public void StartSelected()
        {
            object quest = GetSelectedActionTarget();
            if (quest == null)
            {
                LastStatus = _quests.Count == 0 ? "No quest objects found. Refresh after loading into a save." : "No quest selected";
                return;
            }

            LastStatus = StartQuestObject(quest)
                ? "Started/activated: " + GetQuestLabel(SelectedIndex)
                : "No start method found for selected quest/entry";
            Refresh();
        }

        public void EndSelected()
        {
            object quest = GetSelectedActionTarget();
            if (quest == null)
            {
                LastStatus = _quests.Count == 0 ? "No quest objects found. Refresh after loading into a save." : "No quest selected";
                return;
            }

            LastStatus = EndQuestObject(quest)
                ? "Ended: " + GetQuestLabel(SelectedIndex)
                : "No end method found for selected quest/entry";
            Refresh();
        }

        public void ResetSelected()
        {
            object quest = GetSelectedActionTarget();
            if (quest == null)
            {
                LastStatus = _quests.Count == 0 ? "No quest objects found. Refresh after loading into a save." : "No quest selected";
                return;
            }

            LastStatus = ResetQuestObject(quest)
                ? "Reset: " + GetQuestLabel(SelectedIndex)
                : "No reset path found for selected quest/entry";
            Refresh();
        }

        public void InspectWelcomeExplosionQuest()
        {
            Quest_WelcomeToHylandPoint welcome = FindWelcomeQuest();
            if (welcome == null)
            {
                LastStatus = "Welcome quest is not loaded in this save.";
                NotificationService.Instance.Warning("Welcome quest not found");
                Refresh();
                return;
            }

            if (welcome.State != EQuestState.Active)
            {
                LastStatus = "Welcome quest is " + welcome.State + ". " +
                    GetWelcomeObjectiveStatus(welcome);
                NotificationService.Instance.Status("Welcome quest state checked");
                Refresh();
                return;
            }

            // This is intentionally read-only. Replaying SetRVDestroyed while a player is
            // reading the letter can bypass the native message/quest transition.
            LastStatus = "Welcome quest is active. " + GetWelcomeObjectiveStatus(welcome);
            NotificationService.Instance.Status("Welcome quest state checked");

            Refresh();
        }

        public bool IsWelcomeQuestActive()
        {
            Quest_WelcomeToHylandPoint welcome = FindWelcomeQuest();
            return welcome != null && welcome.State == EQuestState.Active;
        }

        public bool CompleteWelcomeQuestForManualRvAction()
        {
            Quest_WelcomeToHylandPoint welcome = FindWelcomeQuest();
            if (welcome == null)
            {
                LastStatus = "Welcome quest is not loaded; RV was not changed.";
                return false;
            }

            try
            {
                if (welcome.State == EQuestState.Inactive)
                    welcome.Begin(true);

                welcome.SetRVDestroyed();
                int entryCount = welcome.Entries == null ? 0 : welcome.Entries.Count;
                for (int i = 0; i < entryCount; i++)
                    welcome.SetQuestEntryState(i, EQuestState.Completed, true);

                if (welcome.State != EQuestState.Completed)
                    welcome.Complete(true);

                LastStatus = "Welcome to Hyland Point marked complete for the manual RV action.";
                Refresh();
                return true;
            }
            catch (Exception ex)
            {
                LastStatus = "Could not complete Welcome quest: " + ex.Message;
                return false;
            }
        }

        private object GetSelectedQuest()
        {
            if (SelectedIndex < 0 || SelectedIndex >= _quests.Count)
                return null;
            return _quests[SelectedIndex];
        }

        private object FindWelcomeQuestObject()
        {
            Quest_WelcomeToHylandPoint nativeWelcome = FindWelcomeQuest();
            if (nativeWelcome != null)
                return nativeWelcome;

            for (int i = 0; i < _quests.Count; i++)
            {
                string label = i < _labels.Count ? _labels[i] : BuildLabel(_quests[i]);
                if (!LooksLikeWelcomeExplosion(label))
                    continue;

                object target = GetActionTarget(_quests[i]);
                object quest = GetParentQuestIfEntry(target);
                if (quest != null && LooksLikeQuestType(quest.GetType()))
                    return quest;
                if (target != null && LooksLikeQuestType(target.GetType()))
                    return target;
            }

            object scanned = FindQuestByTitle("Welcome to Hyland Point");
            return scanned;
        }

        private bool AddNativeQuestCatalog()
        {
            bool found = false;

            try
            {
                found |= AddNativeQuestCollection(Quest.ActiveQuests);
                found |= AddNativeQuestCollection(Quest.Quests);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Native quest catalogue unavailable: " + ex.Message);
            }

            return found;
        }

        private bool AddNativeQuestCollection(object collection)
        {
            int count = ReadCount(collection);
            if (count <= 0)
                return false;

            bool found = false;
            count = Math.Min(count, MaxQuestItems);
            for (int i = 0; i < count; i++)
            {
                Quest quest = ReadIndexedItem(collection, i) as Quest;
                if (quest == null)
                    continue;

                AddQuestRecord(quest);
                found = true;
            }

            return found;
        }

        private static Quest_WelcomeToHylandPoint FindWelcomeQuest()
        {
            try
            {
                Quest_WelcomeToHylandPoint[] active = Object.FindObjectsOfType<Quest_WelcomeToHylandPoint>();
                if (active != null && active.Length > 0)
                    return active[0];
            }
            catch { }

            try
            {
                object[] quests = { Quest.ActiveQuests, Quest.Quests };
                for (int source = 0; source < quests.Length; source++)
                {
                    int count = ReadCount(quests[source]);
                    for (int i = 0; i < count; i++)
                    {
                        Quest_WelcomeToHylandPoint welcome =
                            ReadIndexedItem(quests[source], i) as Quest_WelcomeToHylandPoint;
                        if (welcome != null)
                            return welcome;
                    }
                }
            }
            catch { }

            return null;
        }

        private static string GetWelcomeObjectiveStatus(Quest_WelcomeToHylandPoint welcome)
        {
            if (welcome == null)
                return string.Empty;

            try
            {
                string returnState = welcome.ReturnToRVQuest == null
                    ? "missing"
                    : welcome.ReturnToRVQuest.State.ToString();
                string messagesState = welcome.ReadMessagesQuest == null
                    ? "missing"
                    : welcome.ReadMessagesQuest.State.ToString();
                return "Objectives: Return to RV=" + returnState +
                    ", Read messages=" + messagesState + ".";
            }
            catch
            {
                return string.Empty;
            }
        }

        private static object GetParentQuestIfEntry(object value)
        {
            if (value == null || !LooksLikeQuestEntryType(value.GetType()))
                return null;

            object parent = ReadMember(value, "ParentQuest");
            return parent != null && LooksLikeQuestType(parent.GetType()) ? parent : null;
        }

        private object GetSelectedActionTarget()
        {
            return GetActionTarget(GetSelectedQuest());
        }

        private static object GetActionTarget(object quest)
        {
            QuestDisplaySnapshot snapshot = quest as QuestDisplaySnapshot;
            return snapshot != null ? snapshot.ActionTarget : quest;
        }

        private void RebuildSelectedDetails()
        {
            object quest = GetSelectedQuest();
            if (quest == null)
            {
                _selectedDetails = "No quest selected.";
                return;
            }

            object actionTarget = GetActionTarget(quest);
            Quest nativeQuest = actionTarget as Quest;
            if (nativeQuest != null)
            {
                _selectedDetails = BuildNativeQuestDetails(nativeQuest);
                return;
            }

            _selectedDetails = GetQuestLabel(SelectedIndex) + "\n" +
                "Type: " + SafeTypeName(quest) + "\n" +
                "State: " + ReadState(quest) + "\n" +
                "Action target: " + (actionTarget == null ? "HUD text only" : SafeTypeName(actionTarget));
        }

        private static string BuildNativeQuestDetails(Quest quest)
        {
            string title = string.IsNullOrWhiteSpace(quest.Title) ? quest.name : quest.Title;
            string questId = quest.StaticGUID ?? quest.GUID.ToString();
            if (!string.IsNullOrEmpty(questId) && questId.Length > 12)
                questId = questId.Substring(0, 12);
            string details = title + "\nState: " + quest.State +
                " | ID: " + questId + "\nObjectives:";

            try
            {
                int count = quest.Entries == null ? 0 : quest.Entries.Count;
                if (count == 0)
                    return details + " none";

                for (int i = 0; i < count; i++)
                {
                    QuestEntry entry = quest.Entries[i];
                    if (entry == null)
                        continue;
                    details += "\n" + (entry.QuestEntryIndex + 1) + ". " +
                        entry.Title + " [" + entry.State + "]";
                }
            }
            catch
            {
                details += " unavailable";
            }

            return details;
        }

        private void AddQuestManagerObjects()
        {
            object manager = FindQuestManager();
            if (manager == null)
                return;

            AddQuestSource(ReadMember(manager, "ActiveQuests"), "ActiveQuests", 0);
            AddQuestSource(ReadMember(manager, "DefaultQuests"), "DefaultQuests", 0);
            AddQuestSource(ReadMember(manager, "EmployeeQuests"), "EmployeeQuests", 0);
            AddQuestSource(ReadMember(manager, "DeaddropQuests"), "DeaddropQuests", 0);
            AddQuestSource(ReadMember(manager, "Quests"), "Quests", 0);
            AddQuestSource(ReadMember(manager, "Contracts"), "Contracts", 0);
            AddQuestSource(ReadMember(manager, "ActiveContracts"), "ActiveContracts", 0);
            AddQuestSource(ReadMember(manager, "ScheduledContracts"), "ScheduledContracts", 0);
        }

        private void AddLoadedQuestResources()
        {
            string[] typeNames =
            {
                "Il2CppScheduleOne.Quests.Quest",
                "Il2CppScheduleOne.Quests.Contract",
                "ScheduleOne.Quests.Quest",
                "ScheduleOne.Quests.Contract"
            };

            for (int i = 0; i < typeNames.Length; i++)
            {
                Type type = FindType(typeNames[i]);
                if (type == null)
                    continue;

                object[] objects = FindUnityObjectsOfType(type);
                for (int j = 0; j < objects.Length; j++)
                    AddQuestSource(objects[j], type.Name, 0);
            }
        }

        private void AddQuestHudObjects()
        {
            string[] typeNames =
            {
                "Il2CppScheduleOne.UI.QuestHUDUI",
                "Il2CppScheduleOne.UI.QuestEntryHUDUI",
                "ScheduleOne.UI.QuestHUDUI",
                "ScheduleOne.UI.QuestEntryHUDUI"
            };

            for (int i = 0; i < typeNames.Length; i++)
            {
                Type type = FindType(typeNames[i]);
                if (type == null)
                    continue;

                object[] objects = FindUnityObjectsOfType(type);
                for (int j = 0; j < objects.Length; j++)
                {
                    int before = _quests.Count;
                    AddQuestSource(objects[j], type.Name, 0);
                    if (_quests.Count == before)
                        AddQuestHudSnapshot(objects[j], type.Name);
                }
            }

            if (_quests.Count == 0)
                AddQuestHudObjectsFromSceneScan();
        }

        private void AddQuestHudObjectsFromSceneScan()
        {
            MonoBehaviour[] behaviours;
            try { behaviours = Object.FindObjectsOfType<MonoBehaviour>(true); }
            catch { return; }

            if (behaviours == null)
                return;

            int inspected = 0;
            for (int i = 0; i < behaviours.Length && inspected < 5000; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                inspected++;
                _lastHudObjectsInspected++;
                string typeName = SafeTypeName(behaviour);
                string path = GetHierarchyPath(behaviour.transform);
                if (!LooksLikeQuestHudSource(typeName, path))
                    continue;

                int before = _quests.Count;
                AddQuestSource(behaviour, typeName, 0);
                if (_quests.Count == before)
                    AddQuestHudSnapshot(behaviour, typeName);
            }
        }

        private void AddVisibleHudTextSnapshots()
        {
            MonoBehaviour[] behaviours;
            try { behaviours = Object.FindObjectsOfType<MonoBehaviour>(true); }
            catch { return; }

            if (behaviours == null)
                return;

            var seenText = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pendingTitles = new Queue<PendingHudText>();

            for (int i = 0; i < behaviours.Length && _quests.Count < MaxQuestItems; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                _lastHudObjectsInspected++;

                GameObject gameObject = null;
                try { gameObject = behaviour.gameObject; }
                catch { }
                if (gameObject == null || !gameObject.activeInHierarchy)
                    continue;

                string typeName = SafeTypeName(behaviour);
                if (!LooksLikeTextComponent(typeName))
                    continue;

                string path = GetHierarchyPath(behaviour.transform);
                if (LooksLikeMenuTextPath(path))
                    continue;

                string text = NormalizeQuestText(ReadTextFromObject(behaviour));
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                _lastHudTextCandidates++;

                if (!IsUsefulQuestText(text) || !LooksLikeQuestObjectiveText(text, path))
                    continue;

                if (!seenText.Add(text))
                    continue;

                object actionTarget = FindQuestActionTarget(behaviour);
                if (LooksLikeQuestInstructionText(text))
                {
                    string label = text;
                    object target = actionTarget;
                    while (pendingTitles.Count > 0)
                    {
                        PendingHudText pending = pendingTitles.Dequeue();
                        string title = pending.Text;
                        if (!string.Equals(title, text, StringComparison.OrdinalIgnoreCase))
                        {
                            label = title + " - " + text;
                            if (target == null)
                                target = pending.ActionTarget;
                            break;
                        }
                    }

                    AddHudTextSnapshot(label, target);
                    continue;
                }

                pendingTitles.Enqueue(new PendingHudText
                {
                    Text = text,
                    ActionTarget = actionTarget
                });
                if (pendingTitles.Count > 8)
                {
                    PendingHudText pending = pendingTitles.Dequeue();
                    AddHudTextSnapshot(pending.Text, pending.ActionTarget);
                }
            }

            while (pendingTitles.Count > 0 && _quests.Count < MaxQuestItems)
            {
                PendingHudText pending = pendingTitles.Dequeue();
                AddHudTextSnapshot(pending.Text, pending.ActionTarget);
            }
        }

        private void AddHudTextSnapshot(string label, object actionTarget)
        {
            if (string.IsNullOrWhiteSpace(label))
                return;

            AddQuestRecord(new QuestDisplaySnapshot
            {
                Label = label,
                State = actionTarget == null ? "HUD active" : ReadState(actionTarget),
                Source = "Visible HUD objective",
                ActionTarget = actionTarget
            });

            _lastHudSnapshots++;
        }

        private void AddQuestHudSnapshot(object hudObject, string sourceName)
        {
            string label = ExtractQuestHudText(hudObject);
            if (string.IsNullOrWhiteSpace(label))
                return;

            object actionTarget = FindQuestActionTarget(hudObject);
            AddQuestRecord(new QuestDisplaySnapshot
            {
                Label = label,
                State = actionTarget == null ? "HUD active" : ReadState(actionTarget),
                Source = sourceName ?? "Quest HUD",
                ActionTarget = actionTarget
            });
        }

        private void AddQuestSource(object source, string sourceName, int depth)
        {
            if (source == null || _quests.Count >= MaxQuestItems || depth > 2)
                return;

            if (source is string)
                return;

            Type type = source.GetType();
            if (!IsCollectionLike(type) && LooksLikeQuestItemType(type))
                AddQuestRecord(source);

            if (depth >= 2)
                return;

            AddKnownMembers(source, depth + 1);
            AddEnumerableItems(source, depth + 1);
            AddIndexedItems(source, depth + 1);
        }

        private void AddKnownMembers(object source, int depth)
        {
            string[] memberNames =
            {
                "Quest", "ParentQuest", "PrereqQuest", "PrerequisiteQuests",
                "ActiveQuests", "DefaultQuests", "EmployeeQuests", "DeaddropQuests",
                "Quests", "Contracts", "ActiveContracts", "ScheduledContracts",
                "QuestEntries", "Entries", "QuestEntry", "GenericQuestEntry",
                "ReadMessageQuestEntry", "ReturnToRVQuest", "DealQuest",
                "MeetingQuestEntry", "PurchaseRoomQuests"
            };

            for (int i = 0; i < memberNames.Length; i++)
            {
                object value = ReadMember(source, memberNames[i]);
                if (value != null && !ReferenceEquals(value, source))
                    AddQuestSource(value, memberNames[i], depth);
            }
        }

        private void AddEnumerableItems(object source, int depth)
        {
            IEnumerable enumerable = source as IEnumerable;
            if (enumerable == null)
                return;

            try
            {
                int count = 0;
                foreach (object item in enumerable)
                {
                    if (count++ >= MaxCollectionItems || _quests.Count >= MaxQuestItems)
                        break;
                    AddQuestSource(item, "enumerable", depth);
                }
            }
            catch { }
        }

        private void AddIndexedItems(object source, int depth)
        {
            int count = ReadCount(source);
            if (count <= 0)
                return;

            count = Math.Min(count, MaxCollectionItems);
            for (int i = 0; i < count && _quests.Count < MaxQuestItems; i++)
            {
                object item = ReadIndexedItem(source, i);
                if (item != null)
                    AddQuestSource(item, "indexed", depth);
            }
        }

        private void AddQuestRecord(object quest)
        {
            string key = BuildObjectKey(quest);
            if (!_seen.Add(key))
                return;

            _quests.Add(quest);
            _labels.Add(BuildLabel(quest));
        }

        private void SortQuestRecords()
        {
            if (_quests.Count <= 1)
                return;

            var items = new List<QuestSortItem>(_quests.Count);
            for (int i = 0; i < _quests.Count; i++)
            {
                items.Add(new QuestSortItem
                {
                    Quest = _quests[i],
                    Label = i < _labels.Count ? _labels[i] : BuildLabel(_quests[i]),
                    OriginalIndex = i
                });
            }

            items.Sort((a, b) =>
            {
                int compare = GetQuestSortPriority(a.Quest, a.Label)
                    .CompareTo(GetQuestSortPriority(b.Quest, b.Label));
                if (compare != 0)
                    return compare;

                compare = string.Compare(CleanLabelForSort(a.Label), CleanLabelForSort(b.Label),
                    StringComparison.OrdinalIgnoreCase);
                return compare != 0 ? compare : a.OriginalIndex.CompareTo(b.OriginalIndex);
            });

            _quests.Clear();
            _labels.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                _quests.Add(items[i].Quest);
                _labels.Add(items[i].Label);
            }
        }

        private static int GetQuestSortPriority(object quest, string label)
        {
            string state = ReadState(GetActionTarget(quest)) ?? ReadState(quest);
            string clean = CleanLabelForSort(label);
            int priority = 20;

            if (state.IndexOf("Active", StringComparison.OrdinalIgnoreCase) >= 0 ||
                state.IndexOf("HUD active", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                priority = 0;
            }
            else if (state.IndexOf("Inactive", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                priority = 30;
            }
            else if (state.IndexOf("Completed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                priority = 50;
            }
            else if (state.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     state.IndexOf("Cancelled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     state.IndexOf("Expired", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                priority = 60;
            }

            if (clean.IndexOf("Welcome to Hyland Point", StringComparison.OrdinalIgnoreCase) >= 0)
                priority -= 5;
            else if (clean.IndexOf("Deal for", StringComparison.OrdinalIgnoreCase) >= 0)
                priority += 2;

            return priority;
        }

        private static string CleanLabelForSort(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return string.Empty;

            int bracket = label.LastIndexOf(" [", StringComparison.Ordinal);
            return bracket > 0 ? label.Substring(0, bracket) : label;
        }

        private static bool LooksLikeQuestItemType(Type type)
        {
            if (type == null)
                return false;

            string fullName = type.FullName ?? type.Name;
            string name = type.Name ?? string.Empty;

            if (name.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return fullName.IndexOf(".Quests.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Contract", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeQuestEntryType(Type type)
        {
            if (type == null)
                return false;

            string fullName = type.FullName ?? type.Name ?? string.Empty;
            return fullName.IndexOf(".Quests.", StringComparison.OrdinalIgnoreCase) >= 0 &&
                fullName.IndexOf("QuestEntry", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeQuestType(Type type)
        {
            if (type == null)
                return false;

            string fullName = type.FullName ?? type.Name ?? string.Empty;
            string name = type.Name ?? string.Empty;
            if (name.IndexOf("Manager", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            return fullName.IndexOf(".Quests.", StringComparison.OrdinalIgnoreCase) >= 0 &&
                (name.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 name.IndexOf("Contract", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static object FindQuestActionTarget(object source)
        {
            if (source == null)
                return null;

            object direct = FindDirectQuestActionTarget(source);
            if (direct != null)
                return direct;

            Component component = source as Component;
            if (component == null)
                return null;

            try
            {
                Transform current = component.transform;
                int depth = 0;
                while (current != null && depth++ < 12)
                {
                    MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                    if (behaviours != null)
                    {
                        for (int i = 0; i < behaviours.Length; i++)
                        {
                            object target = FindDirectQuestActionTarget(behaviours[i]);
                            if (target != null)
                                return target;
                        }
                    }

                    current = current.parent;
                }
            }
            catch { }

            return null;
        }

        private static object FindDirectQuestActionTarget(object source)
        {
            if (source == null)
                return null;

            object directEntry = ReadMember(source, "QuestEntry");
            if (directEntry != null && LooksLikeQuestEntryType(directEntry.GetType()))
                return directEntry;

            object directQuest = ReadMember(source, "Quest");
            if (directQuest != null && LooksLikeQuestType(directQuest.GetType()))
                return directQuest;

            Type type = source.GetType();
            if (LooksLikeQuestEntryType(type) || LooksLikeQuestType(type))
                return source;

            return null;
        }

        private static bool IsCollectionLike(Type type)
        {
            if (type == null)
                return false;

            string name = type.FullName ?? type.Name ?? string.Empty;
            return type.IsArray ||
                name.IndexOf("List", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Collection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Dictionary", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("HashSet", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Enumerable", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildLabel(object quest)
        {
            QuestDisplaySnapshot snapshot = quest as QuestDisplaySnapshot;
            if (snapshot != null)
                return snapshot.Label + " [" + snapshot.State + "]";

            Quest nativeQuest = quest as Quest;
            if (nativeQuest != null)
            {
                string nativeTitle = string.IsNullOrWhiteSpace(nativeQuest.Title)
                    ? nativeQuest.name
                    : nativeQuest.Title;
                string objective = GetActiveObjectiveLabel(nativeQuest);
                return nativeTitle + " [" + nativeQuest.State + "]" +
                    (string.IsNullOrWhiteSpace(objective) ? string.Empty : " - " + objective);
            }

            string title = ReadStringMember(quest, "Title") ??
                ReadStringMember(quest, "QuestTitle") ??
                ReadStringMember(quest, "QuestName") ??
                ReadStringMember(quest, "Name") ??
                ReadStringMember(quest, "name");

            Object unityObject = quest as Object;
            if (string.IsNullOrWhiteSpace(title) && unityObject != null)
                title = unityObject.name;
            if (string.IsNullOrWhiteSpace(title))
                title = quest != null ? quest.GetType().Name : "Quest";

            return title + " [" + ReadState(quest) + "]";
        }

        private static string GetActiveObjectiveLabel(Quest quest)
        {
            try
            {
                if (quest.Entries == null)
                    return null;

                for (int i = 0; i < quest.Entries.Count; i++)
                {
                    QuestEntry entry = quest.Entries[i];
                    if (entry != null && entry.State == EQuestState.Active)
                        return entry.Title;
                }
            }
            catch { }

            return null;
        }

        private static string ReadState(object quest)
        {
            QuestDisplaySnapshot snapshot = quest as QuestDisplaySnapshot;
            if (snapshot != null)
                return snapshot.State;

            if (quest == null)
                return "unknown";

            string[] stateMembers =
            {
                "State", "QuestState", "Status", "CurrentState", "ActivationState",
                "QuestEntryState", "EntryState", "IsActive", "Active", "IsComplete",
                "IsCompleted", "Completed", "Started"
            };

            for (int i = 0; i < stateMembers.Length; i++)
            {
                object value = ReadMember(quest, stateMembers[i]);
                if (value != null)
                    return value.ToString();
            }

            return "unknown";
        }

        private static string ReadStringMember(object target, string name)
        {
            object value = ReadMember(target, name);
            return value as string;
        }

        private static object ReadMember(object target, string name)
        {
            if (target == null || string.IsNullOrEmpty(name))
                return null;

            try
            {
                Type type = target.GetType();
                PropertyInfo property = type.GetProperty(
                    name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.GetIndexParameters().Length == 0)
                    return property.GetValue(target, null);

                FieldInfo field = type.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field.GetValue(target);
            }
            catch { }

            return null;
        }

        private static int ReadCount(object source)
        {
            object count = ReadMember(source, "Count") ?? ReadMember(source, "Length");
            if (count == null)
                return -1;

            try { return Convert.ToInt32(count); }
            catch { return -1; }
        }

        private static object ReadIndexedItem(object source, int index)
        {
            if (source == null)
                return null;

            try
            {
                PropertyInfo itemProperty = source.GetType().GetProperty(
                    "Item",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (itemProperty != null)
                    return itemProperty.GetValue(source, new object[] { index });
            }
            catch { }

            try
            {
                MethodInfo getter = source.GetType().GetMethod(
                    "get_Item",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(int) },
                    null);
                return getter?.Invoke(source, new object[] { index });
            }
            catch
            {
                return null;
            }
        }

        private static object FindQuestManager()
        {
            string[] typeNames =
            {
                "Il2CppScheduleOne.Quests.QuestManager",
                "ScheduleOne.Quests.QuestManager"
            };

            for (int i = 0; i < typeNames.Length; i++)
            {
                Type type = FindType(typeNames[i]);
                if (type == null)
                    continue;

                object instance = ReadStaticMember(type, "Instance") ??
                    ReadStaticMember(type, "instance") ??
                    ReadStaticMember(type, "_instance");
                if (instance != null)
                    return instance;

                object[] objects = FindUnityObjectsOfType(type);
                if (objects.Length > 0)
                    return objects[0];
            }

            return null;
        }

        private static object FindQuestByTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            string[] typeNames =
            {
                "Il2CppScheduleOne.Quests.Quest",
                "ScheduleOne.Quests.Quest"
            };

            for (int i = 0; i < typeNames.Length; i++)
            {
                Type type = FindType(typeNames[i]);
                if (type == null)
                    continue;

                object[] objects = FindUnityObjectsOfType(type);
                for (int j = 0; j < objects.Length; j++)
                {
                    object quest = objects[j];
                    string questTitle = BuildLabel(quest);
                    if (questTitle.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                        return quest;
                }
            }

            return null;
        }

        private static object[] FindUnityObjectsOfType(Type type)
        {
            if (type == null)
                return Array.Empty<object>();

            List<object> results = new List<object>();

            try
            {
                MethodInfo[] methods = typeof(Resources).GetMethods(
                    BindingFlags.Static | BindingFlags.Public);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null ||
                        method.Name != "FindObjectsOfTypeAll" ||
                        !method.IsGenericMethodDefinition ||
                        method.GetParameters().Length != 0)
                    {
                        continue;
                    }

                    object raw = method.MakeGenericMethod(type).Invoke(null, null);
                    AddObjectResults(raw, results);
                    if (results.Count > 0)
                        return results.ToArray();
                }
            }
            catch { }

            try
            {
                MethodInfo method = typeof(Resources).GetMethod(
                    "FindObjectsOfTypeAll",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Type) },
                    null);
                object raw = method?.Invoke(null, new object[] { type });
                AddObjectResults(raw, results);
                if (results.Count > 0)
                    return results.ToArray();
            }
            catch { }

            try
            {
                MethodInfo method = typeof(Object).GetMethod(
                    "FindObjectsOfType",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Type), typeof(bool) },
                    null);
                object raw = method?.Invoke(null, new object[] { type, true });
                AddObjectResults(raw, results);
                if (results.Count > 0)
                    return results.ToArray();
            }
            catch { }

            try
            {
                MethodInfo method = typeof(Object).GetMethod(
                    "FindObjectsOfType",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Type) },
                    null);
                object raw = method?.Invoke(null, new object[] { type });
                AddObjectResults(raw, results);
            }
            catch { }

            return results.ToArray();
        }

        private static void AddObjectResults(object raw, List<object> results)
        {
            if (raw == null || results == null)
                return;

            Array array = raw as Array;
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    object value = array.GetValue(i);
                    if (value != null)
                        results.Add(value);
                }

                return;
            }

            IEnumerable enumerable = raw as IEnumerable;
            if (enumerable == null)
                return;

            try
            {
                foreach (object value in enumerable)
                {
                    if (value != null)
                        results.Add(value);
                }
            }
            catch { }
        }

        private static Type FindType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type;
                try { type = assemblies[i].GetType(fullName, false); }
                catch { type = null; }
                if (type != null)
                    return type;
            }

            string shortName = fullName;
            int dot = shortName.LastIndexOf('.');
            if (dot >= 0 && dot + 1 < shortName.Length)
                shortName = shortName.Substring(dot + 1);

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] types;
                try { types = assemblies[i].GetTypes(); }
                catch { continue; }

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    string name = type?.FullName ?? string.Empty;
                    if (name.EndsWith("." + shortName, StringComparison.OrdinalIgnoreCase))
                        return type;
                }
            }

            return null;
        }

        private static string ExtractQuestHudText(object hudObject)
        {
            var candidates = new List<string>();
            CollectQuestTextCandidates(hudObject, candidates);

            string first = null;
            string second = null;
            for (int i = 0; i < candidates.Count; i++)
            {
                string value = NormalizeQuestText(candidates[i]);
                if (!IsUsefulQuestText(value))
                    continue;

                if (first == null)
                {
                    first = value;
                    continue;
                }

                if (second == null && !string.Equals(first, value, StringComparison.OrdinalIgnoreCase))
                {
                    second = value;
                    break;
                }
            }

            if (first == null)
                return null;
            return second == null ? first : first + " - " + second;
        }

        private static void CollectQuestTextCandidates(object target, List<string> candidates)
        {
            if (target == null || candidates == null || candidates.Count >= 12)
                return;

            string direct = target as string;
            if (direct != null)
            {
                candidates.Add(direct);
                return;
            }

            Type type = target.GetType();
            MemberInfo[] members;
            try { members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); }
            catch { return; }

            for (int i = 0; i < members.Length && candidates.Count < 12; i++)
            {
                MemberInfo member = members[i];
                string name = member?.Name ?? string.Empty;
                if (!LooksLikeTextMember(name))
                    continue;

                object value = null;
                try
                {
                    PropertyInfo property = member as PropertyInfo;
                    if (property != null && property.GetIndexParameters().Length == 0)
                        value = property.GetValue(target, null);
                    FieldInfo field = member as FieldInfo;
                    if (field != null)
                        value = field.GetValue(target);
                }
                catch { }

                AddTextCandidate(value, candidates);
            }

            Component component = target as Component;
            if (component == null || candidates.Count >= 12)
                return;

            MonoBehaviour[] children = null;
            try { children = component.GetComponentsInChildren<MonoBehaviour>(true); }
            catch { }
            if (children == null)
                return;

            for (int i = 0; i < children.Length && candidates.Count < 12; i++)
            {
                MonoBehaviour child = children[i];
                if (child == null)
                    continue;

                string childType = SafeTypeName(child);
                string childPath = GetHierarchyPath(child.transform);
                if (!LooksLikeQuestHudSource(childType, childPath) &&
                    !LooksLikeTextComponent(childType))
                {
                    continue;
                }

                AddTextCandidate(child, candidates);
            }
        }

        private static bool LooksLikeTextComponent(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return false;

            return typeName.IndexOf("TextMeshPro", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("TMP_Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.EndsWith(".Text", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeQuestHudSource(string typeName, string hierarchyPath)
        {
            string text = (typeName ?? string.Empty) + " " + (hierarchyPath ?? string.Empty);
            return text.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Objective", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Mission", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0 &&
                text.IndexOf("Entry", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeMenuTextPath(string hierarchyPath)
        {
            if (string.IsNullOrEmpty(hierarchyPath))
                return false;

            return hierarchyPath.IndexOf("Nugzz", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hierarchyPath.IndexOf("NugzzMenu", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeQuestObjectiveText(string text, string hierarchyPath)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string combined = text + " " + (hierarchyPath ?? string.Empty);
            if (combined.IndexOf("Quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("Objective", StringComparison.OrdinalIgnoreCase) >= 0 ||
                combined.IndexOf("Mission", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string[] cues =
            {
                "Welcome to Hyland Point", "Use the management clipboard", "Open your phone",
                "Investigate", "assign the", "Go to ", "Talk to ", "Meet ", "Deliver ",
                "Collect ", "Purchase ", "Buy ", "Sell ", "Return ", "Read ", "Harvest ",
                "Water ", "Plant ", "Cook ", "Package ", "Mix ", "Reach ", "Sleep ",
                "Wait ", "Clean ", "Repair ", "Find ", "Deal for", "Botanists", "Handlers",
                "Chemists", "Cleaners"
            };

            for (int i = 0; i < cues.Length; i++)
            {
                if (combined.IndexOf(cues[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static bool LooksLikeQuestInstructionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string[] cues =
            {
                "Use ", "Open ", "Go ", "Talk ", "Meet ", "Deliver ", "Collect ", "Purchase ",
                "Buy ", "Sell ", "Return ", "Read ", "Harvest ", "Water ", "Plant ", "Cook ",
                "Package ", "Mix ", "Reach ", "Sleep ", "Wait ", "Clean ", "Repair ", "Find ",
                "Investigate", "assign "
            };

            for (int i = 0; i < cues.Length; i++)
            {
                if (text.IndexOf(cues[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static bool LooksLikeTextMember(string name)
        {
            return name.IndexOf("Text", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Description", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Subtitle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void AddTextCandidate(object value, List<string> candidates)
        {
            if (value == null || candidates == null)
                return;

            string direct = value as string;
            if (direct != null)
            {
                candidates.Add(direct);
                return;
            }

            object text = ReadMember(value, "text") ?? ReadMember(value, "Text");
            string textString = text as string;
            if (textString != null)
                candidates.Add(textString);
        }

        private static string ReadTextFromObject(object value)
        {
            if (value == null)
                return null;

            string direct = value as string;
            if (direct != null)
                return direct;

            object text = ReadMember(value, "text") ?? ReadMember(value, "Text");
            return text as string;
        }

        private static string NormalizeQuestText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Replace("\r", " ").Replace("\n", " ").Trim();
            while (text.IndexOf("  ", StringComparison.Ordinal) >= 0)
                text = text.Replace("  ", " ");
            return text;
        }

        private static bool IsUsefulQuestText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 4 || text.Length > 180)
                return false;

            if (text.IndexOf("No active", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Quest Control", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Nugzz", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Status:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("No quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Refresh Quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Selected Quest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Live Quests", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Manual controls", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("scene-specific", StringComparison.OrdinalIgnoreCase) >= 0 ||
                text.IndexOf("Page ", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return false;
            }

            if (string.Equals(text, "Complete", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Start / Activate", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Cancel / Fail", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Prev Page", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "Next Page", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            try
            {
                string path = transform.name ?? string.Empty;
                Transform current = transform.parent;
                int depth = 0;
                while (current != null && depth++ < 16)
                {
                    path = (current.name ?? string.Empty) + "/" + path;
                    current = current.parent;
                }

                return path;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string BuildObjectKey(object value)
        {
            if (value == null)
                return "null";

            QuestDisplaySnapshot snapshot = value as QuestDisplaySnapshot;
            if (snapshot != null)
            {
                string targetKey = snapshot.ActionTarget == null
                    ? "hud"
                    : BuildObjectKey(snapshot.ActionTarget);
                return "snapshot:" + targetKey + ":" + (snapshot.Label ?? string.Empty);
            }

            Object unityObject = value as Object;
            if (unityObject != null)
            {
                try { return "u:" + unityObject.GetInstanceID(); }
                catch { }
            }

            return "r:" + value.GetType().FullName + ":" +
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }

        private static string SafeTypeName(object value)
        {
            QuestDisplaySnapshot snapshot = value as QuestDisplaySnapshot;
            if (snapshot != null)
                return snapshot.Source;

            try { return value != null ? value.GetType().FullName : "null"; }
            catch { return "unknown"; }
        }

        private static bool InvokeQuestAction(object quest, string[] names)
        {
            if (quest == null || names == null)
                return false;

            Type type = quest.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                MethodInfo method = FindNoArgMethod(type, names[i]);
                if (method == null)
                    continue;

                try
                {
                    object[] args = BuildInvocationArgs(method);
                    method.Invoke(quest, args);
                    return true;
                }
                catch { }
            }

            return false;
        }

        private static MethodInfo FindNoArgMethod(Type type, string name)
        {
            try
            {
                MethodInfo[] methods = type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null || !CanInvokeSimpleQuestMethod(method))
                        continue;
                    if (string.Equals(method.Name, name, StringComparison.OrdinalIgnoreCase))
                        return method;
                }
            }
            catch { }

            return null;
        }

        private static bool CanInvokeSimpleQuestMethod(MethodInfo method)
        {
            ParameterInfo[] parameters;
            try { parameters = method.GetParameters(); }
            catch { return false; }

            if (parameters.Length == 0)
                return true;
            if (parameters.Length != 1)
                return false;

            Type parameterType = parameters[0].ParameterType;
            return parameterType == typeof(bool) || parameterType.FullName == "System.Boolean";
        }

        private static object[] BuildInvocationArgs(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
                return null;
            return new object[] { true };
        }

        private static bool StartQuestObject(object target)
        {
            if (target == null)
                return false;

            Quest quest = target as Quest;
            if (quest != null)
            {
                quest.Begin(true);
                return true;
            }

            if (InvokeQuestAction(target, StartMethodNames))
                return true;

            return SetQuestObjectState(target, "Active");
        }

        private static bool CompleteQuestObject(object target)
        {
            if (target == null)
                return false;

            Quest quest = target as Quest;
            if (quest != null)
            {
                quest.Complete(true);
                return true;
            }

            if (InvokeQuestAction(target, CompleteMethodNames))
                return true;

            return SetQuestObjectState(target, "Completed");
        }

        private static bool EndQuestObject(object target)
        {
            if (target == null)
                return false;

            Quest quest = target as Quest;
            if (quest != null)
            {
                quest.End();
                return true;
            }

            if (InvokeQuestAction(target, EndMethodNames))
                return true;

            return SetQuestObjectState(target, "Cancelled");
        }

        private static bool ResetQuestObject(object target)
        {
            Quest quest = target as Quest;
            if (quest != null)
            {
                quest.SetQuestState(EQuestState.Inactive, true);
                return true;
            }

            return SetQuestObjectState(target, "Inactive");
        }

        private static bool SetQuestObjectState(object target, string stateName)
        {
            if (target == null || string.IsNullOrEmpty(stateName))
                return false;

            object state = CreateQuestStateValue(target, stateName);
            if (state == null)
                return false;

            if (LooksLikeQuestEntryType(target.GetType()))
            {
                if (InvokeMethod(target, "SetState", new[] { state, (object)true }))
                    return true;

                object parent = ReadMember(target, "ParentQuest");
                object index = ReadMember(target, "QuestEntryIndex");
                if (parent != null && index != null)
                {
                    try
                    {
                        return InvokeMethod(parent, "SetQuestEntryState",
                            new[] { Convert.ToInt32(index), state, (object)true });
                    }
                    catch { }
                }

                return false;
            }

            if (LooksLikeQuestType(target.GetType()))
                return InvokeMethod(target, "SetQuestState", new[] { state, (object)true });

            return false;
        }

        private static object CreateQuestStateValue(object target, string stateName)
        {
            Type stateType = FindType("Il2CppScheduleOne.Quests.EQuestState") ??
                FindType("ScheduleOne.Quests.EQuestState");

            if (stateType == null)
            {
                MethodInfo method = FindMethod(target.GetType(), "SetQuestState", 2) ??
                    FindMethod(target.GetType(), "SetState", 2);
                ParameterInfo[] parameters = method?.GetParameters();
                if (parameters != null && parameters.Length > 0)
                    stateType = parameters[0].ParameterType;
            }

            if (stateType == null || !stateType.IsEnum)
                return null;

            try { return Enum.Parse(stateType, stateName, true); }
            catch { return null; }
        }

        private static bool InvokeMethod(object target, string methodName, object[] args)
        {
            if (target == null || string.IsNullOrEmpty(methodName))
                return false;

            MethodInfo method = FindMethod(target.GetType(), methodName, args?.Length ?? 0);
            if (method == null)
                return false;

            try
            {
                method.Invoke(target, args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static MethodInfo FindMethod(Type type, string methodName, int parameterCount)
        {
            if (type == null)
                return null;

            try
            {
                MethodInfo[] methods = type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method == null ||
                        !string.Equals(method.Name, methodName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == parameterCount)
                        return method;
                }
            }
            catch { }

            return null;
        }

        private static bool TrySetVariable(string name, string value)
        {
            bool changed = false;

            try
            {
                object player = ManagerCacheService.Instance.LocalPlayer;
                changed |= InvokeSetVariable(player, name, value);
            }
            catch { }

            try
            {
                object database = FindVariableDatabase();
                changed |= InvokeSetVariable(database, name, value);
            }
            catch { }

            return changed;
        }

        private static bool InvokeSetVariable(object target, string name, string value)
        {
            if (target == null)
                return false;

            try
            {
                MethodInfo method = target.GetType().GetMethod(
                    "SetVariableValue",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string), typeof(string), typeof(bool) },
                    null);
                if (method == null)
                    return false;

                method.Invoke(target, new object[] { name, value, true });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object FindVariableDatabase()
        {
            try
            {
                Type type = typeof(VariableDatabase);
                object instance = ReadStaticMember(type, "Instance");
                if (instance != null)
                    return instance;

                VariableDatabase[] objects = Resources.FindObjectsOfTypeAll<VariableDatabase>();
                return objects != null && objects.Length > 0 ? objects[0] : null;
            }
            catch
            {
                return null;
            }
        }

        private static object ReadStaticMember(Type type, string name)
        {
            try
            {
                PropertyInfo property = type.GetProperty(
                    name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                    return property.GetValue(null, null);

                FieldInfo field = type.GetField(
                    name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return field?.GetValue(null);
            }
            catch
            {
                return null;
            }
        }

        private static bool LooksLikeWelcomeExplosion(string label)
        {
            if (string.IsNullOrEmpty(label))
                return false;

            return label.IndexOf("Welcome", StringComparison.OrdinalIgnoreCase) >= 0 ||
                label.IndexOf("Hyland", StringComparison.OrdinalIgnoreCase) >= 0 ||
                label.IndexOf("Explosion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                label.IndexOf("RV", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static readonly string[] CompleteMethodNames =
        {
            "Complete", "CompleteQuest", "Complete_Server", "Finish",
            "FinishQuest", "Succeed", "CompleteEntry"
        };

        private static readonly string[] StartMethodNames =
        {
            "Begin", "StartQuest", "Activate", "Start", "Initialize",
            "InitializeQuest", "CheckQuestStart"
        };

        private static readonly string[] EndMethodNames =
        {
            "End", "EndQuest", "Cancel", "CancelQuest", "Deactivate",
            "DestroyJournalEntry"
        };
    }
}
