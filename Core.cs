using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.UI;
using MelonLoader;
using S1API.Lifecycle;
using UnityEngine;
using UnityEngine.SceneManagement;
using NugzzMenu.Services;
using NugzzMenu.UI;
using ApiPlayer = S1API.Entities.Player;

namespace NugzzMenu
{
    public class Core : MelonMod
    {
        private const string Version = "0.9.0";
        private const int WindowId = 98765;

        private enum MenuTab
        {
            Cheats,
            Money,
            Time,
            Vehicles,
            Items,
            Lobby,
            Settings
        }

        private static readonly string[] TabLabels = { "Cheats", "Money", "Time", "Vehicles", "Items", "Lobby", "Settings" };
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

        private KeyCode _menuKey = KeyCode.F8;
        private MelonPreferences_Category _preferences;
        private MelonPreferences_Entry<string> _menuKeyPreference;
        private MelonPreferences_Entry<bool> _verboseDebugPreference;
        private HarmonyLib.Harmony _harmony;

        private Rect _windowRect = new Rect(40f, 40f, 820f, 690f);
        private float _measuredContentHeight = 620f;
        private bool _isMenuOpen;

        public override void OnInitializeMelon()
        {
            _preferences = MelonPreferences.CreateCategory("Nugzz", "Nugzz Settings");
            _menuKeyPreference = _preferences.CreateEntry<string>("MenuKeybind", "F8", "Menu Toggle Key", "Key to open/close the Nugzz menu", false, false, null, null);
            _verboseDebugPreference = _preferences.CreateEntry<bool>("VerboseDebugLogging", false, "Verbose Debug Logging", "Write extra Nugzz diagnostic logs", false, false, null, null);
            DebugLogService.Instance.SetVerbose(_verboseDebugPreference.Value);

            if (!Enum.TryParse(_menuKeyPreference.Value, true, out _menuKey))
                _menuKey = KeyCode.F8;

            LoggerInstance.Msg($"Nugzz v{Version} by XUnfairX | {_menuKeyPreference.Value} to open");

            GUISystemService.Instance.Initialize();
            GameLifecycle.OnLoadComplete += HandleLoadComplete;
            GameLifecycle.OnPreSceneChange += HandlePreSceneChange;
            ApiPlayer.LocalPlayerSpawned += HandleApiPlayerSpawned;
            ApiPlayer.PlayerSpawned += HandleApiPlayerSpawned;
            try
            {
                _harmony = new HarmonyLib.Harmony("com.xunfairx.nugzzmenu.thirdperson");
                _harmony.PatchAll(typeof(Core).Assembly);
                CompatibilityService.Instance.ApplyRuntimeCompatibilityFixes(_harmony);
                LoggerInstance.Msg("[Nugzz] Harmony patches applied successfully");
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Warning("[Nugzz] Harmony patch install failed: " + ex);
            }
            LoggerInstance.Msg("[Nugzz] All services initialized successfully");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            ManagerCacheService.Instance.Invalidate();

            if (sceneName == "Main")
            {
                InitializeGameplayServices();
            }
        }

        public override void OnDeinitializeMelon()
        {
            GameLifecycle.OnLoadComplete -= HandleLoadComplete;
            GameLifecycle.OnPreSceneChange -= HandlePreSceneChange;
            ApiPlayer.LocalPlayerSpawned -= HandleApiPlayerSpawned;
            ApiPlayer.PlayerSpawned -= HandleApiPlayerSpawned;
            VehicleCollisionService.Instance.Reset();
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
            ManagerCacheService.Instance.Invalidate();
            VehicleCollisionService.Instance.Reset();
        }

        private void HandleApiPlayerSpawned(ApiPlayer player)
        {
            ManagerCacheService.Instance.Invalidate();
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
            if (Input.GetKeyDown(KeyCode.BackQuote))
                ToggleDevConsole();
            if (Input.GetKeyDown(_menuKey))
                ToggleMenu();
            if (Input.GetKeyDown(KeyCode.G) && !_isMenuOpen)
                ToggleCamera(!CameraService.Instance.ThirdPersonEnabled);

            NotificationService.Instance.Update();
            PlayerCheatService.Instance.Update();
            EffectsService.Instance.Update();
            CameraService.Instance.MaintainThirdPersonState(_isMenuOpen);
            ItemService.Instance.ProcessPendingSpawns();
            VehicleService.Instance.Update();
            VehicleCollisionService.Instance.Update();
            BuildingService.Instance.UpdateOutsideItemPickup(_isMenuOpen);
            CompatibilityService.Instance.Update(_harmony);

            // The registry can become available a few frames after scene initialization.
            if (!_itemCacheInitialized && ItemService.Instance.ItemCount == 0)
            {
                ItemService.Instance.InitializeCache();
                _itemCacheInitialized = ItemService.Instance.IsCached;
            }

            if (FlyingService.Instance.Enabled)
                FlyingService.Instance.ApplyFlyMovement();
        }

