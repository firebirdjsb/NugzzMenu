using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.UI;
using MelonLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using NugzzMenu.Services;
using NugzzMenu.UI;

namespace NugzzMenu
{
    public class Core : MelonMod
    {
        private const string Version = "0.9.9R4";
        private const int WindowId = 98765;
        private const float HeaderHeight = 56f;
        private const float TabStripHeight = 36f;
        private const float WindowBottomPadding = 16f;

        private enum MenuTab
        {
            Cheats,
            Money,
            Time,
            Vehicles,
            Properties,
            Items,
            Lobby,
            Performance,
            Relationships,
            Quests,
            Settings
        }

        private static readonly string[] TabLabels = { "CHEATS", "MONEY", "TIME", "VEHICLES", "PROPERTIES", "ITEMS", "LOBBY", "FPS", "RELATIONS", "QUESTS", "SETTINGS" };
        private static readonly string[] MoneyAmountLabels = { "$500", "$1K", "$5K", "$10K", "$50K", "$100K", "$500K", "$1M" };
        private static readonly float[] MoneyAmounts = { 500f, 1000f, 5000f, 10000f, 50000f, 100000f, 500000f, 1000000f };
        private static readonly int[] ExperienceAmounts = { 100, 500, 1000, 5000, 10000, 50000 };
        private static readonly string[] ExperienceAmountLabels = { "100", "500", "1K", "5K", "10K", "50K" };

        private int _moneyIndex = 3;
        private int _experienceIndex = 2;
        private MenuTab _selectedTab;
        private bool _itemCacheInitialized;
        private readonly CheatsState _cheatsState = new CheatsState();
        private readonly ItemsState _itemsState = new ItemsState();
        private readonly LobbyState _lobbyState = new LobbyState();
        private readonly SettingsState _settingsState = new SettingsState();
        private readonly PropertiesState _propertiesState = new PropertiesState();

        private KeyCode _menuKey = KeyCode.F8;
        private MelonPreferences_Category _preferences;
        private MelonPreferences_Entry<string> _menuKeyPreference;
        private MelonPreferences_Entry<bool> _verboseDebugPreference;
        private MelonPreferences_Entry<bool> _keybindOverlayPreference;
        private MelonPreferences_Entry<bool> _doubleSpaceFlyPreference;
        private HarmonyLib.Harmony _harmony;
        private Delegate _s1LoadCompleteHandler;
        private Delegate _s1PreSceneChangeHandler;
        private Delegate _s1LocalPlayerSpawnedHandler;
        private Delegate _s1PlayerSpawnedHandler;

        private Rect _windowRect = new Rect(40f, 40f, 820f, 690f);
        private float _measuredContentHeight = 620f;
        private readonly float[] _tabContentHeights = new float[TabLabels.Length];
        private readonly Vector2[] _tabScrollPositions = new Vector2[TabLabels.Length];
        private bool _isMenuOpen;
        private bool _authorityWasAllowed = true;
        private bool _isWindowDragging;
        private Vector2 _windowDragOffset;
        private long _nextGuiExceptionLogAtMs;
        private bool _keybindOverlayRuntimeSupported = true;
        private bool _skinApplicationSupported = true;

        public override void OnInitializeMelon()
        {
            GameplayStateGateService.Instance.SetMenuOpen(false);
            SessionAuthorityService.Instance.Initialize();
            _preferences = MelonPreferences.CreateCategory("Nugzz", "Nugzz Settings");
            _menuKeyPreference = _preferences.CreateEntry<string>("MenuKeybind", "F8", "Menu Toggle Key", "Key to open/close the Nugzz menu", false, false, null, null);
            _verboseDebugPreference = _preferences.CreateEntry<bool>("VerboseDebugLogging", false, "Verbose Debug Logging", "Write extra Nugzz diagnostic logs", false, false, null, null);
            _keybindOverlayPreference = _preferences.CreateEntry<bool>("KeybindOverlay", true, "Keybind HUD", "Show compact in-game controls at the top of the screen", false, false, null, null);
            _doubleSpaceFlyPreference = _preferences.CreateEntry<bool>("DoubleSpaceFlyHotkey", true, "Double Space Fly Hotkey", "Double-tap Space to toggle fly mode", false, false, null, null);
            DebugLogService.Instance.SetVerbose(_verboseDebugPreference.Value);
            KeybindOverlayService.Instance.SetEnabled(_keybindOverlayPreference.Value);
            KeybindOverlayService.Instance.SetMenuKey(_menuKeyPreference.Value);
            FlyingService.Instance.SetDoubleSpaceHotkeyEnabled(_doubleSpaceFlyPreference.Value);
            FlyingService.Instance.SetVehicleFlyEnabled(false);

            if (!Enum.TryParse(_menuKeyPreference.Value, true, out _menuKey))
                _menuKey = KeyCode.F8;

            LoggerInstance.Msg($"Nugzz v{Version} by XUnfairX | {_menuKeyPreference.Value} to open");

            GUISystemService.Instance.Initialize();
            SubscribeS1ApiEvents();
            try
            {
                // MelonLoader installs attribute-based Harmony patches before initialization.
                // This instance is only for compatibility patches resolved at runtime.
                _harmony = new HarmonyLib.Harmony("com.xunfairx.nugzzmenu.runtime");
                CompatibilityService.Instance.ApplyRuntimeCompatibilityFixes(_harmony);
                LoggerInstance.Msg("[Nugzz] Runtime compatibility patches applied successfully");
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Warning("[Nugzz] Harmony patch install failed: " + ex);
            }
            LoggerInstance.Msg("[Nugzz] All services initialized successfully");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            DebugTestRoomService.Instance.ResetForScene();
            ShapePrefabService.Instance.ResetForScene();
            SkateboardTuneService.Instance.ResetForScene();
            ConsoleAutocompleteService.Instance.ResetForScene();
            SaveManagementService.Instance.SetCurrentScene(sceneName);
            ManagerCacheService.Instance.Invalidate();
            TeleportService.Instance.MarkCatalogDirty();

            if (sceneName == "Main")
            {
                InitializeGameplayServices();
            }
        }

        public override void OnDeinitializeMelon()
        {
            GameplayStateGateService.Instance.SetMenuOpen(false);
            UnsubscribeS1ApiEvents();
            DebugTestRoomService.Instance.ClearRoom();
            VehicleCollisionService.Instance.Reset();
            VehicleMenuCameraService.Instance.Reset();
            SkateboardTuneService.Instance.ResetAll();
            ConsoleAutocompleteService.Instance.ResetForScene();
            GUIFit.ClearCache();
            TMPHybridService.Instance.Reset();
        }

