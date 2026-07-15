using System;
using System.Text;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class KeybindOverlayService
    {
        private static readonly KeybindOverlayService _instance = new KeybindOverlayService();
        public static KeybindOverlayService Instance => _instance;

        private const float RefreshInterval = 1.5f;

        private readonly StringBuilder _builder = new StringBuilder(160);
        private readonly GUIContent _content = new GUIContent();
        private bool _enabled = true;
        private string _menuKey = "F8";
        private string _cachedText = string.Empty;
        private float _nextRefreshTime;
        private GUIStyle _labelStyle;

        public bool Enabled => _enabled;

        private KeybindOverlayService() { }

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        public void SetMenuKey(string key)
        {
            _menuKey = string.IsNullOrEmpty(key) ? "F8" : key;
            _cachedText = string.Empty;
        }

        public void Draw(bool menuOpen)
        {
            if (menuOpen || !_enabled || ManagerCacheService.Instance.LocalPlayer == null)
                return;

            Event current = Event.current;
            if (current != null && current.type != EventType.Repaint)
                return;

            string text = GetCachedText();
            if (string.IsNullOrEmpty(text))
                return;

            GUISystemService gui = GUISystemService.Instance;
            EnsureStyle(gui);

            float width = Mathf.Clamp(text.Length * 6f + 20f, 280f, 440f);
            float x = Mathf.Clamp(Screen.width - width - 16f, 8f, Screen.width - width - 8f);
            float y = Mathf.Clamp(Screen.height - 138f, 44f, Screen.height - 40f);
            Rect rect = new Rect(x, y, width, 24f);

            if (gui.NotificationTexture != null)
                GUI.DrawTexture(rect, gui.NotificationTexture);
            else
                GUI.Box(rect, GUIContent.none);

            if (gui.AccentTexture != null)
                GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), gui.AccentTexture);

            _content.text = text;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 3f, rect.width - 16f, 18f), _content, _labelStyle);
        }

        private string GetCachedText()
        {
            float now = Time.unscaledTime;
            if (now >= _nextRefreshTime || string.IsNullOrEmpty(_cachedText))
            {
                _cachedText = BuildText();
                _nextRefreshTime = now + RefreshInterval;
            }

            return _cachedText;
        }

        private void EnsureStyle(GUISystemService gui)
        {
            if (_labelStyle != null)
                return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip
            };
            _labelStyle.normal.textColor = gui.GetColorForCategory(LabelCategory.Notif);
            if (gui.UIFont != null)
                _labelStyle.font = gui.UIFont;
        }

        private string BuildText()
        {
            _builder.Length = 0;
            Append(_menuKey + " Menu");
            Append("G 3rd Person");

            if (FlyingService.Instance.DoubleSpaceHotkeyEnabled)
                Append("Space+Space Fly");

            if (FlyingService.Instance.Enabled)
                Append("Fly Move: WASD Space Ctrl");

            return _builder.ToString();
        }

        private void Append(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (_builder.Length > 0)
                _builder.Append("  |  ");
            _builder.Append(text);
        }
    }
}
