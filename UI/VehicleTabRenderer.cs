using System;
using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    public static class VehicleTabRenderer
    {
        private static int _pendingRiskySpawnIndex = -1;
        private static float _pendingRiskySpawnTime = -100f;

        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            VehicleService service)
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
                bool canSpawn = service.CanSpawnVehicles();
                float warningH = selectedRisky ? 28f : 0f;
                float hostNoticeH = canSpawn ? 0f : 24f;
                GUIFit.Panel(new Rect(0f, y, w, (float)(rows * 24 + 60) + warningH + hostNoticeH), boxStyle);
                float rowY = y + 3f;

                for (int i = 0; i < count; i += 3)
                {
                    float colW = (w - 12f) / 3f;
                    for (int j = 0; j < 3 && i + j < count; j++)
                    {
                        int vehicleIndex = i + j;
                        string buttonLabel = service.IsVehicleRiskyAt(vehicleIndex) ? "[!] " + names[vehicleIndex] : names[vehicleIndex];
                        if (GUIFit.Button(new Rect(4f + j * (colW + 4f), rowY, colW, 18f), buttonLabel, buttonStyle))
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

                string spawnLabel = canSpawn ? "Spawn Selected" : "Host Only";
                if (GUIFit.Button(new Rect(w * 0.62f, rowY, w * 0.36f - 4f, 22f), spawnLabel, buttonStyle))
                {
                    if (!canSpawn)
                    {
                        NotificationService.Instance.Warning("Vehicle spawning is host-only in multiplayer");
                        return;
                    }

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

                y += (float)(rows * 24 + 64) + warningH + hostNoticeH;
            }

            y += 8f;
            TMPHybridService.Instance.Label(4f, y, w, 18f, "WORLD CONTROLS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;

            bool canUseWorldControls = service.CanSpawnVehicles();
            GUIFit.Panel(new Rect(0f, y, w, 90f), boxStyle);
            float controlY = y + 6f;

            string rvBlowLabel = canUseWorldControls ? "Blow Up RV" : "Host Only";
            if (GUIFit.Button(new Rect(6f, controlY, w * 0.48f - 8f, 22f), rvBlowLabel, buttonStyle))
            {
                if (!canUseWorldControls)
                    NotificationService.Instance.Warning("RV controls are host-only in multiplayer");
                else
                    service.BlowUpRV();
            }

            string rvFixLabel = canUseWorldControls ? "Fix / Respawn RV" : "Host Only";
            if (GUIFit.Button(new Rect(w * 0.52f, controlY, w * 0.48f - 8f, 22f), rvFixLabel, buttonStyle))
            {
                if (!canUseWorldControls)
                    NotificationService.Instance.Warning("RV controls are host-only in multiplayer");
                else
                    service.FixOrRespawnRV();
            }

            controlY += 28f;
            string manorState = service.BenzieManorAccessEnabled ? "On" : "Off";
            TMPHybridService.Instance.Label(6f, controlY + 2f, w * 0.48f, 18f,
                "Benzie Manor Access: " + manorState,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            string manorLabel = canUseWorldControls ? (service.BenzieManorAccessEnabled ? "Turn Manor Off" : "Allow Manor Use") : "Host Only";
            if (GUIFit.Button(new Rect(w * 0.52f, controlY, w * 0.48f - 8f, 22f), manorLabel, buttonStyle))
            {
                if (!canUseWorldControls)
                    NotificationService.Instance.Warning("Benzie Manor access is host-only in multiplayer");
                else
                    service.SetBenzieManorAccess(!service.BenzieManorAccessEnabled);
            }

            controlY += 26f;
            TMPHybridService.Instance.Label(6f, controlY, w - 12f, 18f,
                canUseWorldControls ? "Host tools for story RV state and Benzie Manor access." : "World controls are protected: only the host can sync these changes.",
                canUseWorldControls
                    ? GUISystemService.Instance.GetColorForCategory(LabelCategory.Label)
                    : GUISystemService.Instance.GetColorForCategory(LabelCategory.Error),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Label));

            y += 98f;
        }

        private static bool IsRiskySpawnConfirmed(int selected)
        {
            return _pendingRiskySpawnIndex == selected && Time.unscaledTime - _pendingRiskySpawnTime <= 6f;
        }
    }
}
