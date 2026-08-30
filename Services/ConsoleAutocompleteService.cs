using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne.UI;
using UnityEngine;
using UnityEngine.UI;
using GameConsole = Il2CppScheduleOne.Console;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Adds command discovery and completion directly to the vanilla console UI.
    /// Command submission remains entirely owned by the game.
    /// </summary>
    public sealed class ConsoleAutocompleteService
    {
        private const int MaxSuggestions = 8;
        private const float TitleHeight = 27f;
        private const float StructureHeight = 24f;
        private const float HelperHeight = 38f;
        private const float RowHeight = 23f;
        private const float RowsTop = 96f;
        private static readonly ConsoleAutocompleteService _instance =
            new ConsoleAutocompleteService();

        private readonly List<CommandInfo> _commands = new List<CommandInfo>();
        private readonly List<CommandInfo> _matches = new List<CommandInfo>();
        private readonly RectTransform[] _rowRects = new RectTransform[MaxSuggestions];
        private readonly Image[] _rowBackgrounds = new Image[MaxSuggestions];
        private readonly Text[] _rowLabels = new Text[MaxSuggestions];

        private ConsoleUI _console;
        private object _inputField;
        private FieldInfo _inputFieldField;
        private PropertyInfo _inputFieldProperty;
        private PropertyInfo _textProperty;
        private PropertyInfo _focusedProperty;
        private PropertyInfo _caretProperty;
        private MethodInfo _activateMethod;
        private GameObject _panelRoot;
        private RectTransform _panelRect;
        private Text _titleLabel;
        private Text _structureLabel;
        private Text _helperLabel;
        private Canvas _canvas;
        private Font _font;
        private readonly Vector3[] _inputCorners = new Vector3[4];
        private float _nextFindTime;
        private string _lastText;
        private int _knownCommandCount = -1;
        private int _selectedIndex;
        private int _windowStart;

        public static ConsoleAutocompleteService Instance => _instance;
        public bool IsTyping { get; private set; }
        public bool HasSuggestions => IsTyping && _matches.Count > 0;

        private ConsoleAutocompleteService() { }

        public void Update()
        {
            if (!TryBindConsole())
                return;

            try
            {
                if (!IsConsoleOpen())
                {
                    if (IsTyping || _matches.Count > 0)
                        ClearSuggestions();
                    IsTyping = false;
                    return;
                }

                ResolveInputField();
                IsTyping = _inputField != null &&
                           GetBool(_focusedProperty, _inputField);
            }
            catch
            {
                ResetConsoleReference();
                return;
            }

            if (!IsTyping)
            {
                ClearSuggestions();
                return;
            }

            RefreshCommandCache();
            RefreshMatchesIfNeeded();
            EnsureNativePanel();
            RefreshNativePanel();
            HandleMouseInput();
        }

        public bool HandleNativeConsoleInput(ConsoleUI console)
        {
            if (console == null)
                return false;

            if (_console != console)
                BindConsole(console);

            try
            {
                ResolveInputField();
                IsTyping = IsConsoleOpen() && _inputField != null &&
                           GetBool(_focusedProperty, _inputField);
                if (!IsTyping)
                    return false;

                RefreshCommandCache();
                RefreshMatchesIfNeeded();
                if (_matches.Count == 0)
                    return false;

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    MoveSelection(1);
                    return true;
                }

                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    MoveSelection(-1);
                    return true;
                }

                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    Complete(_matches[_selectedIndex].Word);
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException(
                    "Native console suggestion input failed", ex);
                ResetConsoleReference();
            }

            return false;
        }

        public void ResetForScene()
        {
            ResetConsoleReference();
            _nextFindTime = 0f;
        }

        private bool TryBindConsole()
        {
            if (_console != null)
                return true;

            IsTyping = false;
            SetPanelActive(false);
            if (Time.unscaledTime < _nextFindTime)
                return false;

            _nextFindTime = Time.unscaledTime + 1f;
            try { BindConsole(UnityEngine.Object.FindObjectOfType<ConsoleUI>()); }
            catch { BindConsole(null); }
            return _console != null;
        }

        private void BindConsole(ConsoleUI console)
        {
            if (_console == console)
                return;

            DestroyNativePanel();
            _console = console;
            _inputField = null;
            _inputFieldField = null;
            _inputFieldProperty = null;
            _textProperty = null;
            _focusedProperty = null;
            _caretProperty = null;
            _activateMethod = null;
            _canvas = null;
            _lastText = null;
            _matches.Clear();
            _selectedIndex = 0;
            _windowStart = 0;
        }

        private void ResolveInputField()
        {
            if (_inputField != null || _console == null)
                return;

            Type consoleType = _console.GetType();
            _inputFieldProperty = consoleType.GetProperty(
                "InputField", BindingFlags.Instance | BindingFlags.Public);
            _inputFieldField = consoleType.GetField(
                "InputField", BindingFlags.Instance | BindingFlags.Public);
            _inputField = _inputFieldProperty?.GetValue(_console) ??
                          _inputFieldField?.GetValue(_console);
            if (_inputField == null)
                return;

            Type inputType = _inputField.GetType();
            _textProperty = inputType.GetProperty(
                "text", BindingFlags.Instance | BindingFlags.Public);
            _focusedProperty = inputType.GetProperty(
                "isFocused", BindingFlags.Instance | BindingFlags.Public);
            _caretProperty = inputType.GetProperty(
                "caretPosition", BindingFlags.Instance | BindingFlags.Public);
            _activateMethod = inputType.GetMethod(
                "ActivateInputField", BindingFlags.Instance | BindingFlags.Public,
                null, Type.EmptyTypes, null);
        }

        private bool IsConsoleOpen()
        {
            try
            {
                return _console != null && _console.Container != null &&
                       _console.Container.activeInHierarchy;
            }
            catch { return false; }
        }

        private void RefreshCommandCache()
        {
            try
            {
                var source = GameConsole.Commands;
                int count = source?.Count ?? 0;
                if (count == _knownCommandCount && _commands.Count > 0)
                    return;

                _knownCommandCount = count;
                _commands.Clear();
                for (int i = 0; i < count; i++)
                {
                    GameConsole.ConsoleCommand command = source[i];
                    if (command == null || string.IsNullOrWhiteSpace(command.CommandWord))
                        continue;

                    _commands.Add(new CommandInfo(
                        command.CommandWord.Trim(),
                        command.CommandDescription,
                        command.ExampleUsage,
                        ResolveSource(command)));
                }

                _commands.Sort((a, b) => string.Compare(
                    a.Word, b.Word, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException(
                    "Console command cache failed", ex);
            }
        }

        private void RefreshMatchesIfNeeded()
        {
            string text = GetText();
            if (_lastText != null &&
                string.Equals(text, _lastText, StringComparison.Ordinal))
                return;

            _lastText = text;
            RebuildMatches(text);
        }

        private void RebuildMatches(string input)
        {
            _matches.Clear();
            _selectedIndex = 0;
            _windowStart = 0;
            string token = FirstToken(input);
            for (int i = 0; i < _commands.Count; i++)
            {
                CommandInfo command = _commands[i];
                if (token.Length == 0 ||
                    command.Word.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                    _matches.Add(command);
            }

            if (_matches.Count > 0 || token.Length == 0)
                return;

            for (int i = 0; i < _commands.Count; i++)
            {
                CommandInfo command = _commands[i];
                if (command.Word.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    _matches.Add(command);
            }
        }

        private void EnsureNativePanel()
        {
            if (_panelRoot != null || _console == null)
                return;

            try
            {
                Transform parent = _console.canvas != null
                    ? _console.canvas.transform
                    : _console.Container?.transform;
                if (parent == null)
                    return;

                _canvas = _console.canvas;
                _font = GetBuiltinFont();
                _panelRoot = CreateRectObject("NugzzCommandSuggestions", parent,
                    out _panelRect);
                _panelRect.anchorMin = new Vector2(0f, 1f);
                _panelRect.anchorMax = new Vector2(1f, 1f);
                _panelRect.pivot = new Vector2(0.5f, 1f);
                _panelRect.anchoredPosition = new Vector2(0f, -48f);
                AddImage(_panelRoot, new Color(0.025f, 0.03f, 0.045f, 0.97f));

                GameObject accent = CreateRectObject("Accent", _panelRect,
                    out RectTransform accentRect);
                accentRect.anchorMin = new Vector2(0f, 1f);
                accentRect.anchorMax = new Vector2(1f, 1f);
                accentRect.pivot = new Vector2(0.5f, 1f);
                accentRect.anchoredPosition = Vector2.zero;
                accentRect.sizeDelta = new Vector2(0f, 2f);
                AddImage(accent, new Color(0.22f, 0.62f, 0.92f, 1f));

                GameObject title = CreateRectObject("Title", _panelRect,
                    out RectTransform titleRect);
                SetTopRow(titleRect, 10f, 4f, TitleHeight);
                _titleLabel = AddText(title,
                    "Schedule I - Console Auto-Complete (NUGZZ)",
                    18, FontStyle.Bold, new Color(0.95f, 0.97f, 1f, 1f));
                _titleLabel.supportRichText = false;

                GameObject structure = CreateRectObject("Structure", _panelRect,
                    out RectTransform structureRect);
                SetTopRow(structureRect, 10f, 31f, StructureHeight);
                _structureLabel = AddText(structure, string.Empty, 15,
                    FontStyle.Bold, new Color(0.86f, 0.94f, 0.76f, 1f));
                _structureLabel.supportRichText = false;

                GameObject helper = CreateRectObject("Helper", _panelRect,
                    out RectTransform helperRect);
                SetTopRow(helperRect, 10f, 55f, HelperHeight);
                _helperLabel = AddText(helper, string.Empty, 13,
                    FontStyle.Normal, new Color(0.8f, 0.84f, 0.9f, 1f));
                _helperLabel.supportRichText = false;
                _helperLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
                _helperLabel.verticalOverflow = VerticalWrapMode.Truncate;

                for (int i = 0; i < MaxSuggestions; i++)
                {
                    GameObject row = CreateRectObject("Suggestion" + i,
                        _panelRect, out _rowRects[i]);
                    SetTopRow(_rowRects[i], 10f,
                        RowsTop + i * RowHeight, RowHeight - 1f);
                    _rowBackgrounds[i] = AddImage(row,
                        new Color(0.035f, 0.045f, 0.06f, 0.5f));

                    GameObject label = CreateRectObject("Label", _rowRects[i],
                        out RectTransform labelRect);
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(9f, 0f);
                    labelRect.offsetMax = new Vector2(-9f, 0f);
                    _rowLabels[i] = AddText(label, string.Empty, 13,
                        FontStyle.Normal, new Color(0.9f, 0.92f, 0.96f, 1f));
                    _rowLabels[i].supportRichText = true;
                }

                _panelRect.SetAsLastSibling();
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException(
                    "Native console suggestion panel failed", ex);
                DestroyNativePanel();
            }
        }

        private void RefreshNativePanel()
        {
            if (_panelRoot == null)
                return;

            if (!HasCompleteNativePanel())
            {
                DestroyNativePanel();
                return;
            }

            bool visible = HasSuggestions;
            SetPanelActive(visible);
            if (!visible)
                return;

            EnsureSelectionVisible();
            int visibleCount = Mathf.Min(MaxSuggestions, _matches.Count);
            _panelRect.sizeDelta = new Vector2(
                -20f, RowsTop + visibleCount * RowHeight + 8f);
            PositionPanelUnderInput();
            _panelRect.SetAsLastSibling();

            CommandInfo selectedCommand = _matches[_selectedIndex];
            _structureLabel.text = BuildStructure(selectedCommand);
            _helperLabel.text = BuildHelper(selectedCommand);

            for (int i = 0; i < MaxSuggestions; i++)
            {
                int matchIndex = _windowStart + i;
                bool active = i < visibleCount && matchIndex < _matches.Count;
                _rowRects[i].gameObject.SetActive(active);
                if (!active)
                    continue;

                CommandInfo match = _matches[matchIndex];
                bool selected = matchIndex == _selectedIndex;
                string wordColor = selected ? "#DCEFC5" : "#E8EDF4";
                string sourceColor = match.IsVanilla ? "#8B93A0" : "#7DFFB2";
                _rowLabels[i].text = "<color=" + wordColor + ">" +
                    EscapeRichText(match.Word) + "</color> <color=" + sourceColor +
                    ">- " + EscapeRichText(match.Source) + "</color>";
                _rowBackgrounds[i].color = selected
                    ? new Color(0.09f, 0.14f, 0.1f, 0.92f)
                    : new Color(0.035f, 0.045f, 0.06f, 0.5f);
                _rowLabels[i].color = Color.white;
            }
        }

        private void PositionPanelUnderInput()
        {
            float topOffset = 48f;
            try
            {
                Component input = _inputField as Component;
                RectTransform inputRect = input?.transform as RectTransform;
                RectTransform canvasRect = _panelRect?.parent as RectTransform;
                if (inputRect != null && canvasRect != null)
                {
                    inputRect.GetWorldCorners(_inputCorners);
                    Camera camera = _canvas != null &&
                        _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                        ? _canvas.worldCamera
                        : null;
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                        camera, _inputCorners[0]);
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvasRect, screenPoint, camera, out Vector2 localPoint))
                        topOffset = canvasRect.rect.yMax - localPoint.y + 4f;
                }
            }
            catch { }

            _panelRect.anchoredPosition = new Vector2(0f, -Mathf.Max(0f, topOffset));
        }

        private void HandleMouseInput()
        {
            if (!HasSuggestions || !HasCompleteNativePanel())
                return;

            float wheel = Input.mouseScrollDelta.y;
            if (wheel > 0.01f)
                MoveSelection(-1);
            else if (wheel < -0.01f)
                MoveSelection(1);

            if (!Input.GetMouseButtonDown(0))
                return;

            Camera camera = null;
            try
            {
                if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    camera = _canvas.worldCamera;
            }
            catch { }

            int visibleCount = Mathf.Min(MaxSuggestions, _matches.Count);
            for (int i = 0; i < visibleCount; i++)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(
                        _rowRects[i], Input.mousePosition, camera))
                    continue;

                int matchIndex = _windowStart + i;
                _selectedIndex = matchIndex;
                Complete(_matches[matchIndex].Word);
                return;
            }
        }

        private bool HasCompleteNativePanel()
        {
            if (_panelRoot == null || _panelRect == null || _titleLabel == null ||
                _structureLabel == null || _helperLabel == null)
                return false;

            for (int i = 0; i < MaxSuggestions; i++)
            {
                if (_rowRects[i] == null || _rowBackgrounds[i] == null ||
                    _rowLabels[i] == null)
                    return false;
            }

            return true;
        }

        private void MoveSelection(int direction)
        {
            if (_matches.Count == 0)
                return;

            _selectedIndex = (_selectedIndex + direction + _matches.Count) %
                             _matches.Count;
            EnsureSelectionVisible();
            RefreshNativePanel();
        }

        private void EnsureSelectionVisible()
        {
            if (_matches.Count == 0)
            {
                _selectedIndex = 0;
                _windowStart = 0;
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _matches.Count - 1);
            if (_selectedIndex < _windowStart)
                _windowStart = _selectedIndex;
            else if (_selectedIndex >= _windowStart + MaxSuggestions)
                _windowStart = _selectedIndex - MaxSuggestions + 1;

            _windowStart = Mathf.Clamp(
                _windowStart, 0, Mathf.Max(0, _matches.Count - MaxSuggestions));
        }

        private void Complete(string word)
        {
            if (_inputField == null || _textProperty == null ||
                string.IsNullOrWhiteSpace(word))
                return;

            try
            {
                string current = GetText();
                int start = 0;
                while (start < current.Length && char.IsWhiteSpace(current[start]))
                    start++;
                bool slash = start < current.Length && current[start] == '/';
                int tokenStart = slash ? start + 1 : start;
                int split = current.IndexOf(' ', tokenStart);
                string prefix = current.Substring(0, tokenStart);
                string suffix = split >= 0 ? current.Substring(split) : " ";
                string completed = prefix + word + suffix;
                _textProperty.SetValue(_inputField, completed);
                _caretProperty?.SetValue(_inputField, completed.Length);
                _activateMethod?.Invoke(_inputField, null);
                _lastText = null;
                RefreshMatchesIfNeeded();
                RefreshNativePanel();
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException(
                    "Console completion failed", ex);
                ResetConsoleReference();
            }
        }

        private string GetText()
        {
            try { return _textProperty?.GetValue(_inputField) as string ?? string.Empty; }
            catch { return string.Empty; }
        }

        private void ClearSuggestions()
        {
            _matches.Clear();
            _lastText = null;
            _selectedIndex = 0;
            _windowStart = 0;
            SetPanelActive(false);
        }

        private void SetPanelActive(bool active)
        {
            try
            {
                if (_panelRoot != null && _panelRoot.activeSelf != active)
                    _panelRoot.SetActive(active);
            }
            catch { }
        }

        private void ResetConsoleReference()
        {
            DestroyNativePanel();
            _console = null;
            _inputField = null;
            _inputFieldField = null;
            _inputFieldProperty = null;
            _textProperty = null;
            _focusedProperty = null;
            _caretProperty = null;
            _activateMethod = null;
            _canvas = null;
            IsTyping = false;
            _lastText = null;
            _matches.Clear();
            _selectedIndex = 0;
            _windowStart = 0;
        }

        private void DestroyNativePanel()
        {
            try
            {
                if (_panelRoot != null)
                    UnityEngine.Object.Destroy(_panelRoot);
            }
            catch { }

            _panelRoot = null;
            _panelRect = null;
            _titleLabel = null;
            _structureLabel = null;
            _helperLabel = null;
            for (int i = 0; i < MaxSuggestions; i++)
            {
                _rowRects[i] = null;
                _rowBackgrounds[i] = null;
                _rowLabels[i] = null;
            }
        }

        private static GameObject CreateRectObject(string name, Transform parent,
            out RectTransform rect)
        {
            GameObject obj = new GameObject(name, Il2CppType.Of<RectTransform>());
            rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return obj;
        }

        private static void SetTopRow(RectTransform rect, float sideMargin,
            float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(-sideMargin * 2f, height);
        }

        private static Image AddImage(GameObject obj, Color color)
        {
            Image image = AddComponentSafe<Image>(obj);
            if (image != null)
            {
                image.color = color;
                image.raycastTarget = false;
            }
            return image;
        }

        private Text AddText(GameObject obj, string value, int size,
            FontStyle style, Color color)
        {
            Text label = AddComponentSafe<Text>(obj);
            if (label == null)
                return null;

            label.font = _font;
            label.text = value;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.supportRichText = true;
            return label;
        }

        private static T AddComponentSafe<T>(GameObject obj) where T : Component
        {
            Component component = obj.AddComponent(Il2CppType.Of<T>());
            try { return component?.TryCast<T>(); }
            catch { return component as T; }
        }

        private static bool GetBool(PropertyInfo property, object instance)
        {
            try { return property != null && property.GetValue(instance) is bool value && value; }
            catch { return false; }
        }

        private static Font GetBuiltinFont()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { }
            if (font != null)
                return font;

            try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { return null; }
        }

        private static string FirstToken(string input)
        {
            string value = (input ?? string.Empty).TrimStart();
            if (value.StartsWith("/", StringComparison.Ordinal))
                value = value.Substring(1);

            int split = value.IndexOf(' ');
            return (split >= 0 ? value.Substring(0, split) : value).Trim();
        }

        private static string BuildStructure(CommandInfo command)
        {
            string example = command.Example.Trim();
            if (example.Length == 0)
                return command.Word;
            return example.StartsWith(command.Word, StringComparison.OrdinalIgnoreCase)
                ? example
                : command.Word + " " + example;
        }

        private static string BuildHelper(CommandInfo command)
        {
            string description = command.Description.Trim();
            string source = "Source: " + command.Source;
            return description.Length == 0 ? source : description + "  -  " + source;
        }

        private static string ResolveSource(GameConsole.ConsoleCommand command)
        {
            try
            {
                Type type = command.GetType();
                string assemblyName = type.Assembly.GetName().Name ?? string.Empty;
                if (assemblyName.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) ||
                    (type.Namespace ?? string.Empty).StartsWith(
                        "Il2CppScheduleOne", StringComparison.Ordinal))
                    return "Vanilla";

                Version version = type.Assembly.GetName().Version;
                return version == null
                    ? assemblyName
                    : assemblyName + " v" + version.Major + "." + version.Minor +
                      "." + version.Build;
            }
            catch { return "Vanilla"; }
        }

        private static string EscapeRichText(string value)
        {
            return (value ?? string.Empty).Replace("&", "&amp;")
                .Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private readonly struct CommandInfo
        {
            public readonly string Word;
            public readonly string Description;
            public readonly string Example;
            public readonly string Source;
            public bool IsVanilla => Source.Equals(
                "Vanilla", StringComparison.OrdinalIgnoreCase);

            public CommandInfo(string word, string description, string example,
                string source)
            {
                Word = word ?? string.Empty;
                Description = description ?? string.Empty;
                Example = example ?? string.Empty;
                Source = string.IsNullOrWhiteSpace(source) ? "Vanilla" : source;
            }
        }
    }

    [HarmonyPatch(typeof(ConsoleUI), "Update")]
    internal static class ConsoleAutocompleteInputPatch
    {
        private static bool Prefix(ConsoleUI __instance)
        {
            return !ConsoleAutocompleteService.Instance.HandleNativeConsoleInput(__instance);
        }
    }
}
