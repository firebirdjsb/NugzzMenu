using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Il2CppScheduleOne.Networking;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    /// <summary>
    /// State container for the Settings tab.
    /// </summary>
    public class SettingsState
    {
        public string LanJoinInput { get; set; } = "";
        public string MenuKeybind { get; set; } = "F8";
        public bool UseGameStackLogic { get; set; }
        public bool VerboseDebugLogging { get; set; }
        public bool PlaceAnywhere { get; set; }
    }

    /// <summary>
    /// Renders the Settings tab (D7). Handles credits, LAN join, and keybind settings.
    /// </summary>
    public static class SettingsTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle headerStyle, GUIStyle labelStyle,
            GUIStyle buttonStyle, GUIStyle boxStyle, SettingsState state, bool isHost,
            Lobby lobby, Action<string> setKeybind, Action<string> joinLanAddress, Action forceExitMenu, Action openSteamInvite,
            Action<bool> setGameStackLogic, Action<bool> setVerboseDebugLogging, Action<bool> setPlaceAnywhere)
        {
            GUI.Box(new Rect(0f, y, w, 100f), "", boxStyle);
            float num = y + 16f;
            TMPHybridService.Instance.Label(0f, num, w, 28f, "Nugzz",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Title),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Title),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Title),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Title));
            num += 32f;
            TMPHybridService.Instance.Label(0f, num, w, 22f, "Made by XUnfairX",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Subtitle),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Subtitle),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Subtitle),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Subtitle));
            num += 28f;
            TMPHybridService.Instance.Label(0f, num, w, 18f, "GrandmasAnkles on NexusMods  |  Do not redistribute.",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            if (isHost && lobby != null && lobby.IsInLobby)
            {
                string localIP = GetLocalIPAddress();
                ushort port = GetGamePort();
                TMPHybridService.Instance.Label(0f, y + 80f, w, 18f, "Host LAN: " + localIP + ":" + port,
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            }
            y = num + 40f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "JOIN (Steam + LAN)",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUI.Box(new Rect(0f, y, w, 170f), "", boxStyle);
            float j = y + 4f;

            if (GUIFit.Button(new Rect(6f, j, w - 12f, 18f), "Open Steam Invite UI", buttonStyle))
            {
                openSteamInvite?.Invoke();
            }
            j += 24f;

            TMPHybridService.Instance.Label(6f, j, 160f, 18f, "LAN / Direct Join:",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            j += 22f;

            TMPHybridService.Instance.Label(6f, j, 80f, 18f, "Address:",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            TMPHybridService.Instance.Label(86f, j, w - 92f, 18f,
                string.IsNullOrEmpty(state.LanJoinInput) ? "(type below)" : state.LanJoinInput,
                !string.IsNullOrEmpty(state.LanJoinInput) ? GUISystemService.Instance.GetColorForCategory(LabelCategory.Header) : GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            j += 26f;

            string[] charButtons = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "." };
            float charW = (w - 28f) / charButtons.Length;
            for (int c = 0; c < charButtons.Length; c++)
            {
                if (GUIFit.Button(new Rect(6f + (float)c * (charW + 2f), j, charW, 20f), charButtons[c], buttonStyle))
                {
                    state.LanJoinInput += charButtons[c];
                }
            }
            j += 26f;

            if (GUIFit.Button(new Rect(6f, j, 60f, 20f), "Back", buttonStyle))
            {
                if (state.LanJoinInput.Length > 0)
                    state.LanJoinInput = state.LanJoinInput.Substring(0, state.LanJoinInput.Length - 1);
            }
            if (GUIFit.Button(new Rect(70f, j, 60f, 20f), "Clear", buttonStyle))
            {
                state.LanJoinInput = "";
            }

            if (GUIFit.Button(new Rect(134f, j, w - 140f, 20f), "Join LAN", buttonStyle))
            {
                if (!string.IsNullOrEmpty(state.LanJoinInput))
                {
                    joinLanAddress?.Invoke(state.LanJoinInput.Trim());
                }
            }

            j += 28f;
            if (GUIFit.Button(new Rect(6f, j, w - 12f, 22f), "Force Exit to Main Menu", buttonStyle))
            {
                forceExitMenu?.Invoke();
            }

            y = j + 38f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "KEYBIND",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUI.Box(new Rect(0f, y, w, 50f), "", boxStyle);
            num = y + 3f;
            TMPHybridService.Instance.Label(6f, num, 100f, 18f, "Toggle Key:",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            TMPHybridService.Instance.Label(106f, num, 80f, 18f, state.MenuKeybind,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            num += 22f;

            string[] keys = { "F6", "F7", "F8", "F9", "F10", "Insert", "Home", "Delete" };
            float keyW = (w - 36f) / keys.Length;
            for (int i = 0; i < keys.Length; i++)
            {
                if (GUIFit.Button(new Rect(4f + (float)i * (keyW + 4f), num, keyW, 18f), keys[i], buttonStyle))
                {
                    setKeybind(keys[i]);
                }
            }

            y += 54f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "ITEM SPAWNER",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUI.Box(new Rect(0f, y, w, 32f), "", boxStyle);
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
            GUI.Box(new Rect(0f, y, w, 32f), "", boxStyle);
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
            GUI.Box(new Rect(0f, y, w, 32f), "", boxStyle);
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

        private static string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && 
                        !System.Net.IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        private static ushort GetGamePort()
        {
            try
            {
                var transportType = System.Type.GetType("FishySteamworks.FishySteamworks");
                if (transportType != null)
                {
                    var portField = transportType.GetField("_port",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    if (portField != null)
                        return (ushort)(int)portField.GetValue(null);
                }
            }
            catch { }
            return 27015;
        }
    }
}