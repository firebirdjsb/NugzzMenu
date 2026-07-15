using UnityEngine;
using NugzzMenu.Services;

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

            DrawHeader(4f, y, w, "FRAME CAP");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 56f), boxStyle);
            float bw = (w - 28f) / 6f;
            float rowY = y + 6f;
            if (GUIFit.Button(new Rect(6f, rowY, bw, 22f), "60", buttonStyle)) service.SetTargetFps(60);
            if (GUIFit.Button(new Rect(10f + bw, rowY, bw, 22f), "90", buttonStyle)) service.SetTargetFps(90);
            if (GUIFit.Button(new Rect(14f + bw * 2f, rowY, bw, 22f), "120", buttonStyle)) service.SetTargetFps(120);
            if (GUIFit.Button(new Rect(18f + bw * 3f, rowY, bw, 22f), "144", buttonStyle)) service.SetTargetFps(144);
            if (GUIFit.Button(new Rect(22f + bw * 4f, rowY, bw, 22f), "240", buttonStyle)) service.SetTargetFps(240);
            if (GUIFit.Button(new Rect(26f + bw * 5f, rowY, bw, 22f), "Uncap", buttonStyle)) service.SetTargetFps(0);
            rowY += 26f;
            if (GUIFit.Button(new Rect(6f, rowY, (w - 16f) / 2f, 20f), service.VSyncEnabled ? "VSync: ON" : "VSync: OFF", buttonStyle))
                service.SetVSync(!service.VSyncEnabled);
            DrawLabel(12f + (w - 16f) / 2f, rowY + 1f, (w - 18f) / 2f, 18f,
                "Turn VSync off when using a custom FPS cap.", LabelCategory.Subtitle);
            y += 64f;

            DrawHeader(4f, y, w, "SAFE PRESETS");
            y += 20f;
            GUIFit.Panel(new Rect(0f, y, w, 72f), boxStyle);
            if (GUIFit.Button(new Rect(6f, y + 6f, w * 0.48f, 24f), "Performance Without Ugly", buttonStyle))
                service.ApplySmoothVisualsPreset();
            if (GUIFit.Button(new Rect(w * 0.51f, y + 6f, w * 0.47f, 24f), "Low-Impact Menu", buttonStyle))
                service.ApplyLowImpactMenuPreset();
            DrawLabel(8f, y + 38f, w - 16f, 30f,
                "Preset avoids shadows/textures first. It targets FPS cap, VSync, SSAO, God Rays, and cheaper AA when the game settings are available.",
                LabelCategory.Label);
            y += 80f;
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
            LabelCategory category = LabelCategory.Label)
        {
            TMPHybridService.Instance.Label(x, y, w, h, text ?? string.Empty,
                GUISystemService.Instance.GetColorForCategory(category),
                GUISystemService.Instance.GetFontSizeForCategory(category),
                GUISystemService.Instance.GetAlignmentForCategory(category),
                GUISystemService.Instance.GetStyleForCategory(category));
        }
    }
}
