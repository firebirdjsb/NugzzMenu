using System;
using UnityEngine;

namespace NugzzMenu.Services
{
    public enum LabelCategory
    {
        Title,
        Subtitle,
        Header,
        Label,
        Status,
        Error,
        Notif,
        Catalog,
        Box
    }

    /// <summary>
    /// Manages lightweight native IMGUI styles.
    /// </summary>
    public sealed class GUISystemService
    {
        private static readonly GUISystemService _instance = new GUISystemService();
        public static GUISystemService Instance => _instance;

        public GUIStyle WindowStyle { get; private set; }
        public GUIStyle TabStyle { get; private set; }
        public GUIStyle TabActiveStyle { get; private set; }
        public GUIStyle OnStyle { get; private set; }
        public GUIStyle OffStyle { get; private set; }
        public GUIStyle ButtonStyle { get; private set; }
        public GUIStyle BoxStyle { get; private set; }
        public GUIStyle SmallButtonStyle { get; private set; }
        public GUIStyle HeaderStyle { get; private set; }
        public GUIStyle LabelStyle { get; private set; }
        public GUIStyle StatusStyle { get; private set; }
        public GUIStyle TitleStyle { get; private set; }
        public GUIStyle GoodButtonStyle { get; private set; }
        public GUIStyle WarningButtonStyle { get; private set; }
        public GUIStyle NotificationStyle { get; private set; }
        public GUIStyle CreditStyle { get; private set; }
        public Font UIFont { get; private set; }
        public Font UIDisplayFont { get; private set; }

        public Texture2D BgTexture { get; private set; }
        public Texture2D DarkTexture { get; private set; }
        public Texture2D PanelTexture { get; private set; }
        public Texture2D ShadowTexture { get; private set; }
        public Texture2D BorderTexture { get; private set; }
        public Texture2D AccentTexture { get; private set; }
        public Texture2D AccentSoftTexture { get; private set; }
        public Texture2D ButtonTexture { get; private set; }
        public Texture2D ButtonHoverTexture { get; private set; }
        public Texture2D ButtonActiveTexture { get; private set; }
        public Texture2D HighlightTexture { get; private set; }
        public Texture2D TabTexture { get; private set; }
        public Texture2D TabActiveTexture { get; private set; }
        public Texture2D OnTexture { get; private set; }
        public Texture2D OffTexture { get; private set; }
        public Texture2D TitleTexture { get; private set; }
        public Texture2D NotificationTexture { get; private set; }

        private bool _initialized = false;

        private GUISystemService() { }

        private Texture2D CreateTexture(Color c)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.hideFlags = HideFlags.HideAndDontSave;
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    tex.SetPixel(i, j, c);
            tex.Apply();
            return tex;
        }

        private Texture2D CreateVerticalTexture(Color top, Color bottom)
        {
            Texture2D tex = new Texture2D(2, 16);
            tex.hideFlags = HideFlags.HideAndDontSave;
            for (int y = 0; y < 16; y++)
            {
                Color c = Color.Lerp(bottom, top, y / 15f);
                tex.SetPixel(0, y, c);
                tex.SetPixel(1, y, c);
            }

            tex.Apply();
            return tex;
        }