        private void HandleLoadComplete()
        {
            InitializeGameplayServices();
        }

        private void HandlePreSceneChange()
        {
            _itemCacheInitialized = false;
            DebugTestRoomService.Instance.ResetForScene();
            ShapePrefabService.Instance.ResetForScene();
            SkateboardTuneService.Instance.ResetForScene();
            ConsoleAutocompleteService.Instance.ResetForScene();
            ManagerCacheService.Instance.Invalidate();
            TeleportService.Instance.MarkCatalogDirty();
            VehicleCollisionService.Instance.Reset();
        }

        private void HandleApiPlayerSpawned(object player)
        {
            ManagerCacheService.Instance.Invalidate();
            TeleportService.Instance.MarkCatalogDirty();
            VehicleCollisionService.Instance.RefreshAll();
        }

        private void InitializeGameplayServices()
        {
            ManagerCacheService.Instance.Invalidate();
            ItemService.Instance.InitializeCache();
            VehicleService.Instance.InitializeCache();
            VehicleCollisionService.Instance.Initialize();
            _itemCacheInitialized = ItemService.Instance.IsCached;
        }

        public override void OnUpdate()
        {
            PerformanceService.Instance.BeginNugzzUpdate();
            SessionAuthorityService.Instance.Update();
            bool featuresAllowed = SessionAuthorityService.Instance.FeaturesAllowed;
            if (_authorityWasAllowed && !featuresAllowed)
                ResetAllRuntimeChanges(false);
            _authorityWasAllowed = featuresAllowed;

            if (featuresAllowed)
                ConsoleAutocompleteService.Instance.Update();

            bool menuKeyPressed = Input.GetKeyDown(_menuKey);
            if (menuKeyPressed && !_isMenuOpen)
                RefreshSaveToolSceneState();

            bool mainMenuSaveMode = SaveManagementService.Instance.IsMainMenu &&
                !GameplayStateGateService.IsCharacterCreatorOpen();
            bool nativeUiBlocked = GameplayStateGateService.Instance.IsModControlBlocked(
                out string blockedReason);
            if (nativeUiBlocked && !mainMenuSaveMode)
            {
                if (_isMenuOpen)
                    SetMenuOpen(false);
                if (CameraService.Instance.ThirdPersonEnabled)
                    CameraService.Instance.ToggleThirdPerson(false, false);
            }

            bool hotkeysBlocked = GameplayStateGateService.Instance.AreFeatureHotkeysBlocked();
            if (menuKeyPressed)
            {
                if (_isMenuOpen)
                {
                    if (!GUIFit.IsTextFieldActive)
                        SetMenuOpen(false);
                }
                else if (mainMenuSaveMode || (!nativeUiBlocked && !hotkeysBlocked))
                {
                    ToggleMenu();
                }
            }

            if (Input.GetKeyDown(KeyCode.G) && !_isMenuOpen)
            {
                if (!featuresAllowed)
                    Status(SessionAuthorityService.Instance.BlockReason);
                else if (nativeUiBlocked)
                    Status("3rd person unavailable: " + blockedReason);
                else if (hotkeysBlocked)
                    Status("3rd person unavailable while another interface owns input");
                else
                    ToggleCamera(!CameraService.Instance.ThirdPersonEnabled);
            }

            NotificationService.Instance.Update();
            if (!featuresAllowed)
            {
                PerformanceService.Instance.EndNugzzUpdate();
                return;
            }

            PlayerCheatService.Instance.Update();
            EffectsService.Instance.Update();
            CameraService.Instance.MaintainThirdPersonState(_isMenuOpen);
            ItemService.Instance.ProcessPendingSpawns();
            VehicleService.Instance.Update();
            VehicleCollisionService.Instance.Update();
            VehicleMenuCameraService.Instance.Update(_isMenuOpen);
            VehicleHudLifecycle.Update();
            SkateboardTuneService.Instance.Update();
            PerformanceService.Instance.Update();
            FlyingService.Instance.UpdateHotkeys(hotkeysBlocked);
            ShapePrefabService.Instance.Update(hotkeysBlocked);
            // The registry can become available a few frames after scene initialization.
            if (!_itemCacheInitialized && ItemService.Instance.ItemCount == 0)
            {
                ItemService.Instance.InitializeCache();
                _itemCacheInitialized = ItemService.Instance.IsCached;
            }

            if (FlyingService.Instance.Enabled && !hotkeysBlocked)
                FlyingService.Instance.ApplyFlyMovement();

            PerformanceService.Instance.EndNugzzUpdate();
        }
        public override void OnLateUpdate()
        {
            if (!SessionAuthorityService.Instance.FeaturesAllowed)
                return;
            if (CameraService.Instance.ThirdPersonEnabled)
                CameraService.Instance.ApplyThirdPersonCameraLate();
            VehicleMenuCameraService.Instance.LateUpdate(_isMenuOpen);
            if (FlyingService.Instance.Enabled)
                FlyingService.Instance.ApplyPostMovementLock();
        }

        public override void OnFixedUpdate()
        {
            if (!SessionAuthorityService.Instance.FeaturesAllowed)
                return;
            VehicleCollisionService.Instance.FixedUpdate();
        }

        public override void OnGUI()
        {
            DrawKeybindOverlay();

            try
            {
                OnGUIInternal();
            }
            catch (System.NotSupportedException ex)
            {
                if (ShouldLogGuiException())
                    LoggerInstance.Warning("[Nugzz] OnGUI skipped due to stripped method: " + ex.Message);
            }
            catch (Exception ex)
            {
                if (ShouldLogGuiException())
                    LoggerInstance.Warning("[Nugzz] OnGUI failed: " + ex);
            }
        }

