using System;
using System.Collections.Generic;
using UnityEngine;

namespace NugzzMenu.Services
{
    public static class GUIFit
    {
        private const int DefaultMinFontSize = 7;
        private static readonly Dictionary<GUIStyle, Dictionary<int, GUIStyle>> StyleCache =
            new Dictionary<GUIStyle, Dictionary<int, GUIStyle>>();
        private static string _activeTextFieldKey;

        public static GUIStyle FittedStyle(GUIStyle source, Rect rect, string text, int minFontSize = DefaultMinFontSize, bool wordWrap = false)
        {
            GUIStyle sourceStyle = source ?? GUI.skin.button;
            EnsureFont(sourceStyle);

            int startSize = sourceStyle.fontSize > 0 ? sourceStyle.fontSize : 12;
            int smallest = Mathf.Clamp(minFontSize, 5, startSize);
            string value = text ?? "";

            if (!wordWrap && LikelyFits(sourceStyle, rect, value, startSize))
                return sourceStyle;

            for (int size = startSize; size >= smallest; size--)
            {
                GUIStyle style = GetCachedStyle(sourceStyle, size, wordWrap);
                if (Fits(style, rect, value, wordWrap))
                    return style;
            }

            return GetCachedStyle(sourceStyle, smallest, wordWrap);
        }

        public static bool Button(Rect rect, string text, GUIStyle style, int minFontSize = DefaultMinFontSize, bool wordWrap = false)
        {
            return GUI.Button(rect, text ?? "", FittedStyle(style, rect, text, minFontSize, wordWrap));
        }

        public static void Panel(Rect rect, GUIStyle style)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var gui = GUISystemService.Instance;
            GUI.Box(rect, "", style);

            if (gui.AccentSoftTexture == null)
                return;

            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), gui.AccentSoftTexture);
            if (rect.height >= 24f)
                GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), gui.AccentSoftTexture);
        }

        public static string TextField(Rect rect, string text, int maxLength, string fieldKey = null)
        {
            string value = text ?? string.Empty;
            string key = string.IsNullOrEmpty(fieldKey)
                ? rect.x + ":" + rect.y + ":" + rect.width
                : fieldKey;
            var style = FittedStyle(GUI.skin.textField, rect, text, 8);
            Event current = Event.current;
            bool active = _activeTextFieldKey == key;

            if (active && current != null && current.type == EventType.MouseDown &&
                !rect.Contains(current.mousePosition))
            {
                _activeTextFieldKey = null;
                active = false;
            }

            string display = value;
            if (active)
                display += "|";
            else if (display.Length == 0)
                display = "Click to type...";

            if (GUI.Button(rect, display, style))
            {
                _activeTextFieldKey = key;
                active = true;
            }

            if (!active || current == null || current.type != EventType.KeyDown)
                return value;

            if (current.keyCode == KeyCode.Escape || current.keyCode == KeyCode.Return ||
                current.keyCode == KeyCode.KeypadEnter)
            {
                _activeTextFieldKey = null;
                current.Use();
                return value;
            }

            if (current.keyCode == KeyCode.Backspace)
            {
                if (value.Length > 0)
                    value = value.Substring(0, value.Length - 1);
                current.Use();
                return value;
            }

            char typed = current.character;
            if (typed != '\0' && !char.IsControl(typed) && value.Length < maxLength)
            {
                value += typed;
                current.Use();
            }

            return value;
        }

        public static void EnsureFont(GUIStyle style)
        {
            if (style == null)
                return;
            if (style.font == null)
                style.font = GUISystemService.Instance.UIFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public static void ClearCache()
        {
            StyleCache.Clear();
        }

        private static GUIStyle GetCachedStyle(GUIStyle source, int fontSize, bool wordWrap)
        {
            Dictionary<int, GUIStyle> variants;
            if (!StyleCache.TryGetValue(source, out variants))
            {
                variants = new Dictionary<int, GUIStyle>();
                StyleCache[source] = variants;
            }

            int key = (fontSize << 1) | (wordWrap ? 1 : 0);
            GUIStyle style;
            if (!variants.TryGetValue(key, out style))
            {
                style = new GUIStyle(source)
                {
                    fontSize = fontSize,
                    wordWrap = wordWrap,
                    clipping = TextClipping.Clip
                };
                EnsureFont(style);
                variants[key] = style;
            }

            return style;
        }

        private static bool Fits(GUIStyle style, Rect rect, string text, bool wordWrap)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return true;

            var content = new GUIContent(text ?? "");
            if (wordWrap)
                return style.CalcHeight(content, rect.width) <= rect.height + 1f;

            Vector2 size = style.CalcSize(content);
            return size.x <= rect.width + 1f && size.y <= rect.height + 1f;
        }

        private static bool LikelyFits(GUIStyle style, Rect rect, string text, int fontSize)
        {
            if (style == null || rect.width <= 0f || rect.height <= 0f)
                return true;

            int length = string.IsNullOrEmpty(text) ? 0 : text.Length;
            if (length > 18)
                return false;

            float estimatedWidth = length * fontSize * 0.62f + 12f;
            float estimatedHeight = fontSize + 8f;
            return estimatedWidth <= rect.width && estimatedHeight <= rect.height;
        }
    }
}
