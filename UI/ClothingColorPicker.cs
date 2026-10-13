using NugzzMenu.Services;
using UnityEngine;

namespace NugzzMenu.UI
{
    internal static class ClothingColorPicker
    {
        private static bool _expanded;
        private static readonly string[] NativeItems = { "Gold Aviators", "Silver Aviators",
            "Aqua Tie-Dye Shirt", "Purple Tie-Dye Shirt", "Yellow Tie-Dye Shirt" };

        internal static void Draw(ref float y, float w, GUIStyle buttonStyle,
            GUIStyle boxStyle, ItemService service, int quantity)
        {
            GUIFit.Panel(new Rect(0f, y, w, 34f), boxStyle);
            if (GUIFit.Button(new Rect(6f, y + 5f, w * 0.65f - 8f, 24f),
                "Clothing: " + service.GetClothingColorLabel() + (_expanded ? "  -" : "  +"), buttonStyle))
                _expanded = !_expanded;
            if (GUIFit.Button(new Rect(w * 0.65f + 2f, y + 5f, w * 0.35f - 8f, 24f),
                "Native / No color", buttonStyle)) service.SelectClothingColor(-1);
            y += 40f;
            if (!_expanded) return;

            const int columns = 4;
            int rows = (service.ClothingColorCount + columns - 1) / columns;
            float cellWidth = (w - 30f) / columns;
            GUIFit.Panel(new Rect(0f, y, w, rows * 30f + 12f), boxStyle);
            for (int i = 0; i < service.ClothingColorCount; i++)
            {
                var rect = new Rect(6f + (i % columns) * (cellWidth + 6f), y + 6f + (i / columns) * 30f, cellWidth, 24f);
                if (GUIFit.Button(rect, "    " + (service.IsClothingColorSelected(i) ? "> " : string.Empty) +
                    service.GetClothingColorName(i), buttonStyle)) service.SelectClothingColor(i);
                if (Event.current != null && Event.current.type == EventType.Repaint)
                {
                    Color before = GUI.color;
                    try
                    {
                        GUI.color = service.GetClothingColorSwatch(i);
                        // DrawTexture's generated overload chain ends in a
                        // throwing unstripping stub in current IL2CPP interop.
                        // The existing helper draws a cached GUIStyle via Box.
                        GUIFit.Texture(new Rect(rect.x + 5f, rect.y + 5f, 14f, 14f), Texture2D.whiteTexture);
                    }
                    finally { GUI.color = before; }
                }
            }
            y += rows * 30f + 18f;
            // Native colors are an enum; metallic finishes and tie-dye patterns
            // belong to item definitions, not arbitrary RGB colors.
            string[] tones = { "Gold tone (Yellow)", "Silver tone (Light Grey)", "Bronze tone (Tan)" };
            string[] keys = { "Yellow", "LightGrey", "Tan" };
            float third = (w - 24f) / 3f;
            for (int i = 0; i < tones.Length; i++)
                if (GUIFit.Button(new Rect(6f + i * (third + 6f), y, third, 24f), tones[i], buttonStyle))
                    service.SetClothingColorKey(keys[i]);
            y += 32f;
            TMPHybridService.Instance.Label(6f, y, w - 12f, 34f,
                "Native finishes / patterns: spawn these items in their original colors.",
                GUISystemService.Instance.GetColorForCategory(LabelCategory.Subtitle), 12, TextAnchor.MiddleLeft,
                FontStyle.Normal, true);
            y += 38f;
            float half = (w - 18f) / 2f;
            for (int i = 0; i < NativeItems.Length; i++)
            {
                bool previous = GUI.enabled;
                GUI.enabled = previous && service.TryFindItem(NativeItems[i], out _);
                if (GUIFit.Button(new Rect(6f + (i % 2) * (half + 6f), y + (i / 2) * 28f, half, 24f),
                    "Spawn " + NativeItems[i], buttonStyle)) service.SpawnNativeClothing(NativeItems[i], quantity);
                GUI.enabled = previous;
            }
            y += ((NativeItems.Length + 1) / 2) * 28f + 8f;
        }
    }
}