        private void OnGUIInternal()
        {
            var gui = GUISystemService.Instance;
            var notifications = NotificationService.Instance;
            var text = TMPHybridService.Instance;

            if (_isMenuOpen || notifications.HasNotification)
                TryApplyFontToSkin(gui);

            if (notifications.HasNotification)
            {
                const float notificationWidth = 420f;
                float notificationX = (Screen.width - notificationWidth) / 2f;
                GUIFit.Panel(new Rect(notificationX, 10f, notificationWidth, 34f), gui.NotificationStyle);
                GUIFit.Texture(new Rect(notificationX, 10f, 4f, 34f), gui.AccentTexture);
                text.Label(
                    notificationX + 8f, 10f, notificationWidth - 16f, 34f,
                    notifications.NotificationMessage ?? string.Empty,
                    gui.GetColorForCategory(LabelCategory.Notif),
                    gui.GetFontSizeForCategory(LabelCategory.Notif),
                    gui.GetAlignmentForCategory(LabelCategory.Notif),
                    gui.GetStyleForCategory(LabelCategory.Notif));
            }

            if (!_isMenuOpen)
                return;

            RefreshSaveToolSceneState();
            ApplyDynamicWindowSize();
            ClampWindowToScreen();

            if (gui.ShadowTexture != null)
                GUIFit.Texture(new Rect(_windowRect.x + 8f, _windowRect.y + 10f, _windowRect.width, _windowRect.height), gui.ShadowTexture);
            if (gui.BorderTexture != null)
            {
                GUIFit.Texture(new Rect(_windowRect.x - 1f, _windowRect.y - 1f, _windowRect.width + 2f, 1f), gui.BorderTexture);
                GUIFit.Texture(new Rect(_windowRect.x - 1f, _windowRect.y + _windowRect.height, _windowRect.width + 2f, 1f), gui.BorderTexture);
                GUIFit.Texture(new Rect(_windowRect.x - 1f, _windowRect.y, 1f, _windowRect.height), gui.BorderTexture);
                GUIFit.Texture(new Rect(_windowRect.x + _windowRect.width, _windowRect.y, 1f, _windowRect.height), gui.BorderTexture);
            }

            try
            {
                DrawMenuWindow(gui);
            }
            catch (Exception ex)
            {
                if (ShouldLogGuiException())
                    LoggerInstance.Warning("[Nugzz] Menu shell draw failed: " + ex);
            }
        }

        private void DrawKeybindOverlay()
        {
            if (!_keybindOverlayRuntimeSupported || !SessionAuthorityService.Instance.FeaturesAllowed)
                return;

            try
            {
                KeybindOverlayService.Instance.Draw(_isMenuOpen);
            }
            catch (System.NotSupportedException ex)
            {
                _keybindOverlayRuntimeSupported = false;
                LoggerInstance.Warning("[Nugzz] Optional keybind overlay disabled after game update: " + ex.Message);
            }
            catch (Exception ex)
            {
                _keybindOverlayRuntimeSupported = false;
                LoggerInstance.Warning("[Nugzz] Optional keybind overlay disabled: " + ex.Message);
            }
        }

        private void TryApplyFontToSkin(GUISystemService gui)
        {
            if (!_skinApplicationSupported)
                return;

            try
            {
                gui.ApplyFontToSkin();
            }
            catch (System.NotSupportedException ex)
            {
                _skinApplicationSupported = false;
                LoggerInstance.Warning("[Nugzz] Optional GUI skin disabled after game update: " + ex.Message);
            }
            catch (Exception ex)
            {
                _skinApplicationSupported = false;
                LoggerInstance.Warning("[Nugzz] Optional GUI skin disabled: " + ex.Message);
            }
        }

        private bool ShouldLogGuiException()
        {
            long now = System.Environment.TickCount64;
            if (now < _nextGuiExceptionLogAtMs)
                return false;

            _nextGuiExceptionLogAtMs = now + 2000L;
            return true;
        }

        private void DrawMenuWindow(GUISystemService gui)
        {
            HandleWindowDrag();
            GUIFit.Panel(_windowRect, gui.WindowStyle);
            GUI.BeginGroup(_windowRect);
            try
            {
                DrawWindow(WindowId);
            }
            finally
            {
                GUI.EndGroup();
            }
        }

        private void HandleWindowDrag()
        {
            Event current = Event.current;
            if (current == null)
                return;

            Vector2 mouse = current.mousePosition;
            Rect titleBar = new Rect(_windowRect.x, _windowRect.y, _windowRect.width, 26f);
            if (current.type == EventType.MouseDown && current.button == 0 && titleBar.Contains(mouse))
            {
                _isWindowDragging = true;
                _windowDragOffset = mouse - new Vector2(_windowRect.x, _windowRect.y);
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDrag && current.button == 0 && _isWindowDragging)
            {
                _windowRect.x = mouse.x - _windowDragOffset.x;
                _windowRect.y = mouse.y - _windowDragOffset.y;
                ClampWindowToScreen();
                current.Use();
                return;
            }

            if (current.type == EventType.MouseUp && current.button == 0 && _isWindowDragging)
            {
                _isWindowDragging = false;
                current.Use();
            }
        }

