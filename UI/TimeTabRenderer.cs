using System;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    /// <summary>
    /// Renders the Time tab (D2). Handles time speed and time of day controls.
    /// </summary>
    public static class TimeTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle headerStyle, GUIStyle labelStyle,
            GUIStyle buttonStyle, GUIStyle boxStyle, Action<float> setTimeSpeed, Action<int> setTimeOfDay,
            Action growAllPlants, Action completeDryingRacks)
        {
            TMPHybridService.Instance.Label(4f, y, w, 18f, "TIME SPEED",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUI.Box(new Rect(0f, y, w, 24f), "", boxStyle);
            float num = (w - 24f) / 5f;
            float num2 = y + 3f;

            if (GUIFit.Button(new Rect(4f, num2, num, 18f), "Pause", buttonStyle)) setTimeSpeed(0f);
            if (GUIFit.Button(new Rect(8f + num, num2, num, 18f), "1x", buttonStyle)) setTimeSpeed(1f);
            if (GUIFit.Button(new Rect(12f + num * 2f, num2, num, 18f), "3x", buttonStyle)) setTimeSpeed(3f);
            if (GUIFit.Button(new Rect(16f + num * 3f, num2, num, 18f), "5x", buttonStyle)) setTimeSpeed(5f);
            if (GUIFit.Button(new Rect(20f + num * 4f, num2, num, 18f), "10x", buttonStyle)) setTimeSpeed(10f);

            y += 28f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "TIME OF DAY",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUI.Box(new Rect(0f, y, w, 46f), "", boxStyle);
            num2 = y + 3f;
            float num3 = (w - 8f) / 2f;

            if (GUIFit.Button(new Rect(4f, num2, num3, 18f), "Morning 06:00", buttonStyle)) setTimeOfDay(360);
            if (GUIFit.Button(new Rect(8f + num3, num2, num3, 18f), "Noon 12:00", buttonStyle)) setTimeOfDay(720);
            if (GUIFit.Button(new Rect(4f, num2 + 22f, num3, 18f), "Evening 18:00", buttonStyle)) setTimeOfDay(1080);
            if (GUIFit.Button(new Rect(8f + num3, num2 + 22f, num3, 18f), "Midnight 00:00", buttonStyle)) setTimeOfDay(0);

            y += 50f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "WORLD TIME CHEATS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUI.Box(new Rect(0f, y, w, 28f), "", boxStyle);
            num2 = y + 3f;
            num3 = (w - 8f) / 2f;
            if (GUIFit.Button(new Rect(4f, num2, num3, 22f), "Auto-Grow All Plants", buttonStyle)) growAllPlants?.Invoke();
            if (GUIFit.Button(new Rect(8f + num3, num2, num3, 22f), "Complete Drying Racks", buttonStyle)) completeDryingRacks?.Invoke();
            y += 34f;
        }
    }
}
