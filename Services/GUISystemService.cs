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
    /// Manages GUI styles using native IMGUI without custom font loading.
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

        public Texture2D BgTexture { get; private set; }
        public Texture2D DarkTexture { get; private set; }
        public Texture2D AccentTexture { get; private set; }
        public Texture2D ButtonTexture { get; private set; }
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

        public Color GetColorForCategory(LabelCategory category)
        {
            switch (category)
            {
                case LabelCategory.Title:
                case LabelCategory.Subtitle:
                    return Color.white;
                case LabelCategory.Header:
                    return new Color(0.6f, 0.9f, 0.4f);
                case LabelCategory.Status:
                    return new Color(0.4f, 0.85f, 0.5f);
                case LabelCategory.Error:
                    return Color.red;
                case LabelCategory.Notif:
                case LabelCategory.Box:
                case LabelCategory.Catalog:
                    return Color.white;
                case LabelCategory.Label:
                default:
                    return new Color(0.7f, 0.85f, 0.6f);
            }
        }

        public float GetFontSizeForCategory(LabelCategory category)
        {
            switch (category)
            {
                case LabelCategory.Title:
                    return 20f;
                case LabelCategory.Subtitle:
                    return 11f;
                case LabelCategory.Header:
                    return 16f;
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

            BgTexture = CreateTexture(new Color(0.04f, 0.08f, 0.04f, 0.97f));
            DarkTexture = CreateTexture(new Color(0.02f, 0.05f, 0.02f, 1f));
            AccentTexture = CreateTexture(new Color(0.8f, 0.4f, 0.1f, 1f));
            ButtonTexture = CreateTexture(new Color(0.06f, 0.12f, 0.06f, 1f));
            HighlightTexture = CreateTexture(new Color(0.1f, 0.2f, 0.1f, 1f));
            TabTexture = CreateTexture(new Color(0.04f, 0.08f, 0.04f, 1f));
            TabActiveTexture = CreateTexture(new Color(0.8f, 0.4f, 0.1f, 1f));
            OnTexture = CreateTexture(new Color(0.1f, 0.6f, 0.2f, 1f));
            OffTexture = CreateTexture(new Color(0.7f, 0.2f, 0.1f, 1f));
            TitleTexture = CreateTexture(new Color(0.08f, 0.15f, 0.08f, 1f));
            NotificationTexture = CreateTexture(new Color(0.06f, 0.1f, 0.06f, 0.92f));

            WindowStyle = new GUIStyle();
            WindowStyle.normal.background = BgTexture;
            WindowStyle.normal.textColor = Color.white;
            WindowStyle.padding = new RectOffset(8, 8, 8, 8);

            TabStyle = new GUIStyle();
            TabStyle.normal.background = TabTexture;
            TabStyle.normal.textColor = new Color(0.8f, 0.6f, 0.3f);
            TabStyle.hover.background = HighlightTexture;
            TabStyle.hover.textColor = Color.white;
            TabStyle.alignment = TextAnchor.MiddleCenter;
            TabStyle.fontSize = 11;
            TabStyle.fontStyle = FontStyle.Bold;
            TabStyle.padding = new RectOffset(4, 4, 4, 4);

            TabActiveStyle = new GUIStyle(TabStyle);
            TabActiveStyle.normal.background = TabActiveTexture;
            TabActiveStyle.normal.textColor = Color.white;
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
            OffStyle.hover.background = HighlightTexture;

            ButtonStyle = new GUIStyle();
            ButtonStyle.normal.background = ButtonTexture;
            ButtonStyle.normal.textColor = Color.white;
            ButtonStyle.hover.background = HighlightTexture;
            ButtonStyle.hover.textColor = Color.white;
            ButtonStyle.active.background = AccentTexture;
            ButtonStyle.alignment = TextAnchor.MiddleCenter;
            ButtonStyle.fontSize = 11;
            ButtonStyle.fontStyle = FontStyle.Bold;
            ButtonStyle.padding = new RectOffset(4, 4, 3, 3);

            SmallButtonStyle = new GUIStyle(ButtonStyle);
            SmallButtonStyle.fontSize = 10;

            BoxStyle = new GUIStyle();
            BoxStyle.normal.background = DarkTexture;
            BoxStyle.padding = new RectOffset(4, 4, 4, 4);

            HeaderStyle = new GUIStyle();
            HeaderStyle.normal.textColor = new Color(0.6f, 0.9f, 0.4f);
            HeaderStyle.fontSize = 16;
            HeaderStyle.fontStyle = FontStyle.Bold;
            HeaderStyle.padding = new RectOffset(4, 4, 3, 1);

            LabelStyle = new GUIStyle();
            LabelStyle.normal.textColor = new Color(0.7f, 0.85f, 0.6f);
            LabelStyle.fontSize = 12;
            LabelStyle.padding = new RectOffset(4, 4, 1, 1);

            StatusStyle = new GUIStyle();
            StatusStyle.normal.textColor = new Color(0.4f, 0.85f, 0.5f);
            StatusStyle.fontSize = 11;
            StatusStyle.fontStyle = FontStyle.Italic;
            StatusStyle.alignment = TextAnchor.MiddleCenter;

            TitleStyle = new GUIStyle();
            TitleStyle.normal.textColor = Color.white;
            TitleStyle.fontSize = 20;
            TitleStyle.fontStyle = FontStyle.Bold;
            TitleStyle.padding = new RectOffset(8, 4, 2, 1);

            GoodButtonStyle = new GUIStyle(ButtonStyle);
            GoodButtonStyle.normal.background = CreateTexture(new Color(0.04f, 0.4f, 0.1f, 1f));
            GoodButtonStyle.hover.background = CreateTexture(new Color(0.06f, 0.6f, 0.15f, 1f));

            WarningButtonStyle = new GUIStyle(ButtonStyle);
            WarningButtonStyle.normal.background = CreateTexture(new Color(0.5f, 0.3f, 0.1f, 1f));
            WarningButtonStyle.hover.background = CreateTexture(new Color(0.7f, 0.45f, 0.15f, 1f));

            NotificationStyle = new GUIStyle();
            NotificationStyle.normal.background = NotificationTexture;
            NotificationStyle.normal.textColor = Color.white;
            NotificationStyle.fontSize = 13;
            NotificationStyle.fontStyle = FontStyle.Bold;
            NotificationStyle.alignment = TextAnchor.MiddleCenter;
            NotificationStyle.padding = new RectOffset(10, 10, 6, 6);

            CreditStyle = new GUIStyle();
            CreditStyle.normal.textColor = new Color(0.6f, 0.9f, 0.5f);
            CreditStyle.fontSize = 14;
            CreditStyle.fontStyle = FontStyle.Bold;
            CreditStyle.alignment = TextAnchor.MiddleCenter;
            CreditStyle.padding = new RectOffset(4, 4, 4, 4);

            _initialized = true;
        }

        public void ApplyFontToSkin()
        {
            // No-op with native IMGUI
        }

        public void Reset()
        {
            _initialized = false;
        }
    }
}