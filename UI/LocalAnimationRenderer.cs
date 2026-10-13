using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    internal static class LocalAnimationRenderer
    {
        internal static void Draw(ref float y, float width, GUIStyle onStyle, GUIStyle buttonStyle, GUIStyle boxStyle)
        {
            var service = LocalAnimationService.Instance;
            TMPHybridService.Instance.Label(4f, y, width - 8f, 18f, "LOCAL NPC ANIMATIONS",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetAlignmentForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetStyleForCategory(LabelCategory.Header));
            y += 20f;
            const float height = 156f;
            GUIFit.Panel(new Rect(0f, y, width, height), boxStyle);
            float buttonWidth = (width - 16f) / 2f;
            for (int i = 0; i < LocalAnimationCatalog.Actions.Length; i++)
            {
                var rect = new Rect(6f + (i % 2) * (buttonWidth + 4f), y + 6f + (i / 2) * 25f, buttonWidth, 22f);
                if (GUIFit.Button(rect, LocalAnimationCatalog.Actions[i].Label,
                    service.ActiveAction == i ? onStyle : buttonStyle)) service.Play(i);
            }
            if (GUIFit.Button(new Rect(6f, y + 108f, buttonWidth, 22f), "Stop animation", buttonStyle)) service.Stop();
            TMPHybridService.Instance.Label(10f + buttonWidth, y + 108f, buttonWidth, 22f, service.Status,
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Status),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Status), TextAnchor.MiddleLeft);
            TMPHybridService.Instance.Label(6f, y + 133f, width - 12f, 18f, "Local player · stops after 20s or movement",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label), TextAnchor.MiddleLeft);
            y += height + 4f;
        }
    }
}
