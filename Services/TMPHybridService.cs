using System;
using System.Collections.Generic;
using UnityEngine;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Simple wrapper that uses native GUI.Label for text rendering.
    /// </summary>
    public sealed class TMPHybridService
    {
        private static readonly TMPHybridService _instance = new TMPHybridService();
        public static TMPHybridService Instance => _instance;
        private readonly Dictionary<StyleKey, GUIStyle> _styles = new Dictionary<StyleKey, GUIStyle>();

        private TMPHybridService() { }

        public void Label(
            float x,
            float y,
            float w,
            float h,
            string textString,
            Color color,
            float fontSize = 12f,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            FontStyle style = FontStyle.Normal,
            bool wordWrap = false)
        {
            var key = new StyleKey(color, (int)fontSize, alignment, style, wordWrap);
            GUIStyle guiStyle;
            if (!_styles.TryGetValue(key, out guiStyle))
            {
                guiStyle = new GUIStyle(GUISystemService.Instance.LabelStyle ?? GUI.skin.label);
                guiStyle.normal.textColor = color;
                guiStyle.fontSize = (int)fontSize;
                guiStyle.alignment = alignment;
                guiStyle.fontStyle = style;
                guiStyle.wordWrap = wordWrap;
                guiStyle.clipping = TextClipping.Clip;
                guiStyle.font = GUISystemService.Instance.GetFontForText(fontSize, style);
                GUIFit.EnsureFont(guiStyle);
                _styles[key] = guiStyle;
            }

            GUI.Label(new Rect(x, y, w, h), string.IsNullOrEmpty(textString) ? "" : textString, guiStyle);
        }

        public void Reset()
        {
            _styles.Clear();
        }

        private readonly struct StyleKey : IEquatable<StyleKey>
        {
            private readonly Color _color;
            private readonly int _fontSize;
            private readonly TextAnchor _alignment;
            private readonly FontStyle _fontStyle;
            private readonly bool _wordWrap;

            public StyleKey(Color color, int fontSize, TextAnchor alignment, FontStyle fontStyle, bool wordWrap)
            {
                _color = color;
                _fontSize = fontSize;
                _alignment = alignment;
                _fontStyle = fontStyle;
                _wordWrap = wordWrap;
            }

            public bool Equals(StyleKey other)
            {
                return _color.Equals(other._color) &&
                    _fontSize == other._fontSize &&
                    _alignment == other._alignment &&
                    _fontStyle == other._fontStyle &&
                    _wordWrap == other._wordWrap;
            }

            public override bool Equals(object obj)
            {
                return obj is StyleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = _color.GetHashCode();
                    hash = (hash * 397) ^ _fontSize;
                    hash = (hash * 397) ^ (int)_alignment;
                    hash = (hash * 397) ^ (int)_fontStyle;
                    hash = (hash * 397) ^ (_wordWrap ? 1 : 0);
                    return hash;
                }
            }
        }
    }
}
