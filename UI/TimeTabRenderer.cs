using System;
using UnityEngine;
using NugzzMenu.Services;

namespace NugzzMenu.UI
{
    public static class TimeTabRenderer
    {
        private const int EarliestTimeMinutes = 6 * 60 + 1;
        private const int LatestTimeMinutes = (24 + 4) * 60;

        private static readonly string[] SeedIds =
        {
            "ogkushseed", "sourdieselseed", "greencrackseed",
            "granddaddypurpleseed", "cocaseed"
        };

        private static readonly string[] SeedLabels =
        {
            "OG Kush", "Sour Diesel", "Green Crack",
            "Granddaddy Purple", "Coca"
        };

        private static int _selectedSeed;
        private static float _selectedTimeMinutes = EarliestTimeMinutes;

        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            Action<float> setTimeSpeed, Action<int> setTimeOfDay,
            Action growAllPlants, Action waterAllPlants, Action fillAllPotsWithSoil,
            Action completeDryingRacks, Action completeChemistryStations,
            Action completeLabOvens, Action completeMixingStations,
            Action completeCauldrons, Action<string> seedAllPots)
        {
            TMPHybridService.Instance.Label(4f, y, w, 18f, "TIME SPEED",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUIFit.Panel(new Rect(0f, y, w, 24f), boxStyle);
            float speedButtonWidth = (w - 24f) / 5f;
            float rowY = y + 3f;

            if (GUIFit.Button(new Rect(4f, rowY, speedButtonWidth, 18f), "Pause", buttonStyle)) setTimeSpeed(0f);
            if (GUIFit.Button(new Rect(8f + speedButtonWidth, rowY, speedButtonWidth, 18f), "1x", buttonStyle)) setTimeSpeed(1f);
            if (GUIFit.Button(new Rect(12f + speedButtonWidth * 2f, rowY, speedButtonWidth, 18f), "3x", buttonStyle)) setTimeSpeed(3f);
            if (GUIFit.Button(new Rect(16f + speedButtonWidth * 3f, rowY, speedButtonWidth, 18f), "5x", buttonStyle)) setTimeSpeed(5f);
            if (GUIFit.Button(new Rect(20f + speedButtonWidth * 4f, rowY, speedButtonWidth, 18f), "10x", buttonStyle)) setTimeSpeed(10f);

            y += 28f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "TIME OF DAY",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUIFit.Panel(new Rect(0f, y, w, 54f), boxStyle);
            _selectedTimeMinutes = GUIFit.Slider(
                new Rect(8f, y + 29f, w - 124f, 16f),
                _selectedTimeMinutes, EarliestTimeMinutes, LatestTimeMinutes, 1f);
            int selectedMinutes = Mathf.Clamp(Mathf.RoundToInt(_selectedTimeMinutes),
                EarliestTimeMinutes, LatestTimeMinutes);
            _selectedTimeMinutes = selectedMinutes;

            TMPHybridService.Instance.Label(8f, y + 3f, w - 16f, 18f,
                "Selected Time: " + FormatTime(selectedMinutes) + "  |  Range: 06:01 - 04:00 next day",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                TextAnchor.MiddleLeft,
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            if (GUIFit.Button(new Rect(w - 108f, y + 25f, 100f, 22f), "Apply Time", buttonStyle))
            {
                int clockMinutes = selectedMinutes % (24 * 60);
                int hour = clockMinutes / 60;
                int minute = clockMinutes % 60;
                setTimeOfDay?.Invoke(hour * 100 + minute);
            }

            y += 58f;

            TMPHybridService.Instance.Label(4f, y, w, 18f, "WORLD TIME CHEATS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            GUIFit.Panel(new Rect(0f, y, w, 170f), boxStyle);
            rowY = y + 3f;
            float worldButtonWidth = (w - 8f) / 2f;
            if (GUIFit.Button(new Rect(4f, rowY, worldButtonWidth, 22f), "Auto-Grow All Plants", buttonStyle)) growAllPlants?.Invoke();
            if (GUIFit.Button(new Rect(8f + worldButtonWidth, rowY, worldButtonWidth, 22f), "Complete Drying Racks", buttonStyle)) completeDryingRacks?.Invoke();
            if (GUIFit.Button(new Rect(4f, rowY + 28f, worldButtonWidth, 22f), "Auto-Water Plants", buttonStyle)) waterAllPlants?.Invoke();
            if (GUIFit.Button(new Rect(8f + worldButtonWidth, rowY + 28f, worldButtonWidth, 22f), "Auto Dirt Pour", buttonStyle)) fillAllPotsWithSoil?.Invoke();
            if (GUIFit.Button(new Rect(4f, rowY + 56f, worldButtonWidth, 22f), "Complete Meth Cooks", buttonStyle)) completeChemistryStations?.Invoke();
            if (GUIFit.Button(new Rect(8f + worldButtonWidth, rowY + 56f, worldButtonWidth, 22f), "Complete Lab Ovens", buttonStyle)) completeLabOvens?.Invoke();
            if (GUIFit.Button(new Rect(4f, rowY + 84f, worldButtonWidth, 22f), "Complete Mixing", buttonStyle)) completeMixingStations?.Invoke();
            if (GUIFit.Button(new Rect(8f + worldButtonWidth, rowY + 84f, worldButtonWidth, 22f), "Complete Cauldrons", buttonStyle)) completeCauldrons?.Invoke();

            float seedButtonWidth = (w - 16f) / 3f;
            if (GUIFit.Button(new Rect(4f, rowY + 112f, seedButtonWidth, 22f), "Prev Seed", buttonStyle))
                _selectedSeed = (_selectedSeed + SeedIds.Length - 1) % SeedIds.Length;
            TMPHybridService.Instance.Label(8f + seedButtonWidth, rowY + 112f, seedButtonWidth, 22f,
                SeedLabels[_selectedSeed],
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                TextAnchor.MiddleCenter,
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
            if (GUIFit.Button(new Rect(12f + seedButtonWidth * 2f, rowY + 112f,
                seedButtonWidth, 22f), "Next Seed", buttonStyle))
                _selectedSeed = (_selectedSeed + 1) % SeedIds.Length;

            if (GUIFit.Button(new Rect(4f, rowY + 140f, w - 8f, 22f),
                "Auto-Seed All Soil: " + SeedLabels[_selectedSeed], buttonStyle))
                seedAllPots?.Invoke(SeedIds[_selectedSeed]);

            y += 176f;
        }

        private static string FormatTime(int totalMinutes)
        {
            int clockMinutes = totalMinutes % (24 * 60);
            int hour = clockMinutes / 60;
            int minute = clockMinutes % 60;
            return hour.ToString("D2") + ":" + minute.ToString("D2") +
                   (totalMinutes >= 24 * 60 ? " (next day)" : string.Empty);
        }
    }
}
