using System;
using System.Text;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class KeybindOverlayService
    {
        private static readonly KeybindOverlayService _instance = new KeybindOverlayService();
        public static KeybindOverlayService Instance => _instance;

        private const long RefreshIntervalMs = 1500L;
        private const long ExceptionLogIntervalMs = 2000L;

        private readonly StringBuilder _builder = new StringBuilder(160);
        private bool _enabled = true;
        private string _menuKey = "F8";
        private string _cachedText = string.Empty;
        private long _nextRefreshAtMs;
        private GUIStyle _labelStyle;
        private GUIStyle _shadowStyle;
        private long _nextExceptionLogAtMs;
        private bool _runtimeSupported = true;
        private bool _cachedControllerConnected;
        private bool _cachedPlayStationLayout;

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
            if (!_runtimeSupported)
                return;

            try
            {
                DrawInternal(menuOpen);
            }
            catch (System.NotSupportedException ex)
            {
                _runtimeSupported = false;
                MelonLoader.MelonLogger.Warning(
                    "[Nugzz] Keybind overlay disabled for this session after game update: " + ex.Message);
            }
            catch (Exception ex)
            {
                long now = Environment.TickCount64;
                if (now >= _nextExceptionLogAtMs)
                {
                    _nextExceptionLogAtMs = now + ExceptionLogIntervalMs;
                    MelonLoader.MelonLogger.Warning("[Nugzz] KeybindOverlay.Draw failed: " + ex);
                }
            }
        }

        private void DrawInternal(bool menuOpen)
        {
            if (menuOpen || !_enabled || ManagerCacheService.Instance.LocalPlayer == null ||
                GameplayStateGateService.Instance.IsModControlBlocked(out _))
                return;

            string text = GetCachedText();
            if (string.IsNullOrEmpty(text))
                return;

            EnsureStyle(GUISystemService.Instance);

            float maxWidth = Mathf.Max(280f, Screen.width - 32f);
            float width = Clamp(text.Length * 6f + 20f, 280f, maxWidth);
            float x = Clamp(Screen.width - width - 16f, 8f, Screen.width - width - 8f);
            float y = Clamp(Screen.height - 138f, 44f, Screen.height - 40f);
            Rect rect = new Rect(x, y, width, 24f);

            Rect labelRect = new Rect(rect.x + 8f, rect.y + 3f, rect.width - 16f, 18f);
            GUI.Label(new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height),
                text, _shadowStyle);
            GUI.Label(labelRect, text, _labelStyle);
        }

        private string GetCachedText()
        {
            long now = Environment.TickCount64;
            ControllerInputService input = ControllerInputService.Instance;
            bool controllerConnected = input.ControllerConnected;
            bool playStationLayout = input.IsPlayStation;
            bool inputModeChanged = controllerConnected != _cachedControllerConnected ||
                playStationLayout != _cachedPlayStationLayout;
            if (now >= _nextRefreshAtMs || string.IsNullOrEmpty(_cachedText) || inputModeChanged)
            {
                _cachedText = BuildText();
                _cachedControllerConnected = controllerConnected;
                _cachedPlayStationLayout = playStationLayout;
                _nextRefreshAtMs = now + RefreshIntervalMs;
            }

            return _cachedText;
        }

        private void EnsureStyle(GUISystemService gui)
        {
            if (_labelStyle != null)
                return;

            _labelStyle = new GUIStyle
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip
            };
            _labelStyle.normal.textColor = gui.GetColorForCategory(LabelCategory.Notif);
            if (gui.UIFont != null)
                _labelStyle.font = gui.UIFont;

            _shadowStyle = new GUIStyle(_labelStyle);
            _shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }

        private string BuildText()
        {
            _builder.Length = 0;
            ControllerInputService input = ControllerInputService.Instance;
            if (input.ControllerConnected)
            {
                Append(input.LeftShoulderPrompt + "+" + input.RightShoulderPrompt +
                    "+D-Pad Up Menu");
                Append(input.LeftShoulderPrompt + "+" + input.RightShoulderPrompt +
                    "+D-Pad Down 3rd Person");

                if (FlyingService.Instance.DoubleSpaceHotkeyEnabled)
                    Append(input.ConfirmPrompt + "+" + input.ConfirmPrompt + " Fly");

                if (FlyingService.Instance.Enabled)
                    Append("Fly Move: Left Stick  " + input.ConfirmPrompt + " Up  " +
                        input.CancelPrompt + " Down");
            }
            else
            {
                Append(_menuKey + " Menu");
                Append("G 3rd Person");

                if (FlyingService.Instance.DoubleSpaceHotkeyEnabled)
                    Append("Space+Space Fly");

                if (FlyingService.Instance.Enabled)
                    Append("Fly Move: WASD  Space Up  Ctrl Down");
            }

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
