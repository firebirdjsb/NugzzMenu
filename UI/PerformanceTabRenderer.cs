using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    public static class PerformanceTabRenderer
    {
        public static void Draw(ref float y, float w, GUIStyle buttonStyle, GUIStyle boxStyle,
            PerformanceService service)
        {
            DrawHeader(4f, y, w, "FPS / PERFORMANCE");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 34f), boxStyle);
            DrawLabel(8f, y + 7f, w - 16f, 20f, service.GetSummary(), LabelCategory.Status);
            y += 40f;

            DrawHeader(4f, y, w, "FRAME PACING");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 58f), boxStyle);
            float bw = (w - 28f) / 6f;
            float rowY = y + 6f;
            int[] caps = { 60, 90, 120, 144, 240, 0 };
            string[] capLabels = { "60", "90", "120", "144", "240", "Uncap" };
            for (int i = 0; i < caps.Length; i++)
            {
                if (GUIFit.Button(new Rect(6f + i * (bw + 4f), rowY, bw, 22f),
                        capLabels[i], buttonStyle))
                {
                    service.SetTargetFps(caps[i]);
                }
            }

            rowY += 27f;
            if (GUIFit.Button(new Rect(6f, rowY, (w - 16f) * 0.5f, 20f),
                    service.VSyncEnabled ? "VSync: ON" : "VSync: OFF", buttonStyle))
            {
                service.SetVSync(!service.VSyncEnabled);
            }
            DrawLabel(12f + (w - 16f) * 0.5f, rowY + 1f, (w - 18f) * 0.5f, 18f,
                "Custom caps automatically disable VSync.", LabelCategory.Subtitle);
            y += 66f;

            DrawHeader(4f, y, w, "RUNTIME OPTIMIZERS");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 116f), boxStyle);
            DrawLabel(8f, y + 7f, 180f, 18f, "Decorative light range", LabelCategory.Label);
            DrawButtonRow(y + 5f, 190f, w - 198f,
                new[] { "Native", "75%", "50%", "35%" }, buttonStyle,
                i => service.SetLightRangeScale(new[] { 1f, 0.75f, 0.5f, 0.35f }[i]));

            DrawLabel(8f, y + 37f, 180f, 18f, "Reflection refresh limit", LabelCategory.Label);
            DrawButtonRow(y + 35f, 190f, w - 198f,
                new[] { "Native", "0.5 sec", "1 sec", "2 sec" }, buttonStyle,
                i => service.SetReflectionInterval(new[] { 0f, 0.5f, 1f, 2f }[i]));

            DrawLabel(8f, y + 68f, w - 16f, 40f,
                "These controls use Schedule I's OptimizedLight and ReflectionProbeUpdater systems. " +
                "They reduce distant decorative-light and reflection work without disabling world lighting.",
                LabelCategory.Subtitle, true);
            y += 124f;

            DrawHeader(4f, y, w, "SCENE DETAIL DISTANCE");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 74f), boxStyle);
            DrawLabel(8f, y + 7f, 150f, 18f, "LOD detail", LabelCategory.Label);
            DrawButtonRow(y + 5f, 158f, w - 166f,
                new[] { "75%", "90%", "100%", "125%", "Restore" }, buttonStyle,
                i =>
                {
                    if (i == 4) service.RestoreLodBias();
                    else service.SetLodBias(new[] { 0.75f, 0.9f, 1f, 1.25f }[i]);
                });

            DrawLabel(8f, y + 39f, 150f, 18f, "Shadow distance", LabelCategory.Label);
            DrawButtonRow(y + 37f, 158f, w - 166f,
                new[] { "35m", "60m", "90m", "120m", "Restore" }, buttonStyle,
                i =>
                {
                    if (i == 4) service.RestoreShadowDistance();
                    else service.SetShadowDistance(new[] { 35f, 60f, 90f, 120f }[i]);
                });
            y += 82f;

            DrawHeader(4f, y, w, "PRESETS AND DIAGNOSTICS");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 100f), boxStyle);
            float quarter = (w - 30f) / 4f;
            if (GUIFit.Button(new Rect(6f, y + 6f, quarter, 24f),
                    "Balanced Performance", buttonStyle))
                service.ApplySmoothVisualsPreset();
            if (GUIFit.Button(new Rect(12f + quarter, y + 6f, quarter, 24f),
                    "Low-Impact Menu", buttonStyle))
                service.ApplyLowImpactMenuPreset();
            if (GUIFit.Button(new Rect(18f + quarter * 2f, y + 6f, quarter, 24f),
                    "Scan Scene Stats", buttonStyle))
                service.ScanDiagnostics();
            if (GUIFit.Button(new Rect(24f + quarter * 3f, y + 6f, quarter, 24f),
                    "Restore Runtime Defaults", buttonStyle))
                service.RestoreRuntimeDefaults();

            DrawLabel(8f, y + 39f, w - 16f, 20f, service.Diagnostics, LabelCategory.Status);
            DrawLabel(8f, y + 64f, w - 16f, 28f,
                "Scene scanning only runs when requested, so the diagnostics display does not create its own frame-time spikes.",
                LabelCategory.Subtitle, true);
            y += 108f;
        }

        private static void DrawButtonRow(float y, float x, float width, string[] labels,
            GUIStyle style, System.Action<int> clicked)
        {
            float buttonWidth = (width - (labels.Length - 1) * 4f) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                if (GUIFit.Button(new Rect(x + i * (buttonWidth + 4f), y, buttonWidth, 22f),
                        labels[i], style))
                {
                    clicked(i);
                }
            }
        }

        private static void DrawHeader(float x, float y, float w, string text)
        {
            TMPHybridService.Instance.Label(x, y, w, 18f, text,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
        }

        private static void DrawLabel(float x, float y, float w, float h, string text,
            LabelCategory category, bool wordWrap = false)
        {
            TMPHybridService.Instance.Label(x, y, w, h, text ?? string.Empty,
                GUISystemService.Instance.GetColorForCategory(category),
                GUISystemService.Instance.GetFontSizeForCategory(category),
                GUISystemService.Instance.GetAlignmentForCategory(category),
                GUISystemService.Instance.GetStyleForCategory(category), wordWrap);
        }
    }
}
