using System;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    public class SettingsState
    {
        public string MenuKeybind { get; set; } = "F8";
        public bool UseGameStackLogic { get; set; }
        public bool VerboseDebugLogging { get; set; }
        public bool KeybindOverlay { get; set; } = true;
    }

    public static class SettingsTabRenderer
    {
        private static readonly string[] MenuKeys = { "F6", "F7", "F8", "F9", "F10", "Insert", "Home", "Delete" };

        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            SettingsState state, bool isHost,
            Action<string> setKeybind,
            Action<bool> setGameStackLogic, Action<bool> setVerboseDebugLogging,
            Action<bool> setKeybindOverlay,
            SaveManagementService saveService, DebugTestRoomService testRoomService)
        {
            GUIFit.Panel(new Rect(0f, y, w, 100f), boxStyle);
            float rowY = y + 16f;
            TMPHybridService.Instance.Label(0f, rowY, w, 28f, "Nugzz",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Title),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Title),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Title),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Title));
            rowY += 32f;
            TMPHybridService.Instance.Label(0f, rowY, w, 22f, "Made by XUnfairX",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Subtitle),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Subtitle),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Subtitle),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Subtitle));
            rowY += 28f;
            TMPHybridService.Instance.Label(0f, rowY, w, 18f, "GrandmasAnkles on NexusMods  |  Do not redistribute.",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            y = rowY + 40f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "KEYBIND",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 50f), boxStyle);
            rowY = y + 3f;
            TMPHybridService.Instance.Label(6f, rowY, 100f, 18f, "Toggle Key:",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            TMPHybridService.Instance.Label(106f, rowY, 80f, 18f, state.MenuKeybind,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            rowY += 22f;

            float keyW = (w - 36f) / MenuKeys.Length;
            for (int i = 0; i < MenuKeys.Length; i++)
            {
                if (GUIFit.Button(new Rect(4f + i * (keyW + 4f), rowY, keyW, 18f), MenuKeys[i], buttonStyle))
                {
                    setKeybind(MenuKeys[i]);
                }
            }

            y += 54f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "HUD",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 32f), boxStyle);
            TMPHybridService.Instance.Label(6f, y + 5f, w * 0.68f, 20f, state.KeybindOverlay ? "Keybind HUD: ON" : "Keybind HUD: OFF",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w * 0.72f, y + 5f, w * 0.26f, 20f), state.KeybindOverlay ? "ON" : "OFF", buttonStyle))
            {
                bool next = !state.KeybindOverlay;
                state.KeybindOverlay = next;
                setKeybindOverlay?.Invoke(next);
            }
            y += 38f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "ITEM SPAWNER",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 32f), boxStyle);
            TMPHybridService.Instance.Label(6f, y + 5f, w * 0.68f, 20f, state.UseGameStackLogic ? "Stack Mode: Game" : "Stack Mode: StackMod",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w * 0.72f, y + 5f, w * 0.26f, 20f), state.UseGameStackLogic ? "Game" : "StackMod", buttonStyle))
            {
                bool next = !state.UseGameStackLogic;
                state.UseGameStackLogic = next;
                setGameStackLogic?.Invoke(next);
            }
            y += 38f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "DEBUG",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 32f), boxStyle);
            TMPHybridService.Instance.Label(6f, y + 5f, w * 0.68f, 20f, state.VerboseDebugLogging ? "Verbose Debug Logs: ON" : "Verbose Debug Logs: OFF",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w * 0.72f, y + 5f, w * 0.26f, 20f), state.VerboseDebugLogging ? "ON" : "OFF", buttonStyle))
            {
                bool next = !state.VerboseDebugLogging;
                state.VerboseDebugLogging = next;
                setVerboseDebugLogging?.Invoke(next);
            }
            y += 38f;

            DrawDebugTestRoom(ref y, w, buttonStyle, boxStyle, testRoomService);

            DrawHeader(4f, y, w, "ACHIEVEMENTS");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 32f), boxStyle);
            TMPHybridService.Instance.Label(6f, y + 5f, w * 0.58f, 20f, "Unlock every Steam achievement",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w * 0.62f, y + 5f, w * 0.36f, 20f), "Unlock All", buttonStyle))
            {
                UnlockService.Instance.UnlockAllAchievements();
            }
            y += 38f;

            DrawSaveManager(ref y, w, buttonStyle, boxStyle, saveService);

            TMPHybridService.Instance.Label(0f, y, w, 14f, "Config saved to UserData/MelonPreferences.cfg",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            y += 18f;
        }

        private static void DrawDebugTestRoom(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            DebugTestRoomService testRoomService)
        {
            if (testRoomService == null)
                return;

            DrawHeader(4f, y, w, "DEBUG TEST ROOM");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 88f), boxStyle);

            float rowY = y + 6f;
            float buttonW = (w - 20f) / 2f;

            if (GUIFit.Button(new Rect(6f, rowY, buttonW, 22f), testRoomService.IsLoaded ? "Reload Test Room" : "Load Test Room", buttonStyle))
                testRoomService.LoadRoom();
            if (GUIFit.Button(new Rect(14f + buttonW, rowY, buttonW, 22f), "Teleport To Room", buttonStyle))
                testRoomService.TeleportToRoom();

            rowY += 28f;
            if (GUIFit.Button(new Rect(6f, rowY, w - 12f, 22f), "Clear Test Room / Restore NPCs", buttonStyle))
                testRoomService.ClearRoom();

            rowY += 28f;
            string summary = "Status: " + testRoomService.StatusMessage +
                " | Displays: " + testRoomService.DisplayedBuildables +
                " | NPCs: " + testRoomService.LinedUpNpcs;
            DrawLabel(8f, rowY + 1f, w - 16f, 18f, summary, LabelCategory.Status);

            y += 96f;
        }

        private static void DrawSaveManager(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            SaveManagementService saveService)
        {
            if (saveService == null)
                return;

            saveService.EnsureFresh();
            if (!saveService.IsMainMenu)
            {
                DrawHeader(4f, y, w, "SAVE MANAGER");
                y += 20f;
                GUIFit.Panel(new Rect(0f, y, w, 32f), boxStyle);
                DrawLabel(8f, y + 6f, w - 16f, 18f,
                    "Save tools are hidden in-game. Return to the main menu to inspect, edit, or archive saves.",
                    LabelCategory.Status);
                y += 40f;
                return;
            }

            DrawHeader(4f, y, w, "SAVE MANAGER");
            y += 20f;

            float panelHeight = 294f;
            GUIFit.Panel(new Rect(0f, y, w, panelHeight), boxStyle);
            float rowY = y + 8f;

            DrawLabel(8f, rowY, w - 16f, 18f,
                saveService.IsMainMenu
                    ? "Main menu save tools are unlocked. Deleting a save archives it under Backups instead of permanently removing it."
                    : "Save editing is locked while loaded in-game. Return to the main menu to edit or archive saves.",
                LabelCategory.Label);
            rowY += 22f;

            string rootText = string.IsNullOrEmpty(saveService.ActiveProfilePath)
                ? "Profile: not found"
                : "Profile: " + saveService.ActiveProfilePath;
            DrawLabel(8f, rowY, w - 120f, 18f, rootText, LabelCategory.Subtitle);
            if (GUIFit.Button(new Rect(w - 104f, rowY - 1f, 96f, 20f), "Refresh", buttonStyle))
                saveService.Refresh();
            rowY += 24f;

            string cloudText = saveService.CloudMarkerPresent
                ? "Local Steam Cloud marker: Present"
                : saveService.DisabledCloudMarkerPresent
                    ? "Local Steam Cloud marker: Disabled by Nugzz"
                    : "Local Steam Cloud marker: Missing";
            DrawLabel(8f, rowY, w * 0.58f, 18f, cloudText, LabelCategory.Label);
            string cloudButton = saveService.CloudMarkerPresent
                ? saveService.CloudConfirmPending ? "Confirm Disable" : "Disable Marker"
                : "Restore Marker";
            if (saveService.IsMainMenu &&
                GUIFit.Button(new Rect(w * 0.62f, rowY - 1f, w * 0.36f, 20f), cloudButton, buttonStyle))
            {
                saveService.ToggleLocalSteamCloudMarker();
            }
            rowY += 22f;

            DrawLabel(8f, rowY, w - 16f, 18f,
                "Note: this handles the save profile marker only. Fully disabling Steam Cloud still belongs in Steam's game properties.",
                LabelCategory.Subtitle);
            rowY += 24f;

            if (!saveService.IsMainMenu)
            {
                DrawLabel(8f, rowY, w - 16f, 18f, "Actions are locked, but save slots are still shown for inspection.", LabelCategory.Status);
                rowY += 22f;
            }

            float slotW = (w - 32f) / 5f;
            for (int i = 0; i < saveService.SaveSlots.Count; i++)
            {
                SaveSlotSummary slot = saveService.SaveSlots[i];
                bool selected = slot.SlotNumber == saveService.SelectedSlotNumber;
                string label = slot.Exists
                    ? "Slot " + slot.SlotNumber + ": " + Trim(slot.OrganisationName, 14)
                    : "Slot " + slot.SlotNumber + ": Empty";
                if (GUIFit.Button(new Rect(8f + i * (slotW + 4f), rowY, slotW, 22f),
                        selected ? "> " + label : label, buttonStyle))
                {
                    saveService.SelectSlot(slot.SlotNumber);
                }
            }

            rowY += 30f;
            SaveSlotSummary selectedSlot = saveService.SelectedSlot;
            if (selectedSlot == null || !selectedSlot.Exists)
            {
                DrawLabel(8f, rowY, w - 16f, 18f, "Selected slot is empty.", LabelCategory.Label);
                rowY += 24f;
            }
            else
            {
                string summary = "Slot " + selectedSlot.SlotNumber +
                    " | Version " + (string.IsNullOrEmpty(selectedSlot.LastSaveVersion) ? "?" : selectedSlot.LastSaveVersion) +
                    " | Last played " + (string.IsNullOrEmpty(selectedSlot.LastPlayedLabel) ? "Unknown" : selectedSlot.LastPlayedLabel);
                DrawLabel(8f, rowY, w - 16f, 18f, summary, LabelCategory.Header);
                rowY += 24f;

                DrawLabel(8f, rowY, w - 16f, 18f,
                    "Organisation: " + (string.IsNullOrEmpty(selectedSlot.OrganisationName) ? "Unknown" : selectedSlot.OrganisationName),
                    LabelCategory.Label);
                rowY += 22f;

                DrawLabel(8f, rowY, w - 16f, 18f,
                    "Money: Online $" + selectedSlot.OnlineBalance.ToString("0") +
                    " | Networth $" + selectedSlot.NetWorth.ToString("0") +
                    " | Lifetime $" + selectedSlot.LifetimeEarnings.ToString("0"),
                    LabelCategory.Label);
                rowY += 24f;

                DrawLabel(8f, rowY, 132f, 18f, "Tutorial flag:", LabelCategory.Label);
                if (GUIFit.Button(new Rect(142f, rowY - 1f, 124f, 20f),
                        selectedSlot.PlayTutorial ? "Tutorial: ON" : "Tutorial: OFF", buttonStyle))
                {
                    saveService.SetPlayTutorial(!selectedSlot.PlayTutorial);
                }
                DrawLabel(278f, rowY, w - 286f, 18f,
                    saveService.IsMainMenu ? "Main-menu save action." : "Locked until main menu is detected.",
                    LabelCategory.Subtitle);
                rowY += 26f;

                if (GUIFit.Button(new Rect(8f, rowY, w * 0.32f, 22f), "Backup Slot", buttonStyle))
                    saveService.BackupSelectedSave();
                if (GUIFit.Button(new Rect(16f + w * 0.34f, rowY, w * 0.32f, 22f),
                        saveService.DeleteConfirmPending ? "Confirm Archive Delete" : "Delete Save", buttonStyle))
                    saveService.ArchiveDeleteSelectedSave();
                DrawLabel(24f + w * 0.68f, rowY + 2f, w * 0.30f, 18f,
                    "Delete moves the slot to Backups.",
                    LabelCategory.Subtitle);
                rowY += 30f;
            }

            DrawLabel(8f, rowY, w - 16f, 18f, "Status: " + saveService.StatusMessage, LabelCategory.Status);
            y += panelHeight + 8f;
        }

        private static void DrawHeader(float x, float y, float w, string text)
        {
            TMPHybridService.Instance.Label(x, y, w, 18f, text,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
        }

        private static void DrawLabel(float x, float y, float w, float h, string text, LabelCategory category)
        {
            TMPHybridService.Instance.Label(x, y, w, h, text ?? "",
                GUISystemService.Instance.GetColorForCategory(category),
                GUISystemService.Instance.GetFontSizeForCategory(category),
                GUISystemService.Instance.GetAlignmentForCategory(category),
                GUISystemService.Instance.GetStyleForCategory(category));
        }

        private static string Trim(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
                return value ?? "";
            return value.Substring(0, Math.Max(0, maxLength - 3)) + "...";
        }
    }
}