        private Font CreateFont(string[] preferredFonts, int size)
        {
            for (int i = 0; i < preferredFonts.Length; i++)
            {
                try
                {
                    Font font = Font.CreateDynamicFontFromOSFont(preferredFonts[i], size);
                    if (font != null)
                    {
                        font.hideFlags = HideFlags.HideAndDontSave;
                        return font;
                    }
                }
                catch
                {
                    // Some Unity/IL2CPP runtimes reject OS font creation. Fall back quietly.
                }
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private Font CreateUIFont()
        {
            return CreateFont(new[]
            {
                "Segoe UI Semibold",
                "Bahnschrift",
                "Segoe UI",
                "Trebuchet MS",
                "Verdana"
            }, 16);
        }

        private Font CreateDisplayFont()
        {
            return CreateFont(new[]
            {
                "Agency FB",
                "Bahnschrift Condensed",
                "Bahnschrift SemiBold",
                "Franklin Gothic Medium",
                "Segoe UI Black",
                "Impact"
            }, 18);
        }

        private void ApplyUIFont(params GUIStyle[] styles)
        {
            if (UIFont == null || styles == null)
                return;

            for (int i = 0; i < styles.Length; i++)
            {
                if (styles[i] != null)
                    styles[i].font = UIFont;
            }
        }

        private void ApplyDisplayFont(params GUIStyle[] styles)
        {
            if (UIDisplayFont == null || styles == null)
                return;

            for (int i = 0; i < styles.Length; i++)
            {
                if (styles[i] != null)
                    styles[i].font = UIDisplayFont;
            }
        }

        public Font GetFontForText(float fontSize, FontStyle style)
        {
            if ((fontSize >= 14f || style == FontStyle.BoldAndItalic) && UIDisplayFont != null)
                return UIDisplayFont;

            return UIFont ?? UIDisplayFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public Color GetColorForCategory(LabelCategory category)
        {
            switch (category)
            {
                case LabelCategory.Title:
                case LabelCategory.Subtitle:
                    return new Color(0.98f, 1f, 0.96f);
                case LabelCategory.Header:
                    return new Color(0.62f, 0.95f, 0.58f);
                case LabelCategory.Status:
                    return new Color(0.68f, 0.96f, 0.58f);
                case LabelCategory.Error:
                    return new Color(1f, 0.42f, 0.36f);
                case LabelCategory.Notif:
                case LabelCategory.Box:
                case LabelCategory.Catalog:
                    return new Color(0.94f, 0.98f, 0.92f);
                case LabelCategory.Label:
                default:
                    return new Color(0.78f, 0.88f, 0.76f);
            }
        }

        public float GetFontSizeForCategory(LabelCategory category)
        {
            switch (category)
            {
                case LabelCategory.Title:
                    return 24f;
                case LabelCategory.Subtitle:
                    return 11f;
                case LabelCategory.Header:
                    return 14f;
                case LabelCategory.Status:
                    return 11f;
                case LabelCategory.Label:
                default:
                    return 12f;
            }
        }

        public FontStyle GetStyleForCategory(LabelCategory category)
        {
            switch (category)
            {
                case LabelCategory.Title:
                case LabelCategory.Header:
                case LabelCategory.Subtitle:
                case LabelCategory.Label:
                    return FontStyle.Bold;
                case LabelCategory.Status:
                    return FontStyle.Italic;
                default:
                    return FontStyle.Normal;
            }
        }

        public TextAnchor GetAlignmentForCategory(LabelCategory category)
        {
            switch (category)
            {
                case LabelCategory.Title:
                    return TextAnchor.UpperLeft;
                case LabelCategory.Subtitle:
                    return TextAnchor.UpperRight;
                case LabelCategory.Status:
                    return TextAnchor.MiddleCenter;
                default:
                    return TextAnchor.MiddleLeft;
            }
        }

        public void Initialize()
        {
            if (_initialized) return;

            UIFont = CreateUIFont();
            UIDisplayFont = CreateDisplayFont();

            BgTexture = CreateVerticalTexture(
                new Color(0.055f, 0.075f, 0.058f, 0.98f),
                new Color(0.018f, 0.028f, 0.022f, 0.98f));
            DarkTexture = CreateTexture(new Color(0.025f, 0.04f, 0.03f, 0.96f));
            PanelTexture = CreateTexture(new Color(0.045f, 0.065f, 0.048f, 0.94f));
            ShadowTexture = CreateTexture(new Color(0f, 0f, 0f, 0.42f));
            BorderTexture = CreateTexture(new Color(0.18f, 0.34f, 0.17f, 0.88f));
            AccentTexture = CreateTexture(new Color(0.14f, 0.48f, 0.17f, 1f));
            AccentSoftTexture = CreateTexture(new Color(0.07f, 0.25f, 0.09f, 0.9f));
            ButtonTexture = CreateVerticalTexture(
                new Color(0.085f, 0.12f, 0.09f, 1f),
                new Color(0.052f, 0.075f, 0.057f, 1f));
            ButtonHoverTexture = CreateVerticalTexture(
                new Color(0.10f, 0.24f, 0.12f, 1f),
                new Color(0.06f, 0.15f, 0.075f, 1f));
            ButtonActiveTexture = CreateVerticalTexture(
                new Color(0.95f, 0.62f, 0.18f, 1f),
                new Color(0.66f, 0.34f, 0.08f, 1f));
            HighlightTexture = CreateTexture(new Color(0.08f, 0.22f, 0.10f, 1f));
            TabTexture = CreateTexture(new Color(0.036f, 0.052f, 0.04f, 1f));
            TabActiveTexture = CreateVerticalTexture(
                new Color(0.15f, 0.46f, 0.17f, 1f),
                new Color(0.07f, 0.24f, 0.09f, 1f));
            OnTexture = CreateVerticalTexture(
                new Color(0.15f, 0.72f, 0.38f, 1f),
                new Color(0.055f, 0.34f, 0.17f, 1f));
            OffTexture = CreateVerticalTexture(
                new Color(0.66f, 0.22f, 0.18f, 1f),
                new Color(0.34f, 0.095f, 0.08f, 1f));
            TitleTexture = CreateVerticalTexture(
                new Color(0.08f, 0.13f, 0.085f, 1f),
                new Color(0.034f, 0.055f, 0.038f, 1f));
            NotificationTexture = CreateTexture(new Color(0.04f, 0.075f, 0.045f, 0.96f));

            WindowStyle = new GUIStyle();
            WindowStyle.normal.background = BgTexture;
            WindowStyle.normal.textColor = new Color(0.94f, 0.98f, 0.92f);
            WindowStyle.padding = new RectOffset(10, 10, 10, 10);

            TabStyle = new GUIStyle();
            TabStyle.normal.background = TabTexture;
            TabStyle.normal.textColor = new Color(0.70f, 0.82f, 0.68f);
            TabStyle.hover.background = ButtonHoverTexture;
            TabStyle.hover.textColor = Color.white;
            TabStyle.active.background = ButtonActiveTexture;
            TabStyle.active.textColor = Color.white;
            TabStyle.alignment = TextAnchor.MiddleCenter;
            TabStyle.fontSize = 12;
            TabStyle.fontStyle = FontStyle.Bold;
            TabStyle.padding = new RectOffset(6, 6, 5, 5);

            TabActiveStyle = new GUIStyle(TabStyle);
            TabActiveStyle.normal.background = TabActiveTexture;
            TabActiveStyle.normal.textColor = new Color(0.98f, 1f, 0.95f);
            TabActiveStyle.hover.background = TabActiveTexture;

            OnStyle = new GUIStyle();
            OnStyle.normal.background = OnTexture;
            OnStyle.normal.textColor = Color.white;
            OnStyle.alignment = TextAnchor.MiddleCenter;
            OnStyle.fontSize = 11;
            OnStyle.fontStyle = FontStyle.Bold;
            OnStyle.padding = new RectOffset(4, 4, 3, 3);

            OffStyle = new GUIStyle(OnStyle);
            OffStyle.normal.background = OffTexture;
            OffStyle.hover.background = ButtonHoverTexture;

            ButtonStyle = new GUIStyle();
            ButtonStyle.normal.background = ButtonTexture;
            ButtonStyle.normal.textColor = new Color(0.92f, 0.98f, 0.90f);
            ButtonStyle.hover.background = ButtonHoverTexture;
            ButtonStyle.hover.textColor = new Color(0.98f, 1f, 0.94f);
            ButtonStyle.active.background = ButtonActiveTexture;
            ButtonStyle.active.textColor = Color.white;
            ButtonStyle.alignment = TextAnchor.MiddleCenter;
            ButtonStyle.fontSize = 12;
            ButtonStyle.fontStyle = FontStyle.Bold;
            ButtonStyle.padding = new RectOffset(6, 6, 4, 4);

            SmallButtonStyle = new GUIStyle(ButtonStyle);
            SmallButtonStyle.fontSize = 10;

            BoxStyle = new GUIStyle();
            BoxStyle.normal.background = PanelTexture;
            BoxStyle.padding = new RectOffset(8, 8, 8, 8);

            HeaderStyle = new GUIStyle();
            HeaderStyle.normal.textColor = new Color(0.62f, 0.95f, 0.58f);
            HeaderStyle.fontSize = 14;
            HeaderStyle.fontStyle = FontStyle.Bold;
            HeaderStyle.padding = new RectOffset(4, 4, 3, 1);

            LabelStyle = new GUIStyle();
            LabelStyle.normal.textColor = new Color(0.78f, 0.88f, 0.76f);
            LabelStyle.fontSize = 12;
            LabelStyle.fontStyle = FontStyle.Bold;
            LabelStyle.padding = new RectOffset(4, 4, 1, 1);

            StatusStyle = new GUIStyle();
            StatusStyle.normal.textColor = new Color(0.68f, 0.96f, 0.58f);
            StatusStyle.fontSize = 11;
            StatusStyle.fontStyle = FontStyle.Italic;
            StatusStyle.alignment = TextAnchor.MiddleCenter;

            TitleStyle = new GUIStyle();
            TitleStyle.normal.textColor = new Color(0.98f, 1f, 0.96f);
            TitleStyle.fontSize = 24;
            TitleStyle.fontStyle = FontStyle.Bold;
            TitleStyle.padding = new RectOffset(8, 4, 2, 1);

            GoodButtonStyle = new GUIStyle(ButtonStyle);
            GoodButtonStyle.normal.background = OnTexture;
            GoodButtonStyle.hover.background = CreateVerticalTexture(
                new Color(0.18f, 0.82f, 0.42f, 1f),
                new Color(0.07f, 0.43f, 0.21f, 1f));

            WarningButtonStyle = new GUIStyle(ButtonStyle);
            WarningButtonStyle.normal.background = ButtonActiveTexture;
            WarningButtonStyle.hover.background = CreateVerticalTexture(
                new Color(1f, 0.72f, 0.24f, 1f),
                new Color(0.72f, 0.42f, 0.1f, 1f));

            NotificationStyle = new GUIStyle();
            NotificationStyle.normal.background = NotificationTexture;
            NotificationStyle.normal.textColor = new Color(0.96f, 1f, 0.92f);
            NotificationStyle.fontSize = 13;
            NotificationStyle.fontStyle = FontStyle.Bold;
            NotificationStyle.alignment = TextAnchor.MiddleCenter;
            NotificationStyle.padding = new RectOffset(10, 10, 6, 6);

            CreditStyle = new GUIStyle();
            CreditStyle.normal.textColor = new Color(0.62f, 0.95f, 0.58f);
            CreditStyle.fontSize = 14;
            CreditStyle.fontStyle = FontStyle.Bold;
            CreditStyle.alignment = TextAnchor.MiddleCenter;
            CreditStyle.padding = new RectOffset(4, 4, 4, 4);

            ApplyUIFont(WindowStyle, TabStyle, TabActiveStyle, OnStyle, OffStyle, ButtonStyle, SmallButtonStyle,
                BoxStyle, HeaderStyle, LabelStyle, StatusStyle, TitleStyle, GoodButtonStyle, WarningButtonStyle,
                NotificationStyle, CreditStyle);

            ApplyDisplayFont(TabStyle, TabActiveStyle, OnStyle, OffStyle, ButtonStyle, SmallButtonStyle,
                HeaderStyle, TitleStyle, GoodButtonStyle, WarningButtonStyle, NotificationStyle, CreditStyle);

            _initialized = true;
        }

        public void ApplyFontToSkin()
        {
            if (GUI.skin == null)
                return;

            GUI.skin.button = ButtonStyle ?? GUI.skin.button;
            GUI.skin.box = BoxStyle ?? GUI.skin.box;
            GUI.skin.label = LabelStyle ?? GUI.skin.label;
            if (UIFont != null)
                GUI.skin.textField.font = UIFont;
            GUI.skin.textField.normal.background = DarkTexture;
            GUI.skin.textField.focused.background = PanelTexture;
            GUI.skin.textField.normal.textColor = new Color(0.94f, 0.98f, 0.92f);
            GUI.skin.textField.focused.textColor = Color.white;
        }

        public void Reset()
        {
            _initialized = false;
            UIFont = null;
            UIDisplayFont = null;
        }
    }
}
