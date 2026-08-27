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
        private static readonly Dictionary<Texture2D, GUIStyle> TextureStyleCache =
            new Dictionary<Texture2D, GUIStyle>();
        private static string _activeTextFieldKey;
        private static ControllerInputService _controller;
        private static int _controlCount;
        private static int _focusedControl;
        private static int _pendingFocusMove;
        private static Rect _focusedControlRect;
        private static bool _hasFocusedControlRect;

        public static bool IsTextFieldActive => !string.IsNullOrEmpty(_activeTextFieldKey);

        public static bool TryGetFocusedControlRect(out Rect rect)
        {
            rect = _focusedControlRect;
            return _hasFocusedControlRect;
        }

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

        public static bool Button(Rect rect, string text, GUIStyle style, int minFontSize = DefaultMinFontSize,
            bool wordWrap = false, bool navigable = true)
        {
            bool focused = navigable && RegisterControl(rect);
            bool clicked = GUI.Button(rect, text ?? "", FittedStyle(style, rect, text, minFontSize, wordWrap));
            if (focused)
                DrawFocus(rect);
            if (focused && _controller != null && _controller.ConsumeSubmit())
                clicked = true;
            return clicked;
        }

        public static float Slider(Rect rect, float value, float min, float max, float step = 0f)
        {
            bool focused = RegisterControl(rect);
            GUISystemService gui = GUISystemService.Instance;
            float result = GUI.HorizontalSlider(rect, value, min, max,
                gui.SliderStyle, gui.SliderThumbStyle);
            if (focused)
                DrawFocus(new Rect(rect.x, rect.y - 5f, rect.width, rect.height + 10f));
            if (focused && _controller != null)
            {
                int direction = _controller.ConsumeHorizontalMove();
                if (direction != 0)
                {
                    float increment = step > 0f ? step : Mathf.Max(0.01f, (max - min) / 50f);
                    result = Mathf.Clamp(result + increment * direction, min, max);
                }
            }
            return result;
        }

        public static void Panel(Rect rect, GUIStyle style)
        {
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var gui = GUISystemService.Instance;
            GUI.Box(rect, "", style);

            if (gui.AccentSoftTexture == null)
                return;

            float horizontalInset = Mathf.Min(10f, Mathf.Max(3f, rect.width * 0.08f));
            Texture(new Rect(rect.x + horizontalInset, rect.y + 1f,
                Mathf.Max(0f, rect.width - horizontalInset * 2f), 1f), gui.AccentSoftTexture);
            if (rect.height >= 24f)
            {
                float verticalInset = Mathf.Min(9f, rect.height * 0.25f);
                Texture(new Rect(rect.x + 1f, rect.y + verticalInset, 1f,
                    Mathf.Max(0f, rect.height - verticalInset * 2f)), gui.AccentSoftTexture);
            }
        }

        public static void Surface(Rect rect, GUIStyle style)
        {
            if (rect.width <= 0f || rect.height <= 0f || style == null)
                return;

            GUI.Box(rect, string.Empty, style);
        }

        public static void Texture(Rect rect, Texture2D texture)
        {
            if (texture == null || rect.width <= 0f || rect.height <= 0f)
                return;

            GUIStyle style;
            if (!TextureStyleCache.TryGetValue(texture, out style))
            {
                style = new GUIStyle();
                style.normal.background = texture;
                TextureStyleCache[texture] = style;
            }

            GUI.Box(rect, string.Empty, style);
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

            bool focused = RegisterControl(rect);
            if (GUI.Button(rect, display, style) ||
                (focused && _controller != null && _controller.ConsumeSubmit()))
            {
                _activeTextFieldKey = key;
                active = true;
            }
            if (focused)
                DrawFocus(rect);

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

        public static void BeginControllerPass(ControllerInputService controller)
        {
            _controller = controller;
            _controlCount = 0;
            _hasFocusedControlRect = false;
        }

        public static void EndControllerPass()
        {
            if (_controlCount <= 0)
                _focusedControl = 0;
            else
                _focusedControl = Mathf.Clamp(_focusedControl, 0, _controlCount - 1);

            if (_pendingFocusMove != 0 && _controlCount > 0)
            {
                _focusedControl = (_focusedControl + _pendingFocusMove) % _controlCount;
                if (_focusedControl < 0)
                    _focusedControl += _controlCount;
                _pendingFocusMove = 0;
            }
            _controller = null;
        }

        public static void MoveFocus(int direction)
        {
            if (direction != 0)
                _pendingFocusMove += direction;
        }

        public static void ResetFocus()
        {
            _focusedControl = 0;
            _pendingFocusMove = 0;
            _activeTextFieldKey = null;
        }

        public static void DeactivateTextField()
        {
            _activeTextFieldKey = null;
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
            TextureStyleCache.Clear();
            ResetFocus();
        }

        private static bool RegisterControl(Rect rect)
        {
            int index = _controlCount++;
            bool focused = _controller != null && _controller.ControllerActive && index == _focusedControl;
            if (focused)
            {
                _focusedControlRect = rect;
                _hasFocusedControlRect = true;
            }
            return focused;
        }

        private static void DrawFocus(Rect rect)
        {
            GUISystemService gui = GUISystemService.Instance;
            Rect focus = new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f);
            if (gui.FocusStyle != null)
            {
                Surface(focus, gui.FocusStyle);
                return;
            }

            Texture2D texture = gui.AccentTexture;
            if (texture == null)
                return;
            Texture(new Rect(focus.x, focus.y, focus.width, 2f), texture);
            Texture(new Rect(focus.x, focus.yMax - 2f, focus.width, 2f), texture);
            Texture(new Rect(focus.x, focus.y, 2f, focus.height), texture);
            Texture(new Rect(focus.xMax - 2f, focus.y, 2f, focus.height), texture);
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