        public override void OnLateUpdate()
        {
            if (CameraService.Instance.ThirdPersonEnabled)
                CameraService.Instance.ApplyThirdPersonCamera(_isMenuOpen);
            if (FlyingService.Instance.Enabled)
                FlyingService.Instance.ApplyPostMovementLock();
        }

        public override void OnFixedUpdate()
        {
            VehicleCollisionService.Instance.FixedUpdate();
        }

        public override void OnGUI()
        {
            var gui = GUISystemService.Instance;
            var notifications = NotificationService.Instance;
            var text = TMPHybridService.Instance;

            gui.ApplyFontToSkin();

            if (notifications.HasNotification)
            {
                const float notificationWidth = 420f;
                float notificationX = (Screen.width - notificationWidth) / 2f;
                GUI.Box(new Rect(notificationX, 10f, notificationWidth, 30f), "", gui.BoxStyle);
                text.Label(
                    notificationX, 10f, notificationWidth, 30f,
                    notifications.NotificationMessage ?? string.Empty,
                    gui.GetColorForCategory(LabelCategory.Notif),
                    gui.GetFontSizeForCategory(LabelCategory.Notif),
                    gui.GetAlignmentForCategory(LabelCategory.Notif),
                    gui.GetStyleForCategory(LabelCategory.Notif));
            }

            if (!_isMenuOpen)
                return;

            if (notifications.HasStatus)
            {
                text.Label(
                    4f, 24f, _windowRect.width - 8f, 16f,
                    notifications.StatusMessage ?? string.Empty,
                    gui.GetColorForCategory(LabelCategory.Status),
                    gui.GetFontSizeForCategory(LabelCategory.Status),
                    gui.GetAlignmentForCategory(LabelCategory.Status),
                    gui.GetStyleForCategory(LabelCategory.Status));
            }

            ApplyDynamicWindowSize();
            ClampWindowToScreen();
            _windowRect = GUI.Window(WindowId, _windowRect, (GUI.WindowFunction)DrawWindow, string.Empty, gui.WindowStyle);
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
                case MenuTab.Settings:
                    targetWidth = 760f;
                    break;
                case MenuTab.Items:
                    targetWidth = 820f;
                    break;
                default:
                    targetWidth = 700f;
                    break;
            }

            _windowRect.width = targetWidth;
            _windowRect.height = _measuredContentHeight + 66f;
        }

