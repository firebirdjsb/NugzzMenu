using System;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    /// <summary>
    /// State container for the Cheats tab.
    /// </summary>
    public class CheatsState
    {
        public bool GodMode { get; set; }
        public bool InfiniteStamina { get; set; }
        public bool InfiniteEnergy { get; set; }
        public bool NeverWanted { get; set; }
        public bool SpeedBoost { get; set; }
        public float SpeedMultiplier { get; set; } = 2f;
        public bool InfiniteAmmo { get; set; }
        public bool FlyEnabled { get; set; }
        public float FlySpeed { get; set; } = 20f;
        public bool ThirdPerson { get; set; }
        public float CameraDistance { get; set; }
        public float CameraHeight { get; set; }
        public float CameraShoulder { get; set; }
    }

    /// <summary>
    /// Renders the Cheats tab. Handles god mode, stamina, energy, speed, ammo, fly, camera, and teleport UI.
    /// </summary>
    public static class CheatsTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle headerStyle, GUIStyle labelStyle,
            GUIStyle onStyle, GUIStyle offStyle, GUIStyle buttonStyle, GUIStyle boxStyle,
            CheatsState state, Action<float, int> teleportAction, Action onHeal, Action onClearWanted,
            Action<float> setSpeedMultiplier, Action<bool> toggleFly, Action<float> setFlySpeed, Action<bool> toggleCamera,
            Action<float> setCameraDistance, Action<float> setCameraHeight, Action<float> setCameraShoulder,
            Action onSavePos, Action onLoadPos, Action onTutorialTown)
        {
            TMPHybridService.Instance.Label(4f, y, w, 18f, "CHEATS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUI.Box(new Rect(0f, y, w, 184f), "", boxStyle);
            float num = y + 3f;

            DrawToggle(num, w, labelStyle, onStyle, offStyle, buttonStyle, "God Mode", state.GodMode, v => state.GodMode = v);
            num += 22f;

            DrawToggle(num, w, labelStyle, onStyle, offStyle, buttonStyle, "Infinite Stamina", state.InfiniteStamina, v => state.InfiniteStamina = v);
            num += 22f;

            DrawToggle(num, w, labelStyle, onStyle, offStyle, buttonStyle, "Infinite Energy", state.InfiniteEnergy, v => state.InfiniteEnergy = v);
            num += 22f;

            DrawToggle(num, w, labelStyle, onStyle, offStyle, buttonStyle, "Never Wanted", state.NeverWanted, v => state.NeverWanted = v);
            num += 22f;

            DrawToggle(num, w, labelStyle, onStyle, offStyle, buttonStyle, "Speed Boost", state.SpeedBoost, v => state.SpeedBoost = v);
            num += 22f;

            DrawMultiplier(num, w, "Speed Multiplier", state.SpeedMultiplier, setSpeedMultiplier, buttonStyle);
            num += 22f;

            DrawToggle(num, w, labelStyle, onStyle, offStyle, buttonStyle, "Infinite Ammo", state.InfiniteAmmo, v => state.InfiniteAmmo = v);
            num += 22f;

            float actionWidth = (w - 18f) * 0.5f;
            if (GUIFit.Button(new Rect(6f, num, actionWidth, 20f), "Heal", buttonStyle)) onHeal?.Invoke();
            if (GUIFit.Button(new Rect(12f + actionWidth, num, actionWidth, 20f), "Clear Wanted", buttonStyle)) onClearWanted?.Invoke();

            y += 192f;

            // Fly section
            TMPHybridService.Instance.Label(4f, y, w, 18f, "FLY",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUI.Box(new Rect(0f, y, w, 48f), "", boxStyle);
            num = y + 3f;
            TMPHybridService.Instance.Label(6f, num, w * 0.6f, 20f, "Fly (WASD+Space/Ctrl)",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(w * 0.62f, num, 70f, 20f), state.FlyEnabled ? "ON" : "OFF", state.FlyEnabled ? onStyle : offStyle))
            {
                toggleFly?.Invoke(!state.FlyEnabled);
            }
            num += 22f;
            if (GUIFit.Button(new Rect(6f, num, 48f, 20f), "Slow", buttonStyle))
            {
                setFlySpeed(8f);
            }
            if (GUIFit.Button(new Rect(58f, num, 48f, 20f), "Med", buttonStyle))
            {
                setFlySpeed(20f);
            }
            if (GUIFit.Button(new Rect(110f, num, 48f, 20f), "Fast", buttonStyle))
            {
                setFlySpeed(50f);
            }
            if (GUIFit.Button(new Rect(162f, num, 48f, 20f), "Ultra", buttonStyle))
            {
                setFlySpeed(100f);
            }
            TMPHybridService.Instance.Label(216f, num, 90f, 20f, "Spd:" + state.FlySpeed.ToString("F0"),
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            y += 56f;

            // Camera section
            TMPHybridService.Instance.Label(4f, y, w, 18f, "CAMERA",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            float cameraBoxHeight = state.ThirdPerson ? 100f : 28f;
            GUI.Box(new Rect(0f, y, w, cameraBoxHeight), "", boxStyle);
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

            // Teleport section
            TMPHybridService.Instance.Label(4f, y, w, 18f, "TELEPORT",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            GUI.Box(new Rect(0f, y, w, 70f), "", boxStyle);
            num = y + 3f;
            float num2 = (w - 16f) / 4f;
            if (GUIFit.Button(new Rect(4f, num, num2, 20f), "Fwd 10", buttonStyle))
            {
                teleportAction?.Invoke(10f, 0);
            }
            if (GUIFit.Button(new Rect(8f + num2, num, num2, 20f), "Fwd 50", buttonStyle))
            {
                teleportAction?.Invoke(50f, 0);
            }
            if (GUIFit.Button(new Rect(12f + num2 * 2f, num, num2, 20f), "Up 30", buttonStyle))
            {
                teleportAction?.Invoke(30f, 1);
            }
            if (GUIFit.Button(new Rect(16f + num2 * 3f, num, num2, 20f), "Up 100", buttonStyle))
            {
                teleportAction?.Invoke(100f, 1);
            }
            num += 24f;
            float num3 = (w - 8f) / 2f;
            if (GUIFit.Button(new Rect(4f, num, num3, 20f), "Save Pos", buttonStyle))
            {
                onSavePos?.Invoke();
            }
            if (GUIFit.Button(new Rect(8f + num3, num, num3, 20f), "Load Pos", buttonStyle))
            {
                onLoadPos?.Invoke();
            }
            num += 24f;
            if (GUIFit.Button(new Rect(4f, num, w - 8f, 20f), "Tutorial Town", buttonStyle))
            {
                onTutorialTown?.Invoke();
            }
            y += 78f;
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

        private static void DrawToggle(float y, float w, GUIStyle labelStyle, GUIStyle onStyle, GUIStyle offStyle,
            GUIStyle buttonStyle, string label, bool value, Action<bool> setter)
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
