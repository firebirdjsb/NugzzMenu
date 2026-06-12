using System;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    /// <summary>
    /// State container for the Lobby tab.
    /// </summary>
    public class LobbyState
    {
        public int SelectedPlayerIndex { get; set; } = 0;
        public bool AdminConsent { get; set; }
        public bool AdminOverride { get; set; }
    }

    /// <summary>
    /// Renders the Lobby tab (D5). Handles player list, admin actions, and remote effects.
    /// </summary>
    public static class LobbyTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle headerStyle, GUIStyle labelStyle,
            GUIStyle onStyle, GUIStyle offStyle, GUIStyle buttonStyle, GUIStyle boxStyle,
            LobbyState state, Il2CppSystem.Collections.Generic.List<Player> players, Action<Player> tpToPlayer,
            string[] effectIds, string[] effectLabels, Action<string> applyLocalEffect,
            Action onTeleportUp, Action onRagdoll, Action onStand, Action onClearEffects)
        {
            TMPHybridService.Instance.Label(4f, y, w, 18f, "LOBBY PLAYERS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            int num = (players?.Count > 0) ? players.Count : 0;
            if (num == 0)
            {
                GUI.Box(new Rect(0f, y, w, 26f), "", boxStyle);
                TMPHybridService.Instance.Label(6f, y + 4f, w - 12f, 18f, "No players found",
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
                y += 30f;
                return;
            }

            if (state.SelectedPlayerIndex >= num)
                state.SelectedPlayerIndex = 0;

            int rows = Math.Min(num, 8);
            GUI.Box(new Rect(0f, y, w, (float)(rows * 22 + 4)), "", boxStyle);
            float rowY = y + 3f;

            for (int i = 0; i < rows; i++)
            {
                Player player = players[i];
                string text = PlayerLabel(player);
                if (GUIFit.Button(new Rect(4f, rowY, w - 8f, 18f), text, i == state.SelectedPlayerIndex ? onStyle : buttonStyle))
                {
                    state.SelectedPlayerIndex = i;
                }
                rowY += 22f;
            }
            y += (float)(rows * 22 + 8);

            Player selectedPlayer = players[state.SelectedPlayerIndex];
            TMPHybridService.Instance.Label(4f, y, w, 18f, "SELECTED",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUI.Box(new Rect(0f, y, w, 120f), "", boxStyle);
            TMPHybridService.Instance.Label(6f, y + 4f, w - 12f, 18f, PlayerLabel(selectedPlayer),
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            float num2 = (w - 18f) / 2f;
            if (GUIFit.Button(new Rect(6f, y + 26f, num2, 18f), "Copy Code", buttonStyle))
            {
                GUIUtility.systemCopyBuffer = selectedPlayer?.PlayerCode ?? "";
            }
            if (GUIFit.Button(new Rect(12f + num2, y + 26f, num2, 18f), "TP Self To Player", buttonStyle))
            {
                tpToPlayer?.Invoke(selectedPlayer);
            }
            if (GUIFit.Button(new Rect(6f, y + 50f, num2, 18f), "Save Pos", buttonStyle))
            {
                TeleportService.Instance.SavePosition();
            }
            if (GUIFit.Button(new Rect(12f + num2, y + 50f, num2, 18f), "Load Pos", buttonStyle))
            {
                TeleportService.Instance.LoadPosition();
            }
            y += 124f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "LOCAL EFFECT",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            int effectCount = Math.Min(effectIds?.Length ?? 0, effectLabels?.Length ?? 0);
            int effectRows = Math.Max(1, (effectCount + 3) / 4);
            float fxW = (w - 24f) / 4f;
            float localBoxH = 34f + effectRows * 24f;
            GUI.Box(new Rect(0f, y, w, localBoxH), "", boxStyle);

            if (GUIFit.Button(new Rect(6f, y + 6f, fxW, 18f), "Up", buttonStyle)) onTeleportUp?.Invoke();
            if (GUIFit.Button(new Rect(10f + fxW, y + 6f, fxW, 18f), "Ragdoll", buttonStyle)) onRagdoll?.Invoke();
            if (GUIFit.Button(new Rect(14f + fxW * 2f, y + 6f, fxW, 18f), "Stand", buttonStyle)) onStand?.Invoke();
            if (GUIFit.Button(new Rect(18f + fxW * 3f, y + 6f, fxW, 18f), "Clear FX", buttonStyle)) onClearEffects?.Invoke();

            float fxY = y + 30f;
            for (int fx = 0; fx < effectCount; fx++)
            {
                float bx = 6f + (float)(fx % 4) * (fxW + 4f);
                float by = fxY + (float)(fx / 4) * 24f;
                string label = effectLabels[fx] ?? effectIds[fx];
                if (GUIFit.Button(new Rect(bx, by, fxW, 20f), label, buttonStyle))
                {
                    applyLocalEffect?.Invoke(effectIds[fx]);
                }
            }

            y += localBoxH + 4f;
        }

        private static string PlayerLabel(Player player)
        {
            if (player == null) return "<null>";
            string text = player.PlayerName;
            if (string.IsNullOrEmpty(text))
                text = player.name;
            if (string.IsNullOrEmpty(text))
                text = player.PlayerCode;
            if (player.IsLocalPlayer)
                text += " (Local)";
            return text + " [" + player.PlayerCode + "]";
        }
    }
}
