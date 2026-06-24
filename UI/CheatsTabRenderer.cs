using System;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    public class CheatsState
    {
        public bool GodMode { get; set; }
        public bool InfiniteStamina { get; set; }
        public bool NeverWanted { get; set; }
        public bool SpeedBoost { get; set; }
        public float SpeedMultiplier { get; set; } = 2f;
        public float PlayerScale { get; set; } = 1f;
        public bool InfiniteAmmo { get; set; }
        public bool FlyEnabled { get; set; }
        public float FlySpeed { get; set; } = 20f;
        public bool ThirdPerson { get; set; }
        public float CameraDistance { get; set; }
        public float CameraHeight { get; set; }
        public float CameraShoulder { get; set; }
        public int SelectedTeleportIndex { get; set; } = 0;
        public int TeleportPageIndex { get; set; } = 0;
    }

    public static class CheatsTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle onStyle, GUIStyle offStyle,
            GUIStyle buttonStyle, GUIStyle boxStyle,
            CheatsState state, Action<float, int> teleportAction, Action onHeal, Action onClearWanted,
            Action<float> setSpeedMultiplier, Action<float> setPlayerScale,
            Action<bool> toggleFly, Action<float> setFlySpeed, Action<bool> toggleCamera,
            Action<float> setCameraDistance, Action<float> setCameraHeight, Action<float> setCameraShoulder,
            Action onSavePos, Action onLoadPos)
        {
            TMPHybridService.Instance.Label(4f, y, w, 18f, "CHEATS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUIFit.Panel(new Rect(0f, y, w, 184f), boxStyle);
            float rowY = y + 3f;

            DrawToggle(rowY, w, onStyle, offStyle, "God Mode", state.GodMode, value => state.GodMode = value);
            rowY += 22f;

            DrawToggle(rowY, w, onStyle, offStyle, "Infinite Stamina", state.InfiniteStamina, value => state.InfiniteStamina = value);
            rowY += 22f;

            DrawToggle(rowY, w, onStyle, offStyle, "Never Wanted", state.NeverWanted, value => state.NeverWanted = value);
            rowY += 22f;

            DrawToggle(rowY, w, onStyle, offStyle, "Speed Boost", state.SpeedBoost, value => state.SpeedBoost = value);
            rowY += 22f;

            DrawMultiplier(rowY, w, "Speed Multiplier", state.SpeedMultiplier, setSpeedMultiplier, buttonStyle);
            rowY += 22f;

            DrawMultiplier(rowY, w, "Player Size (buggy)", state.PlayerScale, setPlayerScale, buttonStyle);
            rowY += 22f;

            DrawToggle(rowY, w, onStyle, offStyle, "Infinite Ammo", state.InfiniteAmmo, value => state.InfiniteAmmo = value);
            rowY += 22f;

            float actionWidth = (w - 18f) * 0.5f;
            if (GUIFit.Button(new Rect(6f, rowY, actionWidth, 20f), "Heal", buttonStyle)) onHeal?.Invoke();
            if (GUIFit.Button(new Rect(12f + actionWidth, rowY, actionWidth, 20f), "Clear Wanted", buttonStyle)) onClearWanted?.Invoke();

            y += 192f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "FLY",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 48f), boxStyle);
            rowY = y + 3f;
            TMPHybridService.Instance.Label(6f, rowY, w * 0.6f, 20f, "Fly (WASD+Space/Ctrl)",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w * 0.62f, rowY, 70f, 20f), state.FlyEnabled ? "ON" : "OFF", state.FlyEnabled ? onStyle : offStyle))
            {
                toggleFly?.Invoke(!state.FlyEnabled);
            }
            rowY += 22f;
            if (GUIFit.Button(new Rect(6f, rowY, 48f, 20f), "Slow", buttonStyle))
            {
                setFlySpeed(8f);
            }
            if (GUIFit.Button(new Rect(58f, rowY, 48f, 20f), "Med", buttonStyle))
            {
                setFlySpeed(20f);
            }
            if (GUIFit.Button(new Rect(110f, rowY, 48f, 20f), "Fast", buttonStyle))
            {
                setFlySpeed(50f);
            }
            if (GUIFit.Button(new Rect(162f, rowY, 48f, 20f), "Ultra", buttonStyle))
            {
                setFlySpeed(100f);
            }
            TMPHybridService.Instance.Label(216f, rowY, 90f, 20f, "Spd:" + state.FlySpeed.ToString("F0"),
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            y += 56f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "CAMERA",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            float cameraBoxHeight = state.ThirdPerson ? 100f : 28f;
            GUIFit.Panel(new Rect(0f, y, w, cameraBoxHeight), boxStyle);
            if (GUIFit.Button(new Rect(4f, y + 3f, w - 8f, 22f), state.ThirdPerson ? "3rd Person: ON (G)" : "1st Person - press G or click", state.ThirdPerson ? onStyle : buttonStyle))
            {
                toggleCamera?.Invoke(!state.ThirdPerson);
            }
            if (state.ThirdPerson)
            {
                DrawCameraOption(y + 29f, w, "Distance", state.CameraDistance, 0.25f, setCameraDistance, buttonStyle);
                DrawCameraOption(y + 52f, w, "Height", state.CameraHeight, 0.1f, setCameraHeight, buttonStyle);
                DrawCameraOption(y + 75f, w, "Shoulder", state.CameraShoulder, 0.1f, setCameraShoulder, buttonStyle);
            }
            y += cameraBoxHeight + 4f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "TELEPORT",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 46f), boxStyle);
            rowY = y + 3f;
            float teleportButtonWidth = (w - 16f) / 4f;
            if (GUIFit.Button(new Rect(4f, rowY, teleportButtonWidth, 20f), "Fwd 10", buttonStyle))
            {
                teleportAction?.Invoke(10f, 0);
            }
            if (GUIFit.Button(new Rect(8f + teleportButtonWidth, rowY, teleportButtonWidth, 20f), "Fwd 50", buttonStyle))
            {
                teleportAction?.Invoke(50f, 0);
            }
            if (GUIFit.Button(new Rect(12f + teleportButtonWidth * 2f, rowY, teleportButtonWidth, 20f), "Up 30", buttonStyle))
            {
                teleportAction?.Invoke(30f, 1);
            }
            if (GUIFit.Button(new Rect(16f + teleportButtonWidth * 3f, rowY, teleportButtonWidth, 20f), "Up 100", buttonStyle))
            {
                teleportAction?.Invoke(100f, 1);
            }
            rowY += 24f;
            float positionButtonWidth = (w - 8f) / 2f;
            if (GUIFit.Button(new Rect(4f, rowY, positionButtonWidth, 20f), "Save Pos", buttonStyle))
            {
                onSavePos?.Invoke();
            }
            if (GUIFit.Button(new Rect(8f + positionButtonWidth, rowY, positionButtonWidth, 20f), "Load Pos", buttonStyle))
            {
                onLoadPos?.Invoke();
            }
            y += 54f;

            DrawWorldTeleports(ref y, w, onStyle, buttonStyle, boxStyle, state);
        }

        private static void DrawWorldTeleports(ref float y, float w, GUIStyle onStyle, GUIStyle buttonStyle,
            GUIStyle boxStyle, CheatsState state)
        {
            const int pageSize = 8;
            const float rowHeight = 26f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "WORLD TELEPORTS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            TeleportService service = TeleportService.Instance;
            int destinationCount = service.DestinationCount;
            int pageCount = service.GetPageCount(pageSize);
            if (state.TeleportPageIndex >= pageCount)
                state.TeleportPageIndex = Math.Max(0, pageCount - 1);
            if (state.TeleportPageIndex < 0)
                state.TeleportPageIndex = 0;
            if (state.SelectedTeleportIndex >= destinationCount)
                state.SelectedTeleportIndex = Math.Max(0, destinationCount - 1);

            int pageItemCount = service.GetPageItemCount(state.TeleportPageIndex, pageSize);
            float panelHeight = 74f + Math.Max(1, pageItemCount) * rowHeight;
            GUIFit.Panel(new Rect(0f, y, w, panelHeight), boxStyle);

            float smallW = (w - 30f) / 4f;
            if (GUIFit.Button(new Rect(6f, y + 6f, smallW, 22f), "Refresh", buttonStyle))
            {
                service.RefreshCatalog();
                destinationCount = service.DestinationCount;
                pageCount = service.GetPageCount(pageSize);
                state.TeleportPageIndex = Math.Min(state.TeleportPageIndex, Math.Max(0, pageCount - 1));
                state.SelectedTeleportIndex = Math.Min(state.SelectedTeleportIndex, Math.Max(0, destinationCount - 1));
                pageItemCount = service.GetPageItemCount(state.TeleportPageIndex, pageSize);
            }
            if (GUIFit.Button(new Rect(12f + smallW, y + 6f, smallW, 22f), "Teleport", buttonStyle))
            {
                service.TeleportToDestination(state.SelectedTeleportIndex);
            }
            if (GUIFit.Button(new Rect(18f + smallW * 2f, y + 6f, smallW, 22f), "Prev", buttonStyle))
            {
                state.TeleportPageIndex = Math.Max(0, state.TeleportPageIndex - 1);
            }
            if (GUIFit.Button(new Rect(24f + smallW * 3f, y + 6f, smallW, 22f), "Next", buttonStyle))
            {
                state.TeleportPageIndex = Math.Min(pageCount - 1, state.TeleportPageIndex + 1);
            }

            TMPHybridService.Instance.Label(6f, y + 32f, w - 12f, 18f,
                $"Page {state.TeleportPageIndex + 1}/{pageCount} | {destinationCount} destinations",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            TMPHybridService.Instance.Label(6f, y + 50f, w - 12f, 18f,
                service.GetDestinationSummary(state.SelectedTeleportIndex),
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            if (pageItemCount == 0)
            {
                TMPHybridService.Instance.Label(6f, y + 74f, w - 12f, 20f, service.StatusMessage,
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
                y += panelHeight + 4f;
                return;
            }

            int firstIndex = state.TeleportPageIndex * pageSize;
            for (int slot = 0; slot < pageItemCount; slot++)
            {
                int index = firstIndex + slot;
                Rect rect = new Rect(6f, y + 74f + slot * rowHeight, w - 12f, 22f);
                string label = service.GetDestinationButtonLabel(index);
                if (GUIFit.Button(rect, label, index == state.SelectedTeleportIndex ? onStyle : buttonStyle, 6, true))
                {
                    state.SelectedTeleportIndex = index;
                }
            }

            y += panelHeight + 4f;
        }

        private static void DrawCameraOption(float y, float w, string label, float value, float step,
            Action<float> setter, GUIStyle buttonStyle)
        {
            TMPHybridService.Instance.Label(8f, y, 110f, 20f, label + ": " + value.ToString("F2"),
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w - 86f, y, 38f, 20f), "-", buttonStyle))
                setter?.Invoke(value - step);
            if (GUIFit.Button(new Rect(w - 44f, y, 38f, 20f), "+", buttonStyle))
                setter?.Invoke(value + step);
        }

        private static void DrawMultiplier(float y, float w, string label, float value,
            Action<float> setter, GUIStyle buttonStyle)
        {
            TMPHybridService.Instance.Label(6f, y, w - 104f, 20f, label + ": " + value.ToString("F2") + "x",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w - 86f, y, 38f, 20f), "-", buttonStyle))
                setter?.Invoke(value - 0.25f);
            if (GUIFit.Button(new Rect(w - 44f, y, 38f, 20f), "+", buttonStyle))
                setter?.Invoke(value + 0.25f);
        }

        private static void DrawToggle(float y, float w, GUIStyle onStyle, GUIStyle offStyle,
            string label, bool value, Action<bool> setter)
        {
            TMPHybridService.Instance.Label(6f, y, (float)(w * 0.72), 20f, label,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w - 76f, y, 70f, 20f), value ? "ON" : "OFF", value ? onStyle : offStyle))
            {
                setter(!value);
            }
        }
    }
}
