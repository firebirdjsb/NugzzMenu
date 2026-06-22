using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Main-menu-only helpers for inspecting and editing Schedule I save files.
    /// All risky operations are reversible: save deletion archives the folder instead
    /// of removing it permanently, and Steam Cloud handling only renames the local
    /// autocloud marker file inside the save profile.
    /// </summary>
    public sealed class SaveManagementService
    {
        private static readonly SaveManagementService _instance = new SaveManagementService();
        public static SaveManagementService Instance => _instance;

        private const string SavePrefix = "SaveGame_";
        private const string CloudMarkerFile = "steam_autocloud.vdf";
        private const string DisabledCloudMarkerFile = "steam_autocloud.vdf.nugzz.disabled";

        private readonly List<SaveSlotSummary> _saveSlots = new List<SaveSlotSummary>();
        private int _selectedSlotNumber = 1;
        private bool _deleteConfirm;
        private bool _cloudConfirm;
        private string _currentSceneName = "";
        private bool _isMainMenu;
        private bool _cloudMarkerPresent;
        private bool _disabledCloudMarkerPresent;
        private string _organisationDraft = "";
        private string _onlineBalanceDraft = "";
        private string _netWorthDraft = "";
        private string _lifetimeEarningsDraft = "";

        public IReadOnlyList<SaveSlotSummary> SaveSlots => _saveSlots;
        public string SaveRootPath { get; private set; } = "";
        public string ActiveProfilePath { get; private set; } = "";
        public string StatusMessage { get; private set; } = "Save tools ready.";
        public int SelectedSlotNumber => _selectedSlotNumber;
        public bool DeleteConfirmPending => _deleteConfirm;
        public bool CloudConfirmPending => _cloudConfirm;

        public string OrganisationDraft
        {
            get => _organisationDraft;
            set => _organisationDraft = value ?? "";
        }

        public string OnlineBalanceDraft
        {
            get => _onlineBalanceDraft;
            set => _onlineBalanceDraft = value ?? "";
        }

        public string NetWorthDraft
        {
            get => _netWorthDraft;
            set => _netWorthDraft = value ?? "";
        }

        public string LifetimeEarningsDraft
        {
            get => _lifetimeEarningsDraft;
            set => _lifetimeEarningsDraft = value ?? "";
        }

        public bool IsMainMenu
        {
            get
            {
                return _isMainMenu;
            }
        }

        public SaveSlotSummary SelectedSlot
        {
            get
            {
                for (int i = 0; i < _saveSlots.Count; i++)
                {
                    if (_saveSlots[i].SlotNumber == _selectedSlotNumber)
                        return _saveSlots[i];
                }

                return null;
            }
        }

        public bool CloudMarkerPresent => _cloudMarkerPresent;

        public bool DisabledCloudMarkerPresent => _disabledCloudMarkerPresent;

        private SaveManagementService() { }

        public void SetCurrentScene(string sceneName, bool probablyMainMenu = false)
        {
            _currentSceneName = sceneName ?? "";
            _isMainMenu = probablyMainMenu ||
                string.Equals(_currentSceneName, "Menu", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentSceneName, "MainMenu", StringComparison.OrdinalIgnoreCase);
        }

        public void EnsureFresh()
        {
            if (_saveSlots.Count > 0)
                return;

            Refresh();
        }

        public void Refresh()
        {
            try
            {
                SaveRootPath = ResolveSaveRootPath();
                ActiveProfilePath = ResolveActiveProfilePath(SaveRootPath);
                _saveSlots.Clear();

                for (int slot = 1; slot <= 5; slot++)
                {
                    string slotPath = string.IsNullOrEmpty(ActiveProfilePath)
                        ? ""
                        : Path.Combine(ActiveProfilePath, SavePrefix + slot.ToString(CultureInfo.InvariantCulture));
                    _saveSlots.Add(ReadSaveSlot(slot, slotPath));
                }

                if (_selectedSlotNumber < 1 || _selectedSlotNumber > 5)
                    _selectedSlotNumber = 1;

                RefreshCloudMarkerState();
                LoadDraftsFromSelectedSlot();
            }
            catch (Exception ex)
            {
                Report("Save refresh failed: " + ex.Message);
            }
        }

        public void SelectSlot(int slotNumber)
        {
            _selectedSlotNumber = Math.Max(1, Math.Min(5, slotNumber));
            _deleteConfirm = false;
            LoadDraftsFromSelectedSlot();
        }

        public void BackupSelectedSave()
        {
            SaveSlotSummary slot = SelectedSlot;
            if (!CanEditSelectedSlot(slot))
                return;

            try
            {
                string destination = GetUniqueArchivePath("NugzzManual", slot);
                CopyDirectory(slot.Path, destination);
                Report("Backed up Slot " + slot.SlotNumber + ".");
            }
            catch (Exception ex)
            {
                Report("Backup failed: " + ex.Message);
            }
        }

        public void ArchiveDeleteSelectedSave()
        {
            SaveSlotSummary slot = SelectedSlot;
            if (!CanEditSelectedSlot(slot))
                return;

            if (!_deleteConfirm)
            {
                _deleteConfirm = true;
                Report("Click Delete again to archive Slot " + slot.SlotNumber + ".");
                return;
            }

            try
            {
                string destination = GetUniqueArchivePath("NugzzDeleted", slot);
                Directory.Move(slot.Path, destination);
                _deleteConfirm = false;
                Report("Archived Slot " + slot.SlotNumber + " to Backups.");
                Refresh();
            }
            catch (Exception ex)
            {
                _deleteConfirm = false;
                Report("Archive delete failed: " + ex.Message);
            }
        }

        public void SetPlayTutorial(bool enabled)
        {
            SaveSlotSummary slot = SelectedSlot;
            if (!CanEditSelectedSlot(slot))
                return;

            try
            {
                string path = Path.Combine(slot.Path, "Metadata.json");
                string json = LoadJsonText(path);
                json = UpdateJsonBoolProperty(json, "PlayTutorial", enabled);
                WriteJsonText(path, json);
                Report("Slot " + slot.SlotNumber + " tutorial flag " + (enabled ? "enabled." : "disabled."));
                Refresh();
            }
            catch (Exception ex)
            {
                Report("Tutorial flag update failed: " + ex.Message);
            }
        }

        public void ApplyOrganisationName()
        {
            SaveSlotSummary slot = SelectedSlot;
            if (!CanEditSelectedSlot(slot))
                return;

            string name = (_organisationDraft ?? "").Trim();
            if (name.Length == 0)
            {
                Report("Organisation name cannot be blank.");
                return;
            }

            try
            {
                string path = Path.Combine(slot.Path, "Game.json");
                string json = LoadJsonText(path);
                json = UpdateJsonStringProperty(json, "OrganisationName", name);
                WriteJsonText(path, json);
                Report("Slot " + slot.SlotNumber + " organisation updated.");
                Refresh();
            }
            catch (Exception ex)
            {
                Report("Organisation update failed: " + ex.Message);
            }
        }

        public void ApplyMoneyValues()
        {
            SaveSlotSummary slot = SelectedSlot;
            if (!CanEditSelectedSlot(slot))
                return;

            if (!TryParseFloat(_onlineBalanceDraft, out float online) ||
                !TryParseFloat(_netWorthDraft, out float netWorth) ||
                !TryParseFloat(_lifetimeEarningsDraft, out float lifetime))
            {
                Report("Money values must be numbers.");
                return;
            }

            try
            {
                string path = Path.Combine(slot.Path, "Money.json");
                string json = LoadJsonText(path);
                json = UpdateJsonNumberProperty(json, "OnlineBalance", online);
                json = UpdateJsonNumberProperty(json, "Networth", netWorth);
                json = UpdateJsonNumberProperty(json, "LifetimeEarnings", lifetime);
                WriteJsonText(path, json);
                Report("Slot " + slot.SlotNumber + " money data updated.");
                Refresh();
            }
            catch (Exception ex)
            {
                Report("Money update failed: " + ex.Message);
            }
        }

        public void ToggleLocalSteamCloudMarker()
        {
            if (!IsMainMenu)
            {
                Report("Save tools are locked outside the main menu.");
                return;
            }

            if (string.IsNullOrEmpty(ActiveProfilePath) || !Directory.Exists(ActiveProfilePath))
            {
                Report("No active save profile found.");
                return;
            }

            try
            {
                string marker = Path.Combine(ActiveProfilePath, CloudMarkerFile);
                string disabled = Path.Combine(ActiveProfilePath, DisabledCloudMarkerFile);

                if (File.Exists(marker))
                {
                    if (!_cloudConfirm)
                    {
                        _cloudConfirm = true;
                        Report("Click again to rename the local Steam Cloud marker.");
                        return;
                    }

                    File.Move(marker, GetUniqueFilePath(disabled));
                    _cloudConfirm = false;
                    RefreshCloudMarkerState();
                    Report("Local Steam Cloud marker disabled.");
                }
                else if (File.Exists(disabled))
                {
                    File.Move(disabled, marker);
                    _cloudConfirm = false;
                    RefreshCloudMarkerState();
                    Report("Local Steam Cloud marker restored.");
                }
                else
                {
                    _cloudConfirm = false;
                    Report("No Steam Cloud marker file found.");
                }
            }
            catch (Exception ex)
            {
                _cloudConfirm = false;
                Report("Steam Cloud marker update failed: " + ex.Message);
            }
        }

        private bool CanEditSelectedSlot(SaveSlotSummary slot)
        {
            if (!IsMainMenu)
            {
                Report("Save editing is only available at the main menu.");
                return false;
            }

            if (slot == null || !slot.Exists)
            {
                Report("Select an existing save slot first.");
                return false;
            }

            return true;
        }

        private void LoadDraftsFromSelectedSlot()
        {
            SaveSlotSummary slot = SelectedSlot;
            if (slot == null || !slot.Exists)
            {
                _organisationDraft = "";
                _onlineBalanceDraft = "";
                _netWorthDraft = "";
                _lifetimeEarningsDraft = "";
                return;
            }

            _organisationDraft = slot.OrganisationName ?? "";
            _onlineBalanceDraft = FormatFloat(slot.OnlineBalance);
            _netWorthDraft = FormatFloat(slot.NetWorth);
            _lifetimeEarningsDraft = FormatFloat(slot.LifetimeEarnings);
        }

        private static SaveSlotSummary ReadSaveSlot(int slotNumber, string path)
        {
            var summary = new SaveSlotSummary
            {
                SlotNumber = slotNumber,
                Path = path ?? "",
                Exists = !string.IsNullOrEmpty(path) && Directory.Exists(path)
            };

            if (!summary.Exists)
                return summary;

            try
            {
                string game = TryLoadJsonText(Path.Combine(path, "Game.json"));
                if (!string.IsNullOrEmpty(game))
                    summary.OrganisationName = ReadJsonStringProperty(game, "OrganisationName", "Unknown Organisation");

                string metadata = TryLoadJsonText(Path.Combine(path, "Metadata.json"));
                if (!string.IsNullOrEmpty(metadata))
                {
                    summary.PlayTutorial = ReadJsonBoolProperty(metadata, "PlayTutorial", false);
                    summary.LastSaveVersion = ReadJsonStringProperty(metadata, "LastSaveVersion", "");
                    summary.LastPlayedLabel = ReadDateLabel(ReadJsonObjectProperty(metadata, "LastPlayedDate"));
                }

                string money = TryLoadJsonText(Path.Combine(path, "Money.json"));
                if (!string.IsNullOrEmpty(money))
                {
                    summary.OnlineBalance = ReadJsonFloatProperty(money, "OnlineBalance", 0f);
                    summary.NetWorth = ReadJsonFloatProperty(money, "Networth", 0f);
                    summary.LifetimeEarnings = ReadJsonFloatProperty(money, "LifetimeEarnings", 0f);
                }

                if (string.IsNullOrEmpty(summary.OrganisationName))
                    summary.OrganisationName = "Slot " + slotNumber;
            }
            catch (Exception ex)
            {
                summary.ReadError = ex.Message;
            }

            return summary;
        }

        private string GetUniqueArchivePath(string archiveKind, SaveSlotSummary slot)
        {
            string backupRoot = Path.Combine(ActiveProfilePath, "Backups", archiveKind);
            Directory.CreateDirectory(backupRoot);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string basePath = Path.Combine(backupRoot, SavePrefix + slot.SlotNumber + "_" + timestamp);
            string path = basePath;
            int suffix = 2;
            while (Directory.Exists(path))
            {
                path = basePath + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }

            return path;
        }

        private static string ResolveSaveRootPath()
        {
            string profile = Environment.GetEnvironmentVariable("USERPROFILE");
            if (string.IsNullOrEmpty(profile))
                profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(profile, "AppData", "LocalLow", "TVGS", "Schedule I", "Saves");
        }

        private void RefreshCloudMarkerState()
        {
            _cloudMarkerPresent = false;
            _disabledCloudMarkerPresent = false;

            try
            {
                if (string.IsNullOrEmpty(ActiveProfilePath))
                    return;

                _cloudMarkerPresent = File.Exists(Path.Combine(ActiveProfilePath, CloudMarkerFile));
                _disabledCloudMarkerPresent = File.Exists(Path.Combine(ActiveProfilePath, DisabledCloudMarkerFile));
            }
            catch { }
        }

        private static string ResolveActiveProfilePath(string saveRoot)
        {
            if (string.IsNullOrEmpty(saveRoot) || !Directory.Exists(saveRoot))
                return "";

            string best = "";
            DateTime bestWriteTime = DateTime.MinValue;
            string[] dirs = Directory.GetDirectories(saveRoot);
            for (int i = 0; i < dirs.Length; i++)
            {
                string dir = dirs[i];
                if (!LooksLikeSaveProfile(dir))
                    continue;

                DateTime writeTime = Directory.GetLastWriteTimeUtc(dir);
                if (best.Length == 0 || writeTime > bestWriteTime)
                {
                    best = dir;
                    bestWriteTime = writeTime;
                }
            }

            return best;
        }

        private static bool LooksLikeSaveProfile(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return false;

            if (File.Exists(Path.Combine(path, CloudMarkerFile)) ||
                File.Exists(Path.Combine(path, DisabledCloudMarkerFile)))
                return true;

            for (int slot = 1; slot <= 5; slot++)
            {
                if (Directory.Exists(Path.Combine(path, SavePrefix + slot.ToString(CultureInfo.InvariantCulture))))
                    return true;
            }

            return false;
        }

        private static string LoadJsonText(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Missing save file", path);

            return File.ReadAllText(path);
        }

        private static string TryLoadJsonText(string path)
        {
            if (!File.Exists(path))
                return null;

            return File.ReadAllText(path);
        }

        private static void WriteJsonText(string path, string json)
        {
            File.WriteAllText(path, EnsureTrailingNewLine(json));
        }

        private static string ReadJsonStringProperty(string json, string key, string fallback)
        {
            if (!TryFindJsonPropertyValue(json, key, out int start, out int end) ||
                start < 0 ||
                start >= json.Length ||
                json[start] != '"')
            {
                return fallback;
            }

            return TryReadJsonString(json, start, end, out string value) ? value : fallback;
        }

        private static bool ReadJsonBoolProperty(string json, string key, bool fallback)
        {
            if (!TryFindJsonPropertyValue(json, key, out int start, out int end))
                return fallback;

            string value = json.Substring(start, Math.Max(0, end - start)).Trim();
            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                return false;
            return fallback;
        }

        private static float ReadJsonFloatProperty(string json, string key, float fallback)
        {
            if (!TryFindJsonPropertyValue(json, key, out int start, out int end))
                return fallback;

            string value = json.Substring(start, Math.Max(0, end - start)).Trim();
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                ? result
                : fallback;
        }

        private static string ReadJsonObjectProperty(string json, string key)
        {
            if (!TryFindJsonPropertyValue(json, key, out int start, out int end) ||
                start < 0 ||
                start >= json.Length ||
                json[start] != '{')
            {
                return "";
            }

            return json.Substring(start, Math.Max(0, end - start));
        }

        private static string UpdateJsonStringProperty(string json, string key, string value)
        {
            return ReplaceOrAppendJsonProperty(json, key, "\"" + EscapeJsonString(value) + "\"");
        }

        private static string UpdateJsonBoolProperty(string json, string key, bool value)
        {
            return ReplaceOrAppendJsonProperty(json, key, value ? "true" : "false");
        }

        private static string UpdateJsonNumberProperty(string json, string key, float value)
        {
            return ReplaceOrAppendJsonProperty(json, key, value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static string ReadDateLabel(string dateJson)
        {
            if (string.IsNullOrEmpty(dateJson))
                return "Unknown";

            try
            {
                int month = (int)ReadJsonFloatProperty(dateJson, "Month", 0f);
                int day = (int)ReadJsonFloatProperty(dateJson, "Day", 0f);
                int year = (int)ReadJsonFloatProperty(dateJson, "Year", 0f);
                int hour = (int)ReadJsonFloatProperty(dateJson, "Hour", 0f);
                int minute = (int)ReadJsonFloatProperty(dateJson, "Minute", 0f);
                if (year <= 0 || month <= 0 || day <= 0)
                    return "Unknown";

                return string.Format(CultureInfo.InvariantCulture,
                    "{0:0000}-{1:00}-{2:00} {3:00}:{4:00}",
                    year, month, day, hour, minute);
            }
            catch
            {
                return "Unknown";
            }
        }

        private static bool TryFindJsonPropertyValue(string json, string key, out int valueStart, out int valueEnd)
        {
            valueStart = -1;
            valueEnd = -1;

            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return false;

            string quotedKey = "\"" + key + "\"";
            int keyIndex = json.IndexOf(quotedKey, StringComparison.Ordinal);
            if (keyIndex < 0)
                return false;

            int colon = json.IndexOf(':', keyIndex + quotedKey.Length);
            if (colon < 0)
                return false;

            valueStart = SkipWhitespace(json, colon + 1);
            if (valueStart < 0 || valueStart >= json.Length)
                return false;

            valueEnd = FindJsonValueEnd(json, valueStart);
            return valueEnd > valueStart;
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length && char.IsWhiteSpace(text[index]))
                index++;
            return index;
        }

        private static int FindJsonValueEnd(string json, int start)
        {
            if (start < 0 || start >= json.Length)
                return start;

            char first = json[start];
            if (first == '"')
                return FindJsonStringEnd(json, start) + 1;
            if (first == '{')
                return FindBalancedEnd(json, start, '{', '}') + 1;
            if (first == '[')
                return FindBalancedEnd(json, start, '[', ']') + 1;

            int index = start;
            while (index < json.Length && json[index] != ',' && json[index] != '}' && json[index] != '\r' && json[index] != '\n')
                index++;
            return index;
        }

        private static int FindJsonStringEnd(string json, int quoteStart)
        {
            bool escaped = false;
            for (int i = quoteStart + 1; i < json.Length; i++)
            {
                char c = json[i];
                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                    return i;
            }

            return json.Length - 1;
        }

        private static int FindBalancedEnd(string json, int start, char open, char close)
        {
            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (c == '\\')
                    {
                        escaped = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    continue;
                }

                if (c == open)
                    depth++;
                else if (c == close)
                {
                    depth--;
                    if (depth <= 0)
                        return i;
                }
            }

            return json.Length - 1;
        }

        private static bool TryReadJsonString(string json, int start, int end, out string value)
        {
            value = "";
            if (start < 0 || start >= json.Length || json[start] != '"')
                return false;

            int stringEnd = Math.Min(end - 1, FindJsonStringEnd(json, start));
            if (stringEnd <= start)
                return false;

            var builder = new StringBuilder();
            bool escaped = false;
            for (int i = start + 1; i < stringEnd; i++)
            {
                char c = json[i];
                if (!escaped)
                {
                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    builder.Append(c);
                    continue;
                }

                escaped = false;
                switch (c)
                {
                    case '"':
                    case '\\':
                    case '/':
                        builder.Append(c);
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            value = builder.ToString();
            return true;
        }

        private static string ReplaceOrAppendJsonProperty(string json, string key, string rawValue)
        {
            if (TryFindJsonPropertyValue(json, key, out int start, out int end))
                return json.Substring(0, start) + rawValue + json.Substring(end);

            int insertIndex = json.LastIndexOf('}');
            if (insertIndex < 0)
                return json;

            string prefix = json.Substring(0, insertIndex).TrimEnd();
            bool needsComma = prefix.Length > 0 && prefix[prefix.Length - 1] != '{' && prefix[prefix.Length - 1] != ',';
            string comma = needsComma ? "," : "";
            return prefix + comma + Environment.NewLine + "    \"" + key + "\": " + rawValue + Environment.NewLine + json.Substring(insertIndex);
        }

        private static string EscapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            var builder = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        private static string EnsureTrailingNewLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Environment.NewLine;
            if (text.EndsWith("\n", StringComparison.Ordinal))
                return text;
            return text + Environment.NewLine;
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                string relative = directory.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }

            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target) ?? destination);
                File.Copy(file, target, false);
            }
        }

        private static string GetUniqueFilePath(string path)
        {
            if (!File.Exists(path))
                return path;

            string directory = Path.GetDirectoryName(path) ?? "";
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            return Path.Combine(directory, name + "_" + timestamp + extension);
        }

        private void Report(string message)
        {
            StatusMessage = message ?? "";
            NotificationService.Instance.Status(StatusMessage);
        }
    }

    public sealed class SaveSlotSummary
    {
        public int SlotNumber;
        public string Path = "";
        public bool Exists;
        public string OrganisationName = "";
        public bool PlayTutorial;
        public string LastSaveVersion = "";
        public string LastPlayedLabel = "";
        public float OnlineBalance;
        public float NetWorth;
        public float LifetimeEarnings;
        public string ReadError = "";
    }
}