        private void ClampWindowToScreen()
        {
            float maxW = Mathf.Max(320f, Screen.width - 20f);
            float maxH = Mathf.Max(180f, Screen.height - 20f);
            float minW = Mathf.Min(620f, maxW);
            float minH = Mathf.Min(180f, maxH);
            _windowRect.width = Mathf.Clamp(_windowRect.width, minW, maxW);
            _windowRect.height = Mathf.Clamp(_windowRect.height, minH, maxH);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, Mathf.Max(0f, Screen.width - _windowRect.width));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, Mathf.Max(0f, Screen.height - _windowRect.height));
        }

        private void ApplyDynamicWindowSize()
        {
            float targetWidth;
            switch (_selectedTab)
            {
                case MenuTab.Money:
                    targetWidth = 620f;
                    break;
                case MenuTab.Time:
                    targetWidth = 680f;
                    break;
                case MenuTab.Cheats:
                    targetWidth = 700f;
                    break;
                case MenuTab.Lobby:
                    targetWidth = 720f;
                    break;
                case MenuTab.Vehicles:
                case MenuTab.Properties:
                case MenuTab.Performance:
                case MenuTab.Relationships:
                case MenuTab.Quests:
                case MenuTab.Settings:
                    targetWidth = 860f;
                    break;
                case MenuTab.Items:
                    targetWidth = 860f;
                    break;
                default:
                    targetWidth = 700f;
                    break;
            }

            _windowRect.width = targetWidth;
            _windowRect.height = GetCurrentTabContentHeight() + HeaderHeight + TabStripHeight + WindowBottomPadding;
        }

        private float GetCurrentTabContentHeight()
        {
            int tabIndex = (int)_selectedTab;
            if (tabIndex >= 0 && tabIndex < _tabContentHeights.Length &&
                _tabContentHeights[tabIndex] > 0f)
            {
                return _tabContentHeights[tabIndex];
            }

            if (_selectedTab == MenuTab.Settings)
                return Mathf.Max(_measuredContentHeight, 760f);

            return _measuredContentHeight;
        }

        private void DrawWindow(int id)
        {
            var gui = GUISystemService.Instance;
            var tmp = TMPHybridService.Instance;

            float contentW = _windowRect.width - 20f;
            float y = 2f;

            GUIFit.Texture(new Rect(-10f, -10f, _windowRect.width + 20f, 58f), gui.TitleTexture);
            GUIFit.Texture(new Rect(0f, 46f, _windowRect.width, 2f), gui.AccentTexture);
            GUIFit.Texture(new Rect(0f, 48f, _windowRect.width, 1f), gui.AccentSoftTexture);
            GUIFit.Texture(new Rect(0f, 0f, 4f, _windowRect.height), gui.AccentSoftTexture);

            tmp.Label(12f, 0f, 220f, 32f, "NugzzMenu",
                gui.GetColorForCategory(LabelCategory.Title),
                gui.GetFontSizeForCategory(LabelCategory.Title),
                gui.GetAlignmentForCategory(LabelCategory.Title),
                gui.GetStyleForCategory(LabelCategory.Title));

            tmp.Label(14f, 30f, 300f, 14f, "Schedule I control suite",
                gui.GetColorForCategory(LabelCategory.Label),
                10f,
                TextAnchor.MiddleLeft,
                FontStyle.Normal);

            string hostText = GetHostLabel();
            string rightText = $"v{Version}  |  {_menuKeyPreference.Value}";
            GUIFit.Panel(new Rect(contentW - 224f, 10f, 220f, 24f), gui.BoxStyle);
            tmp.Label(contentW - 216f, 13f, 76f, 18f, hostText,
                GetHostLabelColor(),
                10f,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);
            tmp.Label(contentW - 138f, 13f, 126f, 18f, rightText,
                gui.GetColorForCategory(LabelCategory.Subtitle),
                gui.GetFontSizeForCategory(LabelCategory.Subtitle),
                gui.GetAlignmentForCategory(LabelCategory.Subtitle),
                gui.GetStyleForCategory(LabelCategory.Subtitle));

            if (NotificationService.Instance.HasStatus)
            {
                string status = NotificationService.Instance.StatusMessage ?? string.Empty;
                float chipW = Mathf.Min(260f, Mathf.Max(110f, status.Length * 7f + 24f));
                const float statusRightInset = 240f;
                GUIFit.Panel(new Rect(contentW - statusRightInset - chipW, 10f, chipW, 24f), gui.NotificationStyle);
                tmp.Label(contentW - statusRightInset + 8f - chipW, 12f, chipW - 16f, 20f, status,
                    gui.GetColorForCategory(LabelCategory.Status),
                    gui.GetFontSizeForCategory(LabelCategory.Status),
                    TextAnchor.MiddleCenter,
                    gui.GetStyleForCategory(LabelCategory.Status));
            }

            y = HeaderHeight;

            if (!SessionAuthorityService.Instance.FeaturesAllowed)
            {
                DrawAuthorityBlocked(contentW, ref y);
                return;
            }

            DrawTabs(ref y, contentW);

            try
            {
                float drawW = Mathf.Min(contentW, 840f);
                float drawX = Mathf.Max(10f, (contentW - drawW) * 0.5f + 6f);
                float availableH = Mathf.Max(0f, _windowRect.height - y - 8f);
                int tabIndex = (int)_selectedTab;
                float measuredHeight = GetCurrentTabContentHeight();
                bool needsScroll = measuredHeight > availableH + 1f;
                float viewW = needsScroll ? Mathf.Max(240f, drawW - 18f) : drawW;
                float viewH = Mathf.Max(availableH, measuredHeight + 12f);
                float localY = 0f;
                GUIFit.Texture(new Rect(drawX - 8f, y - 4f, drawW + 16f, Mathf.Max(80f, availableH + 2f)), gui.DarkTexture);
                GUIFit.Texture(new Rect(drawX - 8f, y - 4f, 3f, Mathf.Max(80f, availableH + 2f)), gui.AccentSoftTexture);
                if (needsScroll)
                {
                    TMPHybridService.Instance.Label(drawX + drawW - 84f, y - 18f, 82f, 14f, "Scroll for more",
                        gui.GetColorForCategory(LabelCategory.Subtitle),
                        gui.GetFontSizeForCategory(LabelCategory.Subtitle),
                        TextAnchor.MiddleRight,
                        gui.GetStyleForCategory(LabelCategory.Subtitle));
                }

                Rect viewport = new Rect(drawX, y, drawW, availableH);
                float maxScrollY = Mathf.Max(0f, measuredHeight - availableH + 12f);
                if (needsScroll)
                {
                    Vector2 scroll = _tabScrollPositions[tabIndex];
                    if (GUIFit.Button(new Rect(drawX + drawW - 72f, y - 20f, 32f, 16f), "Up", gui.ButtonStyle))
                        scroll.y -= 80f;
                    if (GUIFit.Button(new Rect(drawX + drawW - 36f, y - 20f, 34f, 16f), "Down", gui.ButtonStyle))
                        scroll.y += 80f;

                    _tabScrollPositions[tabIndex] = new Vector2(
                        Mathf.Clamp(scroll.x, 0f, 0f),
                        Mathf.Clamp(scroll.y, 0f, maxScrollY));
                }
                else
                {
                    _tabScrollPositions[tabIndex] = Vector2.zero;
                }

                GUI.BeginGroup(viewport);
                GUI.BeginGroup(new Rect(0f, -_tabScrollPositions[tabIndex].y, viewW, viewH));
                try
                {
                    switch (_selectedTab)
                    {
                        case MenuTab.Cheats:
                            DrawCheatsTab(ref localY, viewW);
                            break;
                        case MenuTab.Money:
                            DrawMoneyTab(ref localY, viewW);
                            break;
                        case MenuTab.Time:
                            DrawTimeTab(ref localY, viewW);
                            break;
                        case MenuTab.Vehicles:
                            DrawVehiclesTab(ref localY, viewW);
                            break;
                        case MenuTab.Properties:
                            DrawPropertiesTab(ref localY, viewW);
                            break;
                        case MenuTab.Items:
                            DrawItemsTab(ref localY, viewW);
                            break;
                        case MenuTab.Lobby:
                            DrawLobbyTab(ref localY, viewW);
                            break;
                        case MenuTab.Performance:
                            DrawPerformanceTab(ref localY, viewW);
                            break;
                        case MenuTab.Relationships:
                            DrawRelationshipsTab(ref localY, viewW);
                            break;
                        case MenuTab.Quests:
                            DrawQuestsTab(ref localY, viewW);
                            break;
                        case MenuTab.Settings:
                            DrawSettingsTab(ref localY, viewW);
                            break;
                    }
                }
                finally
                {
                    GUI.EndGroup();
                    GUI.EndGroup();
                }

                DrawManualScrollbar(viewport, measuredHeight, availableH, _tabScrollPositions[tabIndex].y);
                y += localY;
                _measuredContentHeight = Mathf.Max(100f, localY);
                if (tabIndex >= 0 && tabIndex < _tabContentHeights.Length)
                {
                    _tabContentHeights[tabIndex] = _measuredContentHeight;
                    float updatedMaxScrollY = Mathf.Max(0f, _measuredContentHeight - availableH + 12f);
                    if (_tabScrollPositions[tabIndex].y > updatedMaxScrollY)
                    {
                        _tabScrollPositions[tabIndex] = new Vector2(
                            _tabScrollPositions[tabIndex].x,
                            updatedMaxScrollY);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ShouldLogGuiException())
                    LoggerInstance.Warning("[Nugzz] GUI draw failed on " + _selectedTab + ": " + ex);

                tmp.Label(4f, y, contentW, 20f, "Error: " + ex.Message,
                    gui.GetColorForCategory(LabelCategory.Error),
                    gui.GetFontSizeForCategory(LabelCategory.Error),
                    gui.GetAlignmentForCategory(LabelCategory.Error),
                    gui.GetStyleForCategory(LabelCategory.Error));
            }
        }

        private static void DrawManualScrollbar(Rect viewport, float contentHeight, float visibleHeight, float scrollY)
        {
            if (contentHeight <= visibleHeight + 1f || visibleHeight <= 0f)
                return;

            var gui = GUISystemService.Instance;
            float maxScrollY = Mathf.Max(1f, contentHeight - visibleHeight + 12f);
            float trackH = Mathf.Max(40f, visibleHeight);
            float thumbH = Mathf.Clamp((visibleHeight / Mathf.Max(contentHeight, 1f)) * trackH, 28f, trackH);
            float thumbY = viewport.y + Mathf.Clamp(scrollY / maxScrollY, 0f, 1f) * (trackH - thumbH);
            Rect track = new Rect(viewport.xMax - 8f, viewport.y, 5f, trackH);
            Rect thumb = new Rect(viewport.xMax - 8f, thumbY, 5f, thumbH);

            if (gui.AccentSoftTexture != null)
                GUIFit.Texture(track, gui.AccentSoftTexture);
            if (gui.AccentTexture != null)
                GUIFit.Texture(thumb, gui.AccentTexture);
        }

        private string GetHostLabel()
        {
            if (!SessionAuthorityService.Instance.FeaturesAllowed)
                return SessionAuthorityService.Instance.IsRpModBlocked ? "RP LOCKED" : "HOST REQUIRED";

            try
            {
                bool inLobby = LobbyService.Instance.IsInLobby();
                bool isHost = LobbyService.Instance.IsHost();
                return !inLobby ? "SOLO/HOST" : isHost ? "HOST" : "NON-HOST";
            }
            catch
            {
                return "SOLO/HOST";
            }
        }

        private Color GetHostLabelColor()
        {
            if (!SessionAuthorityService.Instance.FeaturesAllowed)
                return new Color(1f, 0.22f, 0.18f);

            try
            {
                bool inLobby = LobbyService.Instance.IsInLobby();
                bool isHost = LobbyService.Instance.IsHost();
                return !inLobby || isHost
                    ? new Color(0.55f, 1f, 0.25f)
                    : new Color(1f, 0.22f, 0.18f);
            }
            catch
            {
                return new Color(0.55f, 1f, 0.25f);
            }
        }

        private void DrawAuthorityBlocked(float w, ref float y)
        {
            var gui = GUISystemService.Instance;
            var tmp = TMPHybridService.Instance;
            float panelW = Mathf.Min(w - 20f, 760f);
            float x = Mathf.Max(10f, (w - panelW) * 0.5f);

            GUIFit.Panel(new Rect(x, y + 8f, panelW, 190f), gui.BoxStyle);
            tmp.Label(x + 16f, y + 22f, panelW - 32f, 28f,
                "NUGZZ FEATURES DISABLED",
                gui.GetColorForCategory(LabelCategory.Error), 18f,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            tmp.Label(x + 22f, y + 58f, panelW - 44f, 76f,
                SessionAuthorityService.Instance.BlockReason,
                gui.GetColorForCategory(LabelCategory.Label), 13f,
                TextAnchor.UpperCenter, FontStyle.Normal, true);
            tmp.Label(x + 22f, y + 140f, panelW - 44f, 36f,
                SessionAuthorityService.Instance.IsRpModBlocked
                    ? "Remove S.I.A.K - Imperium and restart the game to use Nugzz."
                    : "Join a host running the same NugzzMenu DLL. Access enables automatically after the host is detected.",
                gui.GetColorForCategory(LabelCategory.Status), 12f,
                TextAnchor.MiddleCenter, FontStyle.Italic, true);
            y += 210f;
            _measuredContentHeight = y;
        }

        private void DrawTabs(ref float y, float w)
        {
            float tabWidth = (w - 12f) / TabLabels.Length;
            for (int i = 0; i < TabLabels.Length; i++)
            {
                bool selected = i == (int)_selectedTab;
                Rect tabRect = new Rect(8f + i * tabWidth, y, tabWidth - 4f, 28f);
                if (GUIFit.Button(tabRect, TabLabels[i],
                        selected ? GUISystemService.Instance.TabActiveStyle : GUISystemService.Instance.TabStyle))
                {
                    _selectedTab = (MenuTab)i;
                }

                if (selected)
                {
                    GUIFit.Texture(new Rect(tabRect.x + 8f, tabRect.yMax - 3f, tabRect.width - 16f, 2f),
                        GUISystemService.Instance.AccentTexture);
                }
            }
            y += TabStripHeight;
        }

        private void ToggleMenu()
        {
            if (!_isMenuOpen &&
                GameplayStateGateService.Instance.IsModControlBlocked(out string reason) &&
                !(SaveManagementService.Instance.IsMainMenu &&
                  !GameplayStateGateService.IsCharacterCreatorOpen()))
            {
                Status("Menu unavailable: " + reason);
                return;
            }

            SetMenuOpen(!_isMenuOpen);
        }

        private void SetMenuOpen(bool open)
        {
            if (_isMenuOpen == open)
                return;

            bool wasOpen = _isMenuOpen;
            _isMenuOpen = open;
            GameplayStateGateService.Instance.SetMenuOpen(open);
            VehicleMenuCameraService.Instance.NotifyMenuStateChanged(open, wasOpen);
            ApplyMenuInputState();
        }

        private void ApplyMenuInputState()
        {
            try
            {
                bool keepNativeCursor = !_isMenuOpen && ShouldKeepNativeCursor();
                bool gameplayFocus = !_isMenuOpen && !keepNativeCursor;
                Cursor.visible = _isMenuOpen || keepNativeCursor;
                Cursor.lockState = (_isMenuOpen || keepNativeCursor)
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;

                if (gameplayFocus)
                {
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                }

                var camera = PlayerCamera.Instance;
                if (IsUsingNativeRideCamera(camera))
                    return;

                camera?.SetCanLook(
                    !_isMenuOpen &&
                    !keepNativeCursor &&
                    !CameraService.Instance.ThirdPersonEnabled);
            }
            catch { }
        }

        private static bool IsUsingNativeRideCamera(PlayerCamera camera)
        {
            try
            {
                if (camera != null &&
                    (camera.CameraMode == PlayerCamera.ECameraMode.Vehicle ||
                     camera.CameraMode == PlayerCamera.ECameraMode.Skateboard))
                    return true;

                var player = ManagerCacheService.Instance.LocalPlayer;
                return player != null &&
                       (player.IsInVehicle || player.CurrentVehicleSeat != null ||
                        player.IsSkating || player.ActiveSkateboard != null);
            }
            catch
            {
                return false;
            }
        }

        private static bool ShouldKeepNativeCursor()
        {
            try
            {
                if (IsPauseMenuOpen())
                    return true;

                if (GameplayStateGateService.IsCharacterCreatorOpen())
                    return true;

                Scene scene = SceneManager.GetActiveScene();
                return scene.IsValid() &&
                    string.Equals(scene.name, "Menu", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void RefreshSaveToolSceneState()
        {
            try
            {
                Scene scene = SceneManager.GetActiveScene();
                bool probablyMainMenu = false;
                try
                {
                    probablyMainMenu = ManagerCacheService.Instance.LocalPlayer == null;
                }
                catch { }

                if (scene.IsValid())
                    SaveManagementService.Instance.SetCurrentScene(scene.name, probablyMainMenu);
            }
            catch { }
        }

        private static bool IsPauseMenuOpen()
        {
            return GameplayStateGateService.IsPaused();
        }

        private void DrawCheatsTab(ref float y, float w)
        {
            var state = _cheatsState;
            state.GodMode = PlayerCheatService.Instance.GodMode;
            state.InfiniteStamina = PlayerCheatService.Instance.InfiniteStamina;
            state.SpeedBoost = PlayerCheatService.Instance.SpeedBoost;
            state.SpeedMultiplier = PlayerCheatService.Instance.SpeedMultiplier;
            state.PlayerScale = PlayerCheatService.Instance.PlayerScale;
            state.JumpMultiplier = PlayerCheatService.Instance.JumpMultiplier;
            state.GravityMultiplier = PlayerCheatService.Instance.GravityMultiplier;
            state.InfiniteAmmo = PlayerCheatService.Instance.InfiniteAmmo;
            state.NeverWanted = PlayerCheatService.Instance.NeverWanted;
            state.BottomlessTrashGrabber = PlayerCheatService.Instance.BottomlessTrashGrabber;
            state.FlyEnabled = FlyingService.Instance.Enabled;
            state.FlySpeed = FlyingService.Instance.Speed;
            state.DoubleSpaceFlyHotkey = FlyingService.Instance.DoubleSpaceHotkeyEnabled;
            state.ThirdPerson = CameraService.Instance.ThirdPersonEnabled;
            state.CameraDistance = CameraService.Instance.Distance;
            state.CameraHeight = CameraService.Instance.Height;
            state.CameraShoulder = CameraService.Instance.ShoulderOffset;

            CheatsTabRenderer.Draw(ref y, w, GUISystemService.Instance.OnStyle,
                GUISystemService.Instance.OffStyle, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, state,
                 TeleportAction, Heal, ClearWanted, SetSpeedMultiplier, SetPlayerScale,
                 SetJumpMultiplier, SetGravityMultiplier,
                 ToggleFly, SetFlySpeed, SetDoubleSpaceFlyHotkey, ToggleCamera,
                 CameraService.Instance.SetDistance, CameraService.Instance.SetHeight, CameraService.Instance.SetShoulderOffset,
                 SavePosition, LoadPosition);

            PlayerCheatService.Instance.GodMode = state.GodMode;
            PlayerCheatService.Instance.InfiniteStamina = state.InfiniteStamina;
            PlayerCheatService.Instance.SpeedBoost = state.SpeedBoost;
            PlayerCheatService.Instance.SpeedMultiplier = state.SpeedMultiplier;
            PlayerCheatService.Instance.PlayerScale = state.PlayerScale;
            PlayerCheatService.Instance.JumpMultiplier = state.JumpMultiplier;
            PlayerCheatService.Instance.GravityMultiplier = state.GravityMultiplier;
            PlayerCheatService.Instance.InfiniteAmmo = state.InfiniteAmmo;
            PlayerCheatService.Instance.NeverWanted = state.NeverWanted;
            PlayerCheatService.Instance.BottomlessTrashGrabber = state.BottomlessTrashGrabber;
        }

        private void DrawMoneyTab(ref float y, float w)
        {
            MoneyTabRenderer.Draw(ref y, w, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, MoneyAmountLabels, ExperienceAmountLabels, _moneyIndex, _experienceIndex,
                i => _moneyIndex = i, i => _experienceIndex = i,
                AddCash, AddOnlineBalance, AddXP);
        }

        private void AddCash() { try { EconomyService.Instance.AdjustCash(MoneyAmounts[_moneyIndex], true); Status($"+${MoneyAmounts[_moneyIndex]:N0} cash"); } catch { } }

        private void AddOnlineBalance() { try { EconomyService.Instance.AdjustOnlineBalance(MoneyAmounts[_moneyIndex]); Status($"+${MoneyAmounts[_moneyIndex]:N0} online"); } catch { } }

        private void AddXP() { try { GameManagerService.Instance.AddXP(ExperienceAmounts[_experienceIndex]); Status($"+{ExperienceAmounts[_experienceIndex]} XP"); } catch { } }

        private void DrawTimeTab(ref float y, float w)
        {
            TimeTabRenderer.Draw(ref y, w, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle,
                TimeManagerService.Instance.SetTimeSpeed,
                i => TimeManagerService.Instance.SetTimeOfDay(i),
                () => WorldObjectService.Instance.GrowAllPlants(),
                () => WorldObjectService.Instance.WaterAllPlants(),
                () => WorldObjectService.Instance.FillAllPotsWithBestSoil(),
                () => WorldObjectService.Instance.CompleteDryingRacks(),
                () => WorldObjectService.Instance.CompleteChemistryStations(),
                () => WorldObjectService.Instance.CompleteLabOvens(),
                () => WorldObjectService.Instance.CompleteMixingStations(),
                () => WorldObjectService.Instance.CompleteCauldrons(),
                seedId => WorldObjectService.Instance.SeedAllPots(seedId));
        }

        private void DrawVehiclesTab(ref float y, float w)
        {
            VehicleTabRenderer.Draw(ref y, w, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, VehicleService.Instance);
        }

        private void DrawPropertiesTab(ref float y, float w)
        {
            PropertiesTabRenderer.Draw(ref y, w, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, _propertiesState, PropertyWorkerService.Instance,
                VehicleService.Instance);
        }

        private void DrawItemsTab(ref float y, float w)
        {
            _itemsState.QualityIndex = ItemService.Instance.GetQualityIndex();
            ItemsTabRenderer.Draw(ref y, w, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, ItemService.Instance, _itemsState,
                quantity => _itemsState.SpawnQuantity = quantity,
                ItemService.Instance.SetQualityIndex,
                i => ItemService.Instance.SetFilter(i));
        }

        private void DrawPerformanceTab(ref float y, float w)
        {
            PerformanceTabRenderer.Draw(ref y, w,
                GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle,
                PerformanceService.Instance);
        }

        private void DrawRelationshipsTab(ref float y, float w)
        {
            RelationshipsTabRenderer.Draw(ref y, w,
                GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle,
                RelationshipService.Instance);
        }

        private void DrawQuestsTab(ref float y, float w)
        {
            QuestTabRenderer.Draw(ref y, w,
                GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle,
                QuestService.Instance);
        }

        private void DrawLobbyTab(ref float y, float w)
        {
            LobbyTabRenderer.Draw(ref y, w, GUISystemService.Instance.OnStyle,
                GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, _lobbyState,
                LobbyService.Instance.GetPlayerList(),
                LobbyService.Instance.TeleportPlayer,
                EffectsService.Instance.EffectIds,
                EffectsService.Instance.EffectLabels,
                effectId => EffectsService.Instance.ApplyEffect(effectId),
                () => LobbyService.Instance.TeleportPlayerUp(25f),
                () => LobbyService.Instance.SetRagdoll(true),
                () => LobbyService.Instance.SetRagdoll(false),
                EffectsService.Instance.ClearAllEffects);
        }

        private void TeleportAction(float distance, int dir)
        {
            var player = GameManagerService.Instance.GetLocalPlayer();
            if (player == null) return;

            var pos = player.transform.position;
            if (dir == 1) player.transform.position = new Vector3(pos.x, pos.y + distance, pos.z);
            else
            {
                var cam = Camera.main;
                if (cam != null) player.transform.position = new Vector3(pos.x + cam.transform.forward.x * distance, pos.y + cam.transform.forward.y * distance, pos.z + cam.transform.forward.z * distance);
            }
            Status($"TP {(dir == 0 ? "fwd" : "up")} {distance}m");
        }

        private void Heal() { try { GameManagerService.Instance.GetPlayerHealth()?.SetHealth(PlayerHealth.MaxHealth); Status("Healed"); } catch { } }

        private void ClearWanted()
        {
            try
            {
                var crime = GameManagerService.Instance.GetPlayerCrimeData();
                if (crime != null)
                {
                    crime.ClearCrimes();
                    for (int i = 0; i < 5; i++) crime.Deescalate();
                    crime.SetArrestProgress(0f);
                    crime.SetBodySearchProgress(0f);
                }
                Status("Cleared");
            }
            catch { }
        }

        private void ResetAllRuntimeChanges(bool includeNetworkedChanges)
        {
            try
            {
                PlayerCheatService.Instance.ResetAll();
                FlyingService.Instance.SetEnabled(false);
                FlyingService.Instance.SetSpeed(20f);
                FlyingService.Instance.SetDoubleSpaceHotkeyEnabled(true);
                FlyingService.Instance.SetVehicleFlyEnabled(false);
                SkateboardTuneService.Instance.ResetAll();
                CameraService.Instance.ToggleThirdPerson(false, _isMenuOpen);
                CameraService.Instance.SetDistance(1.90f);
                CameraService.Instance.SetHeight(0.80f);
                CameraService.Instance.SetShoulderOffset(0.20f);
                PerformanceService.Instance.RestoreRuntimeDefaults();
                ShapePrefabService.Instance.ResetSpawnOptions();

                if (includeNetworkedChanges)
                {
                    EffectsService.Instance.ClearAllEffects();
                    VehicleService.Instance.ResetDrivenVehicleTune();
                    ShapePrefabService.Instance.ClearAll();
                    if (DebugTestRoomService.Instance.IsLoaded)
                        DebugTestRoomService.Instance.ClearRoom();
                    if (!LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost())
                        TimeManagerService.Instance.SetTimeSpeed(1f);
                    Status("All Nugzz cheats and runtime changes reset");
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Runtime reset failed", ex);
                if (includeNetworkedChanges)
                    NotificationService.Instance.Error("Reset failed: " + ex.GetType().Name);
            }
        }

        private void ToggleFly(bool enabled)
        {
            if (enabled && !FlyingService.Instance.CanEnable(out string reason))
            {
                _cheatsState.FlyEnabled = false;
                Status("Fly unavailable while " + reason);
                return;
            }

            FlyingService.Instance.SetEnabled(enabled);
            _cheatsState.FlyEnabled = FlyingService.Instance.Enabled;
            Status(FlyingService.Instance.Enabled ? "Fly ON" : "Fly OFF");
        }

        private void SetFlySpeed(float speed) { FlyingService.Instance.SetSpeed(speed); _cheatsState.FlySpeed = FlyingService.Instance.Speed; }

        private void SetSpeedMultiplier(float multiplier)
        {
            PlayerCheatService.Instance.SpeedMultiplier = multiplier;
            _cheatsState.SpeedMultiplier = PlayerCheatService.Instance.SpeedMultiplier;
        }

        private void SetPlayerScale(float scale)
        {
            PlayerCheatService.Instance.PlayerScale = scale;
            _cheatsState.PlayerScale = PlayerCheatService.Instance.PlayerScale;
        }

        private void SetJumpMultiplier(float multiplier)
        {
            PlayerCheatService.Instance.JumpMultiplier = multiplier;
            _cheatsState.JumpMultiplier = PlayerCheatService.Instance.JumpMultiplier;
        }

        private void SetGravityMultiplier(float multiplier)
        {
            PlayerCheatService.Instance.GravityMultiplier = multiplier;
            _cheatsState.GravityMultiplier = PlayerCheatService.Instance.GravityMultiplier;
        }

        private void ToggleCamera(bool enabled)
        {
            if (enabled && !ThirdPersonCameraService.Instance.CanEnable(out string reason))
            {
                if (CameraService.Instance.ThirdPersonEnabled)
                    CameraService.Instance.ToggleThirdPerson(false, _isMenuOpen);

                Status("3rd person unavailable: " + reason);
                return;
            }

            CameraService.Instance.ToggleThirdPerson(enabled, _isMenuOpen);
        }

        private void SavePosition() { TeleportService.Instance.SavePosition(); }
        private void LoadPosition() { TeleportService.Instance.LoadPosition(); }

        private void DrawSettingsTab(ref float y, float w)
        {
            _settingsState.MenuKeybind = _menuKeyPreference.Value;
            _settingsState.UseGameStackLogic = ItemService.Instance.UseGameStackLogic;
            _settingsState.VerboseDebugLogging = DebugLogService.Instance.VerboseEnabled;
            _settingsState.KeybindOverlay = KeybindOverlayService.Instance.Enabled;
            SettingsTabRenderer.Draw(ref y, w, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, _settingsState, LobbyService.Instance.IsHost(),
                SetKeybind,
                value => ItemService.Instance.UseGameStackLogic = value, SetVerboseDebugLogging,
                SetKeybindOverlay, SaveManagementService.Instance,
                DebugTestRoomService.Instance, () => ResetAllRuntimeChanges(true));
        }

        private void SetKeybind(string key)
        {
            _menuKeyPreference.Value = key;
            _menuKey = (KeyCode)Enum.Parse(typeof(KeyCode), key, true);
            _preferences.SaveToFile(false);
            KeybindOverlayService.Instance.SetMenuKey(key);
            Status($"Keybind: {key}");
        }

        private void SetDoubleSpaceFlyHotkey(bool enabled)
        {
            _doubleSpaceFlyPreference.Value = enabled;
            _preferences.SaveToFile(false);
            FlyingService.Instance.SetDoubleSpaceHotkeyEnabled(enabled);
            _cheatsState.DoubleSpaceFlyHotkey = enabled;
            Status(enabled ? "Double-space fly ON" : "Double-space fly OFF");
        }

        private void SetKeybindOverlay(bool enabled)
        {
            _keybindOverlayPreference.Value = enabled;
            _preferences.SaveToFile(false);
            KeybindOverlayService.Instance.SetEnabled(enabled);
            Status(enabled ? "Keybind HUD ON" : "Keybind HUD OFF");
        }

        private void SetVerboseDebugLogging(bool enabled)
        {
            _verboseDebugPreference.Value = enabled;
            _preferences.SaveToFile(false);
            DebugLogService.Instance.SetVerbose(enabled);
            Status(enabled ? "Debug logs ON" : "Debug logs OFF");
        }

        private void Notify(string msg) { NotificationService.Instance.Notify(msg); }

        private void Status(string msg) { NotificationService.Instance.Status(msg); }

        private void SubscribeS1ApiEvents()
        {
            Type lifecycleType = FindLoadedType("S1API.Lifecycle.GameLifecycle");
            _s1LoadCompleteHandler = SubscribeStaticEvent(
                lifecycleType,
                "OnLoadComplete",
                nameof(HandleLoadComplete));
            _s1PreSceneChangeHandler = SubscribeStaticEvent(
                lifecycleType,
                "OnPreSceneChange",
                nameof(HandlePreSceneChange));

            Type apiPlayerType = FindLoadedType("S1API.Entities.Player");
            _s1LocalPlayerSpawnedHandler = SubscribeStaticEvent(
                apiPlayerType,
                "LocalPlayerSpawned",
                nameof(HandleApiPlayerSpawned));
            _s1PlayerSpawnedHandler = SubscribeStaticEvent(
                apiPlayerType,
                "PlayerSpawned",
                nameof(HandleApiPlayerSpawned));

            if (lifecycleType == null && apiPlayerType == null)
                LoggerInstance.Warning("[Nugzz] S1API runtime events unavailable; using MelonLoader scene hooks only");
        }

        private void UnsubscribeS1ApiEvents()
        {
            Type lifecycleType = FindLoadedType("S1API.Lifecycle.GameLifecycle");
            UnsubscribeStaticEvent(lifecycleType, "OnLoadComplete", _s1LoadCompleteHandler);
            UnsubscribeStaticEvent(lifecycleType, "OnPreSceneChange", _s1PreSceneChangeHandler);

            Type apiPlayerType = FindLoadedType("S1API.Entities.Player");
            UnsubscribeStaticEvent(apiPlayerType, "LocalPlayerSpawned", _s1LocalPlayerSpawnedHandler);
            UnsubscribeStaticEvent(apiPlayerType, "PlayerSpawned", _s1PlayerSpawnedHandler);

            _s1LoadCompleteHandler = null;
            _s1PreSceneChangeHandler = null;
            _s1LocalPlayerSpawnedHandler = null;
            _s1PlayerSpawnedHandler = null;
        }

        private Delegate SubscribeStaticEvent(Type declaringType, string eventName, string handlerName)
        {
            try
            {
                EventInfo eventInfo = declaringType?.GetEvent(
                    eventName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (eventInfo?.EventHandlerType == null)
                    return null;

                MethodInfo handler = GetType().GetMethod(
                    handlerName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (handler == null)
                    return null;

                Delegate subscription = Delegate.CreateDelegate(
                    eventInfo.EventHandlerType,
                    this,
                    handler,
                    false);
                if (subscription == null)
                    return null;

                eventInfo.AddEventHandler(null, subscription);
                return subscription;
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning(
                    "[Nugzz] S1API event hook failed for " + eventName + ": " + ex.Message);
                return null;
            }
        }

        private static void UnsubscribeStaticEvent(Type declaringType, string eventName, Delegate subscription)
        {
            if (subscription == null)
                return;

            try
            {
                EventInfo eventInfo = declaringType?.GetEvent(
                    eventName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                eventInfo?.RemoveEventHandler(null, subscription);
            }
            catch { }
        }

        private static Type FindLoadedType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            try
            {
                Type direct = Type.GetType(fullName + ", S1API", false);
                if (direct != null)
                    return direct;
            }
            catch { }

            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    Type type = assemblies[i]?.GetType(fullName, false);
                    if (type != null)
                        return type;
                }
            }
            catch { }

            return null;
        }
    }
}
