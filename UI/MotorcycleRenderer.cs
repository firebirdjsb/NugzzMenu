using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    internal static class MotorcycleRenderer
    {
        internal static void Draw(ref float y, float width, GUIStyle buttons, GUIStyle box)
        {
            GUIFit.Panel(new Rect(0f, y, width, 82f), box);
            TMPHybridService.Instance.Label(8f, y + 4f, width - 16f, 18f, "RIDEABLE MOTORCYCLE",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Header),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Header), TextAnchor.MiddleLeft);
            float half = (width - 20f) * 0.5f;
            if (GUIFit.Button(new Rect(8f, y + 25f, half, 22f), "Spawn Motorcycle", buttons)) MotorcycleService.Instance.Spawn();
            if (GUIFit.Button(new Rect(12f + half, y + 25f, half, 22f), "Remove Unoccupied", buttons)) MotorcycleService.Instance.RemoveUnoccupied();
            TMPHybridService.Instance.Label(8f, y + 53f, width - 16f, 22f,
                "Solo / host · interact to drive · no storage · session only",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Label),
                GUISystemService.Instance.GetFontSizeForCategory(LabelCategory.Label), TextAnchor.MiddleLeft);
            y += 88f;
        }
    }
}
