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
        public bool PlaceAnywhere { get; set; }
    }

    public static class SettingsTabRenderer
    {
        private static readonly string[] MenuKeys = { "F6", "F7", "F8", "F9", "F10", "Insert", "Home", "Delete" };

        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            SettingsState state, bool isHost,
            Action<string> setKeybind,
            Action<bool> setGameStackLogic, Action<bool> setVerboseDebugLogging, Action<bool> setPlaceAnywhere)
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

            TMPHybridService.Instance.Label(4f, y, w, 18f, "BUILDING",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 32f), boxStyle);
            bool placeAnywhere = state.PlaceAnywhere;
            if (GUIFit.Button(new Rect(6f, y + 5f, w - 12f, 20f), placeAnywhere ? "Place Anywhere: ON" : "Place Anywhere: OFF", buttonStyle))
            {
                bool next = !state.PlaceAnywhere;
                state.PlaceAnywhere = next;
                setPlaceAnywhere?.Invoke(next);
            }
            y += 38f;

            TMPHybridService.Instance.Label(0f, y, w, 14f, "Config saved to UserData/MelonPreferences.cfg",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            y += 18f;
        }
    }
}
