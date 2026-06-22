using System;
using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    public static class VehicleTabRenderer
    {
        private const int VehicleColumns = 3;
        private const int VehicleRowsPerPage = 6;
        private const int VehiclesPerPage = VehicleColumns * VehicleRowsPerPage;

        private static int _pendingRiskySpawnIndex = -1;
        private static float _pendingRiskySpawnTime = -100f;
        private static int _vehiclePage;

        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            VehicleService service)
        {
            int count = service.GetVehicleCount();
            int selected = service.GetSelectedIndex();
            if (count > 0 && (selected < 0 || selected >= count))
            {
                service.SetSelectedIndex(0);
                selected = service.GetSelectedIndex();
            }

            TMPHybridService.Instance.Label(4f, y, w, 18f, "VEHICLE SPAWNER",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            if (count > 0)
            {
                int maxPage = Mathf.Max(0, (count - 1) / VehiclesPerPage);
                _vehiclePage = Mathf.Clamp(_vehiclePage, 0, maxPage);

                int pageStart = _vehiclePage * VehiclesPerPage;
                int pageEnd = Mathf.Min(count, pageStart + VehiclesPerPage);
                int pageCount = pageEnd - pageStart;
                int rows = Mathf.Max(1, (pageCount + VehicleColumns - 1) / VehicleColumns);
                bool selectedWarning = service.ShouldWarnForVehicleAt(selected);
                bool canSpawn = service.CanSpawnVehicles();
                float warningH = selectedWarning ? 34f : 0f;
                float hostNoticeH = canSpawn ? 0f : 24f;
                float panelHeight = (float)(rows * 24 + 92) + warningH + hostNoticeH;
                GUIFit.Panel(new Rect(0f, y, w, panelHeight), boxStyle);
                float rowY = y + 5f;

                float navButtonW = 78f;
                Label(8f, rowY + 2f, w - navButtonW * 2f - 28f, 18f,
                    "Showing " + (pageStart + 1) + "-" + pageEnd + " of " + count +
                    " vehicles  |  Page " + (_vehiclePage + 1) + "/" + (maxPage + 1));

                if (GUIFit.Button(new Rect(w - navButtonW * 2f - 16f, rowY, navButtonW, 22f), "Prev", buttonStyle))
                    _vehiclePage = Mathf.Max(0, _vehiclePage - 1);
                if (GUIFit.Button(new Rect(w - navButtonW - 8f, rowY, navButtonW, 22f), "Next", buttonStyle))
                    _vehiclePage = Mathf.Min(maxPage, _vehiclePage + 1);

                rowY += 28f;

                for (int i = pageStart; i < pageEnd; i += VehicleColumns)
                {
                    float colW = (w - 12f) / VehicleColumns;
                    for (int j = 0; j < VehicleColumns && i + j < pageEnd; j++)
                    {
                        int vehicleIndex = i + j;
                        string vehicleName = service.GetVehicleNameAt(vehicleIndex) ?? "Unknown";
                        string buttonLabel = service.ShouldWarnForVehicleAt(vehicleIndex)
                            ? "[!] " + vehicleName
                            : vehicleName;
                        if (GUIFit.Button(new Rect(4f + j * (colW + 4f), rowY, colW, 18f), buttonLabel, buttonStyle))
                        {
                            service.SetSelectedIndex(vehicleIndex);
                            selected = vehicleIndex;
                            selectedWarning = service.ShouldWarnForVehicleAt(selected);
                            _pendingRiskySpawnIndex = -1;
                        }
                    }
                    rowY += 24f;
                }

                rowY += 6f;
                TMPHybridService.Instance.Label(6f, rowY, w * 0.58f, 22f,
                    "Selected: " + (selectedWarning ? "[!] " : "") + (service.GetSelectedVehicleName() ?? "None"),
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

                string spawnLabel = !canSpawn ? "Host Only" : selectedWarning ? "Spawn Custom" : "Spawn Selected";
                if (GUIFit.Button(new Rect(w * 0.62f, rowY, w * 0.36f - 4f, 22f), spawnLabel, buttonStyle))
                {
                    if (!canSpawn)
                    {
                        NotificationService.Instance.Warning("Vehicle spawning is host-only in multiplayer");
                        return;
                    }

                    if (service.ShouldWarnForVehicleAt(selected) && !IsRiskySpawnConfirmed(selected))
                    {
                        _pendingRiskySpawnIndex = selected;
                        _pendingRiskySpawnTime = Time.unscaledTime;
                        NotificationService.Instance.Warning("Special vehicle. Click Spawn again to confirm.");
                        return;
                    }

                    _pendingRiskySpawnIndex = -1;
                    string result = service.SpawnSelectedVehicle();
                    if (result != null)
                    {
                        NotificationService.Instance.Notify($"Spawned {result} - you can now drive it!");
                    }
                }

                if (!canSpawn)
                {
                    rowY += 26f;
                    TMPHybridService.Instance.Label(6f, rowY, w - 12f, 18f,
                        "Vehicle spawning is protected: only the host can spawn synced vehicles.",
                        GUISystemService.Instance.GetColorForCategory(LabelCategory.Error),
                        GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                        GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                        GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
                }

                if (selectedWarning)
                {
                    rowY += 26f;
                    TMPHybridService.Instance.Label(6f, rowY, w - 12f, 22f,
                        service.GetSelectedVehicleRiskWarning(),
                        GUISystemService.Instance.GetColorForCategory(LabelCategory.Error),
                        GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                        GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                        GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
                }

                y += panelHeight + 4f;
            }

            y += 8f;
            DrawVehicleTuning(ref y, w, buttonStyle, boxStyle, service);
        }

        private static bool IsRiskySpawnConfirmed(int selected)
        {
            return _pendingRiskySpawnIndex == selected && Time.unscaledTime - _pendingRiskySpawnTime <= 6f;
        }

        private static void DrawVehicleTuning(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            VehicleService service)
        {
            Label(4f, y, w, 18f, "DRIVEN VEHICLE TUNING", LabelCategory.Header);
            y += 20f;

            VehicleService.VehicleTuneSettings tune = service.GetDrivenVehicleTune();
            float height = tune == null ? 58f : 330f;
            GUIFit.Panel(new Rect(0f, y, w, height), boxStyle);
            float rowY = y + 6f;

            if (tune == null)
            {
                Label(8f, rowY + 2f, w - 16f, 20f, "Drive a vehicle to tune it. Each vehicle keeps its own settings while loaded.");
                y += height + 8f;
                return;
            }

            Label(8f, rowY, w - 16f, 18f, "Tuning: " + service.GetDrivenVehicleTuneLabel(), LabelCategory.Header);
            rowY += 24f;

            bool changed = false;
            changed |= DrawSlider(ref rowY, w, "Traction", ref tune.TractionMultiplier, 0.1f, 6f, "x");
            changed |= DrawSlider(ref rowY, w, "Steering", ref tune.SteeringMultiplier, 0.1f, 5f, "x");
            changed |= DrawSlider(ref rowY, w, "Engine / Max Speed", ref tune.SpeedMultiplier, 0.1f, 10f, "x");
            changed |= DrawSlider(ref rowY, w, "Brake Strength", ref tune.BrakeMultiplier, 0.1f, 8f, "x");
            changed |= DrawSlider(ref rowY, w, "Handbrake Bite", ref tune.BrakeHardnessMultiplier, 0.1f, 8f, "x");
            changed |= DrawSlider(ref rowY, w, "Headlight Brightness", ref tune.HeadlightBrightnessMultiplier, 0.1f, 10f, "x");

            Label(8f, rowY, w - 16f, 18f, "Headlight Color", LabelCategory.Header);
            rowY += 18f;
            changed |= DrawSlider(ref rowY, w, "Red", ref tune.HeadlightRed, 0f, 1f, "");
            changed |= DrawSlider(ref rowY, w, "Green", ref tune.HeadlightGreen, 0f, 1f, "");
            changed |= DrawSlider(ref rowY, w, "Blue", ref tune.HeadlightBlue, 0f, 1f, "");

            float colorButtonW = (w - 24f) / 3f;
            Label(8f, rowY + 2f, colorButtonW, 18f, "Paint: " + service.GetDrivenVehicleBodyColorLabel());
            if (GUIFit.Button(new Rect(12f + colorButtonW, rowY, colorButtonW, 22f), "Prev Paint", buttonStyle))
                service.CycleDrivenVehicleBodyColor(-1);
            if (GUIFit.Button(new Rect(16f + colorButtonW * 2f, rowY, colorButtonW, 22f), "Next Paint", buttonStyle))
                service.CycleDrivenVehicleBodyColor(1);
            rowY += 28f;

            if (GUIFit.Button(new Rect(8f, rowY, w * 0.48f - 10f, 22f), "Reset This Vehicle", buttonStyle))
                service.ResetDrivenVehicleTune();

            Label(w * 0.50f, rowY + 2f, w * 0.48f, 18f, "Police: press H for siren and lightbar.");

            if (changed)
                service.ApplyDrivenVehicleTune();

            y += height + 8f;
        }

        private static bool DrawSlider(ref float y, float w, string label, ref float value, float min, float max, string suffix)
        {
            float oldValue = value;
            string valueText = suffix == "x" ? value.ToString("0.00") + "x" : value.ToString("0.00");
            Label(8f, y, w * 0.40f, 18f, label + ": " + valueText);
            Rect sliderRect = new Rect(w * 0.42f, y + 4f, w * 0.54f, 16f);
            value = GUI.HorizontalSlider(sliderRect, value, min, max);
            y += 24f;
            return Math.Abs(oldValue - value) > 0.001f;
        }

        private static void Label(float x, float y, float w, float h, string text,
            LabelCategory category = LabelCategory.Label)
        {
            TMPHybridService.Instance.Label(x, y, w, h, text,
                GUISystemService.Instance.GetColorForCategory(category),
                GUISystemService.Instance.GetFontSizeForCategory(category),
                GUISystemService.Instance.GetAlignmentForCategory(category),
                GUISystemService.Instance.GetStyleForCategory(category));
        }
    }
}