        private void DrawWindow(int id)
        {
            var gui = GUISystemService.Instance;
            var tmp = TMPHybridService.Instance;

            float contentW = _windowRect.width - 16f;
            float y = 2f;

            GUI.DrawTexture(new Rect(-8f, -8f, _windowRect.width + 16f, 30f), GUISystemService.Instance.TitleTexture);
            tmp.Label(6f, 0f, 200f, 28f, "Nugzz",
                gui.GetColorForCategory(LabelCategory.Title),
                gui.GetFontSizeForCategory(LabelCategory.Title),
                gui.GetAlignmentForCategory(LabelCategory.Title),
                gui.GetStyleForCategory(LabelCategory.Title));
            tmp.Label(contentW - 160f, 7f, 160f, 16f, $"v{Version} by XUnfairX  |  {_menuKeyPreference.Value}",
                gui.GetColorForCategory(LabelCategory.Subtitle),
                gui.GetFontSizeForCategory(LabelCategory.Subtitle),
                gui.GetAlignmentForCategory(LabelCategory.Subtitle),
                gui.GetStyleForCategory(LabelCategory.Subtitle));
            y = 24f;

            DrawTabs(ref y, contentW);

            try
            {
                float drawW = Mathf.Min(contentW, 760f);
                float drawX = Mathf.Max(0f, (contentW - drawW) * 0.5f);
                float localY = 0f;
                GUI.BeginGroup(new Rect(drawX, y, drawW, Mathf.Max(0f, _windowRect.height - y - 8f)));
                try
                {
                    switch (_selectedTab)
                    {
                        case MenuTab.Cheats:
                            DrawCheatsTab(ref localY, drawW);
                            break;
                        case MenuTab.Money:
                            DrawMoneyTab(ref localY, drawW);
                            break;
                        case MenuTab.Time:
                            DrawTimeTab(ref localY, drawW);
                            break;
                        case MenuTab.Vehicles:
                            DrawVehiclesTab(ref localY, drawW);
                            break;
                        case MenuTab.Items:
                            DrawItemsTab(ref localY, drawW);
                            break;
                        case MenuTab.Lobby:
                            DrawLobbyTab(ref localY, drawW);
                            break;
                        case MenuTab.Settings:
                            DrawSettingsTab(ref localY, drawW);
                            break;
                    }
                }
                finally
                {
                    GUI.EndGroup();
                }
                y += localY;
                _measuredContentHeight = Mathf.Max(100f, localY);
            }
            catch (Exception ex)
            {
                tmp.Label(4f, y, contentW, 20f, "Error: " + ex.Message,
                    gui.GetColorForCategory(LabelCategory.Error),
                    gui.GetFontSizeForCategory(LabelCategory.Error),
                    gui.GetAlignmentForCategory(LabelCategory.Error),
                    gui.GetStyleForCategory(LabelCategory.Error));
            }

            GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 26f));
        }

        private void DrawTabs(ref float y, float w)
        {
            float tabWidth = w / TabLabels.Length;
            for (int i = 0; i < TabLabels.Length; i++)
            {
                if (GUIFit.Button(new Rect(i * tabWidth, y, tabWidth - 2f, 22f), TabLabels[i],
                        i == (int)_selectedTab ? GUISystemService.Instance.TabActiveStyle : GUISystemService.Instance.TabStyle))
                {
                    _selectedTab = (MenuTab)i;
                }
            }
            y += 26f;
        }

        private void ToggleMenu()
        {
            _isMenuOpen = !_isMenuOpen;
            ApplyMenuInputState();
        }

        private void ApplyMenuInputState()
        {
            try
            {
                bool keepNativeCursor = !_isMenuOpen && ShouldKeepNativeCursor();
                Cursor.visible = _isMenuOpen || keepNativeCursor;
                Cursor.lockState = (_isMenuOpen || keepNativeCursor)
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;

                var camera = PlayerCamera.Instance;
                camera?.SetCanLook(
                    !_isMenuOpen &&
                    !keepNativeCursor &&
                    !CameraService.Instance.ThirdPersonEnabled);
            }
            catch { }
        }

        private static bool ShouldKeepNativeCursor()
        {
            try
            {
                if (IsPauseMenuOpen())
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

        private static bool IsPauseMenuOpen()
        {
            try
            {
                var pauseMenu = PauseMenu.Instance;
                return pauseMenu != null && pauseMenu.IsPaused;
            }
            catch
            {
                return false;
            }
        }

        private void DrawCheatsTab(ref float y, float w)
        {
            var state = _cheatsState;
            state.GodMode = PlayerCheatService.Instance.GodMode;
            state.InfiniteStamina = PlayerCheatService.Instance.InfiniteStamina;
            state.SpeedBoost = PlayerCheatService.Instance.SpeedBoost;
            state.SpeedMultiplier = PlayerCheatService.Instance.SpeedMultiplier;
            state.PlayerScale = PlayerCheatService.Instance.PlayerScale;
            state.InfiniteAmmo = PlayerCheatService.Instance.InfiniteAmmo;
            state.NeverWanted = PlayerCheatService.Instance.NeverWanted;
            state.FlyEnabled = FlyingService.Instance.Enabled;
            state.FlySpeed = FlyingService.Instance.Speed;
            state.ThirdPerson = CameraService.Instance.ThirdPersonEnabled;
            state.CameraDistance = CameraService.Instance.Distance;
            state.CameraHeight = CameraService.Instance.Height;
            state.CameraShoulder = CameraService.Instance.ShoulderOffset;

            CheatsTabRenderer.Draw(ref y, w, GUISystemService.Instance.OnStyle,
                GUISystemService.Instance.OffStyle, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, state,
                 TeleportAction, Heal, ClearWanted, SetSpeedMultiplier, SetPlayerScale,
                 ToggleFly, SetFlySpeed, ToggleCamera,
                 CameraService.Instance.SetDistance, CameraService.Instance.SetHeight, CameraService.Instance.SetShoulderOffset,
                 SavePosition, LoadPosition);

            PlayerCheatService.Instance.GodMode = state.GodMode;
            PlayerCheatService.Instance.InfiniteStamina = state.InfiniteStamina;
            PlayerCheatService.Instance.SpeedBoost = state.SpeedBoost;
            PlayerCheatService.Instance.SpeedMultiplier = state.SpeedMultiplier;
            PlayerCheatService.Instance.PlayerScale = state.PlayerScale;
            PlayerCheatService.Instance.InfiniteAmmo = state.InfiniteAmmo;
            PlayerCheatService.Instance.NeverWanted = state.NeverWanted;
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
                () => WorldObjectService.Instance.CompleteDryingRacks());
        }

        private void DrawVehiclesTab(ref float y, float w)
        {
            VehicleTabRenderer.Draw(ref y, w, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, VehicleService.Instance);
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

        private void Heal() { try { GameManagerService.Instance.GetPlayerHealth()?.SetHealth(PlayerHealth.MAX_HEALTH); Status("Healed"); } catch { } }

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

        private void ToggleFly(bool enabled) { FlyingService.Instance.SetEnabled(enabled); Status(enabled ? "Fly ON" : "Fly OFF"); }

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

        private void ToggleCamera(bool enabled) { CameraService.Instance.ToggleThirdPerson(enabled, _isMenuOpen); }

        private void SavePosition() { TeleportService.Instance.SavePosition(); }
        private void LoadPosition() { TeleportService.Instance.LoadPosition(); }

        private void DrawSettingsTab(ref float y, float w)
        {
            _settingsState.MenuKeybind = _menuKeyPreference.Value;
            _settingsState.UseGameStackLogic = ItemService.Instance.UseGameStackLogic;
            _settingsState.VerboseDebugLogging = DebugLogService.Instance.VerboseEnabled;
            _settingsState.PlaceAnywhere = BuildingService.Instance.PlaceAnywhere;
            SettingsTabRenderer.Draw(ref y, w, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, _settingsState, LobbyService.Instance.IsHost(),
                Lobby.Instance, SetKeybind, JoinLanAddress, ForceExitToMainMenu, OpenSteamInviteUI,
                value => ItemService.Instance.UseGameStackLogic = value, SetVerboseDebugLogging,
                BuildingService.Instance.SetPlaceAnywhere);
        }

        private void OpenSteamInviteUI()
        {
            try
            {
                if (Lobby.Instance != null)
                {
                    Lobby.Instance.TryOpenInviteInterface();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Notify("Steam invite error: " + ex.Message);
            }
        }

        private void SetKeybind(string key)
        {
            _menuKeyPreference.Value = key;
            _menuKey = (KeyCode)Enum.Parse(typeof(KeyCode), key, true);
            _preferences.SaveToFile(false);
            Status($"Keybind: {key}");
        }

        private void JoinLanAddress(string address)
        {
            if (Lobby.Instance == null || string.IsNullOrWhiteSpace(address))
                return;

            Lobby.Instance.JoinAsClient(address.Trim());
            Status($"Joining: {address.Trim()}");
        }

        private void SetVerboseDebugLogging(bool enabled)
        {
            _verboseDebugPreference.Value = enabled;
            _preferences.SaveToFile(false);
            DebugLogService.Instance.SetVerbose(enabled);
            Status(enabled ? "Debug logs ON" : "Debug logs OFF");
        }

        private void ForceExitToMainMenu()
        {
            try
            {
                var loadManager = LoadManager.Instance;
                if (loadManager == null)
                    loadManager = UnityEngine.Object.FindObjectOfType<LoadManager>();

                if (loadManager != null)
                {
                    loadManager.ExitToMenu(null, null, false);
                    Status("Exiting to main menu");
                    return;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] LoadManager.ExitToMenu failed: " + ex.Message);
            }

            try
            {
                if (Lobby.Instance != null && Lobby.Instance.IsInLobby)
                    Lobby.Instance.LeaveLobby();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Lobby leave fallback failed: " + ex.Message);
            }

            SceneManager.LoadScene("Menu");
            Status("Forced to menu scene");
        }

        private void ToggleDevConsole()
        {
            try
            {
                var consoleUI = UnityEngine.Object.FindObjectOfType<Il2CppScheduleOne.UI.ConsoleUI>(true);
                if (consoleUI == null)
                {
                    var consoles = UnityEngine.Resources.FindObjectsOfTypeAll<Il2CppScheduleOne.UI.ConsoleUI>();
                    if (consoles != null && consoles.Length > 0) consoleUI = consoles[0];
                }
                if (consoleUI != null) { consoleUI.SetIsOpen(true); Notify("Dev console opened"); }
            }
            catch { }
        }

        private void Notify(string msg) { NotificationService.Instance.Notify(msg); }

        private void Status(string msg) { NotificationService.Instance.Status(msg); }
    }
}
