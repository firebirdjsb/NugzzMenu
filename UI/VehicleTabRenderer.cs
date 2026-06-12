using System;
using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    public static class VehicleTabRenderer
    {
        private static int _pendingRiskySpawnIndex = -1;
        private static float _pendingRiskySpawnTime = -100f;

        public static void Draw(ref float y, float w, GUIStyle headerStyle, GUIStyle labelStyle,
            GUIStyle buttonStyle, GUIStyle boxStyle, VehicleService service)
        {
            int count = service.GetVehicleCount();
            int selected = service.GetSelectedIndex();
            var names = service.GetVehicleNames();

            TMPHybridService.Instance.Label(4f, y, w, 18f, "VEHICLE SPAWNER",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            if (count > 0)
            {
                int rows = (count + 2) / 3;
                bool selectedRisky = service.IsVehicleRiskyAt(selected);
                float warningH = selectedRisky ? 28f : 0f;
                GUI.Box(new Rect(0f, y, w, (float)(rows * 24 + 60) + warningH), "", boxStyle);
                float rowY = y + 3f;

                for (int i = 0; i < count; i += 3)
                {
                    float colW = (w - 12f) / 3f;
                    for (int j = 0; j < 3 && i + j < count; j++)
                    {
                        int vehicleIndex = i + j;
                        string buttonLabel = service.IsVehicleRiskyAt(vehicleIndex) ? "[!] " + names[vehicleIndex] : names[vehicleIndex];
                        if (GUIFit.Button(new Rect(4f + j * (colW + 4f), rowY, colW, 18f), buttonLabel,
                                i + j == selected ? buttonStyle : buttonStyle))
                        {
                            service.SetSelectedIndex(vehicleIndex);
                            selected = vehicleIndex;
                            _pendingRiskySpawnIndex = -1;
                        }
                    }
                    rowY += 24f;
                }

                rowY += 4f;
                TMPHybridService.Instance.Label(6f, rowY, w * 0.58f, 22f,
                    "Selected: " + (selectedRisky ? "[!] " : "") + (service.GetSelectedVehicleName() ?? "None"),
                    GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                    GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

                if (GUIFit.Button(new Rect(w * 0.62f, rowY, w * 0.36f - 4f, 22f), "Spawn Selected", buttonStyle))
                {
                    if (service.IsVehicleRiskyAt(selected) && !IsRiskySpawnConfirmed(selected))
                    {
                        _pendingRiskySpawnIndex = selected;
                        _pendingRiskySpawnTime = Time.unscaledTime;
                        NotificationService.Instance.Warning("NPC/police vehicle. Click Spawn again to confirm.");
                        return;
                    }

                    _pendingRiskySpawnIndex = -1;
                    string result = service.SpawnSelectedVehicle();
                    if (result != null)
                    {
                        NotificationService.Instance.Notify($"Spawned {result} - you can now drive it!");
                    }
                }

                if (selectedRisky)
                {
                    rowY += 26f;
                    TMPHybridService.Instance.Label(6f, rowY, w - 12f, 22f,
                        service.GetSelectedVehicleRiskWarning(),
                        GUISystemService.Instance.GetColorForCategory(LabelCategory.Error),
                        GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                        GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                        GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));
                }

                y += (float)(rows * 24 + 64) + warningH;
            }
        }

        private static bool IsRiskySpawnConfirmed(int selected)
        {
            return _pendingRiskySpawnIndex == selected && Time.unscaledTime - _pendingRiskySpawnTime <= 6f;
        }
    }
}
