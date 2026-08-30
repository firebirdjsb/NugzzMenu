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
        public GUIStyle SliderStyle { get; private set; }
        public GUIStyle SliderThumbStyle { get; private set; }
        public GUIStyle TextFieldStyle { get; private set; }
        public GUIStyle PromptChipStyle { get; private set; }
        public GUIStyle ShadowStyle { get; private set; }
        public GUIStyle HeaderBackdropStyle { get; private set; }
        public GUIStyle ContentBackdropStyle { get; private set; }
        public GUIStyle FocusStyle { get; private set; }
        public Font UIFont { get; private set; }
        public Font UIDisplayFont { get; private set; }

        public Texture2D BgTexture { get; private set; }
        public Texture2D DarkTexture { get; private set; }
        public Texture2D PanelTexture { get; private set; }
        public Texture2D ShadowTexture { get; private set; }
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
        public Texture2D FocusTexture { get; private set; }
        public Texture2D SliderTexture { get; private set; }
        public Texture2D SliderThumbTexture { get; private set; }
        public Texture2D LogoTexture { get; private set; }
        public Texture2D BackdropTexture { get; private set; }

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

        private Texture2D CreateRoundedTexture(Color fill, Color border, int radius = 8,
            int borderWidth = 1)
        {
            return CreateRoundedGradientTexture(fill, fill, border, radius, borderWidth);
        }

        private Texture2D CreateRoundedGradientTexture(Color top, Color bottom, Color border,
            int radius = 8, int borderWidth = 1)
        {
            const int size = 64;
            const int samplesPerAxis = 4;
            Texture2D tex = new Texture2D(size, size);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color fill = Color.Lerp(bottom, top, (y + 0.5f) / size);
                    float alpha = 0f;
                    float red = 0f;
                    float green = 0f;
                    float blue = 0f;

                    for (int sampleY = 0; sampleY < samplesPerAxis; sampleY++)
                    {
                        for (int sampleX = 0; sampleX < samplesPerAxis; sampleX++)
                        {
                            float px = x + (sampleX + 0.5f) / samplesPerAxis;
                            float py = y + (sampleY + 0.5f) / samplesPerAxis;
                            bool outer = InsideRoundedRect(px, py, size, 0f, radius);
                            if (!outer)
                                continue;

                            bool inner = borderWidth <= 0 ||
                                InsideRoundedRect(px, py, size, borderWidth, radius);
                            Color sample = inner ? fill : border;
                            alpha += sample.a;
                            red += sample.r * sample.a;
                            green += sample.g * sample.a;
                            blue += sample.b * sample.a;
                        }
                    }

                    const float sampleCount = samplesPerAxis * samplesPerAxis;
                    if (alpha <= 0.0001f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(
                            red / alpha,
                            green / alpha,
                            blue / alpha,
                            alpha / sampleCount));
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static bool InsideRoundedRect(float x, float y, int size, float inset, float radius)
        {
            float left = inset;
            float right = size - inset;
            float bottom = inset;
            float top = size - inset;
            if (x < left || x > right || y < bottom || y > top)
                return false;

            float corner = Math.Max(0.5f, radius - inset);
            if ((x >= left + corner && x <= right - corner) ||
                (y >= bottom + corner && y <= top - corner))
                return true;

            float centerX = x < left + corner ? left + corner : right - corner;
            float centerY = y < bottom + corner ? bottom + corner : top - corner;
            float dx = x - centerX;
            float dy = y - centerY;
            return dx * dx + dy * dy <= corner * corner;
        }

        private Texture2D CreateLogoTexture()
        {
            const int size = 48;
            const int samplesPerAxis = 4;
            Texture2D tex = new Texture2D(size, size);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            const float center = 23.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 0f;
                    float red = 0f;
                    float green = 0f;
                    float blue = 0f;
                    for (int sampleY = 0; sampleY < samplesPerAxis; sampleY++)
                    {
                        for (int sampleX = 0; sampleX < samplesPerAxis; sampleX++)
                        {
                            float px = x + (sampleX + 0.5f) / samplesPerAxis;
                            float py = y + (sampleY + 0.5f) / samplesPerAxis;
                            float dx = (px - center) / 17f;
                            float dy = (py - center) / 22f;
                            float edge = Math.Abs(dx) + Math.Abs(dy);
                            if (edge > 1f)
                                continue;

                            Color sample = dy > 0f
                                ? (dx < 0f ? new Color(0.47f, 0.80f, 0.10f, 1f) : new Color(0.30f, 0.62f, 0.06f, 1f))
                                : (dx < 0f ? new Color(0.24f, 0.52f, 0.05f, 1f) : new Color(0.13f, 0.35f, 0.03f, 1f));
                            if (edge > 0.91f)
                                sample = new Color(0.63f, 0.91f, 0.16f, 1f);
                            alpha += sample.a;
                            red += sample.r * sample.a;
                            green += sample.g * sample.a;
                            blue += sample.b * sample.a;
                        }
                    }

                    const float sampleCount = samplesPerAxis * samplesPerAxis;
                    tex.SetPixel(x, y, alpha <= 0.0001f
                        ? Color.clear
                        : new Color(red / alpha, green / alpha, blue / alpha, alpha / sampleCount));
                }
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

            BgTexture = CreateRoundedGradientTexture(
                new Color(0.026f, 0.052f, 0.034f, 0.992f),
                new Color(0.010f, 0.023f, 0.015f, 0.992f),
                new Color(0.34f, 0.63f, 0.12f, 0.98f), 16, 1);
            DarkTexture = CreateRoundedGradientTexture(
                new Color(0.024f, 0.046f, 0.030f, 0.98f),
                new Color(0.012f, 0.026f, 0.017f, 0.98f),
                new Color(0.12f, 0.24f, 0.11f, 0.92f), 9, 1);
            PanelTexture = CreateRoundedGradientTexture(
                new Color(0.046f, 0.084f, 0.055f, 0.98f),
                new Color(0.026f, 0.052f, 0.034f, 0.98f),
                new Color(0.17f, 0.32f, 0.14f, 0.94f), 10, 1);
            ShadowTexture = CreateRoundedTexture(
                new Color(0f, 0f, 0f, 0.46f), Color.clear, 18, 0);
            AccentTexture = CreateTexture(new Color(0.14f, 0.48f, 0.17f, 1f));
            AccentSoftTexture = CreateTexture(new Color(0.12f, 0.34f, 0.13f, 0.72f));
            ButtonTexture = CreateRoundedGradientTexture(
                new Color(0.070f, 0.122f, 0.080f, 1f),
                new Color(0.040f, 0.076f, 0.050f, 1f),
                new Color(0.13f, 0.23f, 0.12f, 1f), 7, 1);
            ButtonHoverTexture = CreateRoundedGradientTexture(
                new Color(0.11f, 0.23f, 0.12f, 1f),
                new Color(0.065f, 0.145f, 0.074f, 1f),
                new Color(0.38f, 0.68f, 0.13f, 1f), 7, 1);
            ButtonActiveTexture = CreateRoundedGradientTexture(
                new Color(0.15f, 0.46f, 0.18f, 1f),
                new Color(0.075f, 0.27f, 0.10f, 1f),
                new Color(0.61f, 0.91f, 0.16f, 1f), 7, 1);
            HighlightTexture = CreateRoundedGradientTexture(
                new Color(0.11f, 0.28f, 0.13f, 1f),
                new Color(0.060f, 0.18f, 0.075f, 1f),
                new Color(0.50f, 0.86f, 0.14f, 1f), 7, 1);
            TabTexture = CreateRoundedGradientTexture(
                new Color(0.040f, 0.070f, 0.046f, 1f),
                new Color(0.020f, 0.038f, 0.025f, 1f),
                new Color(0.09f, 0.16f, 0.08f, 1f), 7, 1);
            TabActiveTexture = CreateRoundedGradientTexture(
                new Color(0.12f, 0.31f, 0.14f, 1f),
                new Color(0.055f, 0.19f, 0.075f, 1f),
                new Color(0.56f, 0.88f, 0.14f, 1f), 7, 1);
            OnTexture = CreateRoundedGradientTexture(
                new Color(0.35f, 0.68f, 0.16f, 1f),
                new Color(0.19f, 0.46f, 0.09f, 1f),
                new Color(0.73f, 0.93f, 0.24f, 1f), 12, 1);
            OffTexture = CreateRoundedGradientTexture(
                new Color(0.25f, 0.29f, 0.25f, 1f),
                new Color(0.16f, 0.19f, 0.16f, 1f),
                new Color(0.39f, 0.44f, 0.38f, 1f), 12, 1);
            TitleTexture = CreateRoundedGradientTexture(
                new Color(0.075f, 0.135f, 0.087f, 0.98f),
                new Color(0.030f, 0.060f, 0.039f, 0.98f),
                new Color(0.15f, 0.31f, 0.13f, 0.96f), 11, 1);
            NotificationTexture = CreateRoundedGradientTexture(
                new Color(0.065f, 0.115f, 0.070f, 0.98f),
                new Color(0.030f, 0.065f, 0.038f, 0.98f),
                new Color(0.29f, 0.49f, 0.15f, 0.95f), 10, 1);
            FocusTexture = CreateRoundedTexture(
                new Color(0.10f, 0.24f, 0.11f, 0.10f),
                new Color(0.58f, 1f, 0.36f, 0.98f), 9, 2);
            SliderTexture = CreateRoundedTexture(
                new Color(0.055f, 0.085f, 0.058f, 1f),
                new Color(0.16f, 0.27f, 0.14f, 1f), 12, 1);
            SliderThumbTexture = CreateRoundedGradientTexture(
                new Color(0.97f, 0.97f, 0.91f, 1f),
                new Color(0.80f, 0.84f, 0.74f, 1f),
                new Color(0.52f, 0.76f, 0.16f, 1f), 15, 1);
            LogoTexture = CreateLogoTexture();
            BackdropTexture = CreateTexture(new Color(0.002f, 0.008f, 0.004f, 0.22f));

            WindowStyle = new GUIStyle();
            WindowStyle.normal.background = BgTexture;
            WindowStyle.normal.textColor = new Color(0.94f, 0.98f, 0.92f);
            WindowStyle.padding = new RectOffset(10, 10, 10, 10);
            WindowStyle.border = new RectOffset(16, 16, 16, 16);

            ShadowStyle = new GUIStyle();
            ShadowStyle.normal.background = ShadowTexture;
            ShadowStyle.border = new RectOffset(18, 18, 18, 18);

            HeaderBackdropStyle = new GUIStyle();
            HeaderBackdropStyle.normal.background = TitleTexture;
            HeaderBackdropStyle.border = new RectOffset(11, 11, 11, 11);

            ContentBackdropStyle = new GUIStyle();
            ContentBackdropStyle.normal.background = DarkTexture;
            ContentBackdropStyle.border = new RectOffset(9, 9, 9, 9);

            FocusStyle = new GUIStyle();
            FocusStyle.normal.background = FocusTexture;
            FocusStyle.border = new RectOffset(9, 9, 9, 9);

            TabStyle = new GUIStyle();
            TabStyle.normal.background = TabTexture;
            TabStyle.normal.textColor = new Color(0.70f, 0.82f, 0.68f);
            TabStyle.hover.background = ButtonHoverTexture;
            TabStyle.hover.textColor = Color.white;
            TabStyle.active.background = ButtonActiveTexture;
            TabStyle.active.textColor = Color.white;
            TabStyle.alignment = TextAnchor.MiddleCenter;
            TabStyle.fontSize = 11;
            TabStyle.fontStyle = FontStyle.Bold;
            TabStyle.padding = new RectOffset(6, 6, 5, 5);
            TabStyle.border = new RectOffset(7, 7, 7, 7);

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
            OnStyle.border = new RectOffset(12, 12, 12, 12);

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
            ButtonStyle.fontSize = 11;
            ButtonStyle.fontStyle = FontStyle.Bold;
            ButtonStyle.padding = new RectOffset(6, 6, 4, 4);
            ButtonStyle.border = new RectOffset(7, 7, 7, 7);

            SmallButtonStyle = new GUIStyle(ButtonStyle);
            SmallButtonStyle.fontSize = 10;

            BoxStyle = new GUIStyle();
            BoxStyle.normal.background = PanelTexture;
            BoxStyle.padding = new RectOffset(8, 8, 8, 8);
            BoxStyle.border = new RectOffset(10, 10, 10, 10);

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
            GoodButtonStyle.hover.background = CreateRoundedGradientTexture(
                new Color(0.18f, 0.82f, 0.42f, 1f),
                new Color(0.07f, 0.43f, 0.21f, 1f),
                new Color(0.60f, 0.96f, 0.30f, 1f), 7, 1);

            WarningButtonStyle = new GUIStyle(ButtonStyle);
            WarningButtonStyle.normal.background = ButtonActiveTexture;
            WarningButtonStyle.hover.background = CreateRoundedGradientTexture(
                new Color(1f, 0.72f, 0.24f, 1f),
                new Color(0.72f, 0.42f, 0.1f, 1f),
                new Color(1f, 0.80f, 0.34f, 1f), 7, 1);

            NotificationStyle = new GUIStyle();
            NotificationStyle.normal.background = NotificationTexture;
            NotificationStyle.normal.textColor = new Color(0.96f, 1f, 0.92f);
            NotificationStyle.fontSize = 13;
            NotificationStyle.fontStyle = FontStyle.Bold;
            NotificationStyle.alignment = TextAnchor.MiddleCenter;
            NotificationStyle.padding = new RectOffset(10, 10, 6, 6);
            NotificationStyle.border = new RectOffset(10, 10, 10, 10);

            CreditStyle = new GUIStyle();
            CreditStyle.normal.textColor = new Color(0.62f, 0.95f, 0.58f);
            CreditStyle.fontSize = 14;
            CreditStyle.fontStyle = FontStyle.Bold;
            CreditStyle.alignment = TextAnchor.MiddleCenter;
            CreditStyle.padding = new RectOffset(4, 4, 4, 4);

            SliderStyle = new GUIStyle();
            SliderStyle.normal.background = SliderTexture;
            SliderStyle.hover.background = SliderTexture;
            SliderStyle.active.background = SliderTexture;
            SliderStyle.fixedHeight = 8f;
            SliderStyle.border = new RectOffset(10, 10, 4, 4);

            SliderThumbStyle = new GUIStyle();
            SliderThumbStyle.normal.background = SliderThumbTexture;
            SliderThumbStyle.hover.background = SliderThumbTexture;
            SliderThumbStyle.active.background = SliderThumbTexture;
            SliderThumbStyle.fixedWidth = 18f;
            SliderThumbStyle.fixedHeight = 18f;
            SliderThumbStyle.border = new RectOffset(9, 9, 9, 9);
            SliderThumbStyle.overflow = new RectOffset(0, 0, 4, -4);

            TextFieldStyle = new GUIStyle();
            TextFieldStyle.normal.background = DarkTexture;
            TextFieldStyle.focused.background = PanelTexture;
            TextFieldStyle.hover.background = DarkTexture;
            TextFieldStyle.active.background = PanelTexture;
            TextFieldStyle.normal.textColor = new Color(0.94f, 0.98f, 0.92f);
            TextFieldStyle.focused.textColor = Color.white;
            TextFieldStyle.hover.textColor = new Color(0.94f, 0.98f, 0.92f);
            TextFieldStyle.active.textColor = Color.white;
            TextFieldStyle.alignment = TextAnchor.MiddleLeft;
            TextFieldStyle.fontSize = 12;
            TextFieldStyle.border = new RectOffset(9, 9, 9, 9);
            TextFieldStyle.padding = new RectOffset(10, 10, 5, 5);

            PromptChipStyle = new GUIStyle(ButtonStyle);
            PromptChipStyle.normal.background = DarkTexture;
            PromptChipStyle.hover.background = DarkTexture;
            PromptChipStyle.active.background = DarkTexture;
            PromptChipStyle.normal.textColor = new Color(0.92f, 0.95f, 0.86f, 1f);
            PromptChipStyle.alignment = TextAnchor.MiddleCenter;
            PromptChipStyle.fontSize = 11;

            ApplyUIFont(WindowStyle, TabStyle, TabActiveStyle, OnStyle, OffStyle, ButtonStyle, SmallButtonStyle,
                BoxStyle, HeaderStyle, LabelStyle, StatusStyle, TitleStyle, GoodButtonStyle, WarningButtonStyle,
                NotificationStyle, CreditStyle, SliderStyle, SliderThumbStyle, TextFieldStyle, PromptChipStyle);

            ApplyDisplayFont(TabStyle, TabActiveStyle, OnStyle, OffStyle, ButtonStyle, SmallButtonStyle,
                HeaderStyle, TitleStyle, GoodButtonStyle, WarningButtonStyle, NotificationStyle, CreditStyle,
                PromptChipStyle);

            _initialized = true;
        }

        public void Reset()
        {
            _initialized = false;
            UIFont = null;
            UIDisplayFont = null;
        }
    }
}
