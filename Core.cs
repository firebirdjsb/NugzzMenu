using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Effects;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Law;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Property;
using Il2CppScheduleOne.Vehicles;
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
        private static Core _inst;
        
        private const int TAB_CHEATS = 0;
        private const int TAB_MONEY = 1;
        private const int TAB_TIME = 2;
        private const int TAB_VEHICLES = 3;
        private const int TAB_ITEMS = 4;
        private const int TAB_LOBBY = 5;
        private const int TAB_SETTINGS = 6;

        private static readonly string[] Tabs = new[] { "Cheats", "Money", "Time", "Vehicles", "Items", "Lobby", "Settings" };
        private static readonly string[] ML = new[] { "$500", "$1K", "$5K", "$10K", "$50K", "$100K", "$500K", "$1M" };
        private static readonly float[] MA = new[] { 500f, 1000f, 5000f, 10000f, 50000f, 100000f, 500000f, 1000000f };
        private static readonly int[] XA = new[] { 100, 500, 1000, 5000, 10000, 50000 };
        private static readonly string[] XLb = new[] { "100", "500", "1K", "5K", "10K", "50K" };

        private int _moneyIndex = 3;
        private int _xpIndex = 2;
        private int _itemQuantity = 1;
        private int _tabIndex;
        private bool _itemCacheInitialized;
        private readonly CheatsState _cheatsState = new CheatsState();
        private readonly ItemsState _itemsState = new ItemsState();
        private readonly LobbyState _lobbyState = new LobbyState();
        private readonly SettingsState _settingsState = new SettingsState();

        private KeyCode _bind = KeyCode.F8;
        private MelonPreferences_Category _prefCat;
        private MelonPreferences_Entry<string> _prefKey;
        private MelonPreferences_Entry<bool> _prefVerboseDebug;

        private Rect _windowRect = new Rect(40f, 40f, 820f, 690f);
        private float _measuredContentHeight = 620f;
        private int _qualityIndex = 2;
        private bool _o;

        public override void OnInitializeMelon()
        {
            _inst = this;

            _prefCat = MelonPreferences.CreateCategory("Nugzz", "Nugzz Settings");
            _prefKey = _prefCat.CreateEntry<string>("MenuKeybind", "F8", "Menu Toggle Key", "Key to open/close the Nugzz menu", false, false, null, null);
            _prefVerboseDebug = _prefCat.CreateEntry<bool>("VerboseDebugLogging", false, "Verbose Debug Logging", "Write extra Nugzz diagnostic logs", false, false, null, null);
            DebugLogService.Instance.SetVerbose(_prefVerboseDebug.Value);

            try { _bind = (KeyCode)Enum.Parse(typeof(KeyCode), _prefKey.Value, true); }
            catch { _bind = KeyCode.F8; }

            LoggerInstance.Msg($"  Nugzz v0.7.2 by XUnfairX | {_prefKey.Value} to open");

            GUISystemService.Instance.Initialize();
            TMPHybridService.Instance.Initialize();
            GameLifecycle.OnLoadComplete += HandleLoadComplete;
            GameLifecycle.OnPreSceneChange += HandlePreSceneChange;
            ApiPlayer.LocalPlayerSpawned += HandleApiPlayerSpawned;
            ApiPlayer.PlayerSpawned += HandleApiPlayerSpawned;
            try
            {
                var harmony = new HarmonyLib.Harmony("com.xunfairx.nugzzmenu.thirdperson");
                harmony.PatchAll(typeof(Core).Assembly);
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
            if (Input.GetKeyDown(KeyCode.BackQuote)) { ToggleDevConsole(); }
            if (Input.GetKeyDown(_bind)) { ToggleMenu(); }
            if (Input.GetKeyDown(KeyCode.G) && !_o) { ToggleCamera(!CameraService.Instance.ThirdPersonEnabled); }

            NotificationService.Instance.Update();
            PlayerCheatService.Instance.Update();
            CameraService.Instance.MaintainThirdPersonState(_o);
            ItemService.Instance.ProcessPendingSpawns();
            VehicleService.Instance.Update();
            VehicleCollisionService.Instance.Update();

            // Fallback cache initialization if Registry wasn't ready during scene init
            if (!_itemCacheInitialized && ItemService.Instance.ItemCount == 0)
            {
                ItemService.Instance.InitializeCache();
                _itemCacheInitialized = ItemService.Instance.IsCached;
            }

            if (FlyingService.Instance.Enabled) FlyingService.Instance.ApplyFlyMovement();
        }

        public override void OnLateUpdate()
        {
            if (CameraService.Instance.ThirdPersonEnabled) CameraService.Instance.ApplyThirdPersonCamera(_o);
            if (FlyingService.Instance.Enabled) FlyingService.Instance.ApplyPostMovementLock();
        }

        public override void OnFixedUpdate()
        {
            VehicleCollisionService.Instance.FixedUpdate();
        }

        public override void OnGUI()
        {
            var gui = GUISystemService.Instance;
            var notif = NotificationService.Instance;
            var tmp = TMPHybridService.Instance;

            gui.ApplyFontToSkin();

            tmp.BeginFrame();

            if (notif.HasNotification)
            {
                float w = 420f;
                GUI.Box(new Rect((Screen.width - w) / 2f, 10f, w, 30f), "", gui.BoxStyle);
                tmp.Label(
                    (Screen.width - w) / 2f, 10f, w, 30f,
                    string.IsNullOrEmpty(notif.NotificationMessage) ? "" : notif.NotificationMessage,
                    gui.GetColorForCategory(LabelCategory.Notif),
                    gui.GetFontSizeForCategory(LabelCategory.Notif),
                    gui.GetAlignmentForCategory(LabelCategory.Notif),
                    gui.GetStyleForCategory(LabelCategory.Notif));
            }

            if (!_o)
            {
                tmp.EndFrame();
                return;
            }

            if (notif.HasStatus)
            {
                tmp.Label(
                    4f, 24f, _windowRect.width - 8f, 16f,
                    string.IsNullOrEmpty(notif.StatusMessage) ? "" : notif.StatusMessage,
                    gui.GetColorForCategory(LabelCategory.Status),
                    gui.GetFontSizeForCategory(LabelCategory.Status),
                    gui.GetAlignmentForCategory(LabelCategory.Status),
                    gui.GetStyleForCategory(LabelCategory.Status));
            }

            ApplyDynamicWindowSize();
            ClampWindowToScreen();
            _windowRect = GUI.Window(98765, _windowRect, (GUI.WindowFunction)DrawWindowCallback, "", gui.WindowStyle);

            tmp.EndFrame();
        }

        private static void DrawWindowCallback(int id) { _inst?.DrawWindow(id); }

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
            switch (_tabIndex)
            {
                case TAB_MONEY: targetWidth = 620f; break;
                case TAB_TIME: targetWidth = 680f; break;
                case TAB_CHEATS: targetWidth = 700f; break;
                case TAB_LOBBY: targetWidth = 720f; break;
                case TAB_VEHICLES: targetWidth = 760f; break;
                case TAB_SETTINGS: targetWidth = 760f; break;
                case TAB_ITEMS: targetWidth = 820f; break;
                default: targetWidth = 700f; break;
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
            tmp.Label(contentW - 160f, 7f, 160f, 16f, $"v0.7.2 by XUnfairX  |  {_prefKey.Value}",
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
                    switch (_tabIndex)
                    {
                        case TAB_CHEATS: DrawCheatsTab(ref localY, drawW); break;
                        case TAB_MONEY: DrawMoneyTab(ref localY, drawW); break;
                        case TAB_TIME: DrawTimeTab(ref localY, drawW); break;
                        case TAB_VEHICLES: DrawVehiclesTab(ref localY, drawW); break;
                        case TAB_ITEMS: DrawItemsTab(ref localY, drawW); break;
                        case TAB_LOBBY: DrawLobbyTab(ref localY, drawW); break;
                        case TAB_SETTINGS: DrawSettingsTab(ref localY, drawW); break;
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
            float tabW = w / Tabs.Length;
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (GUIFit.Button(new Rect(i * tabW, y, tabW - 2f, 22f), Tabs[i], i == _tabIndex ? GUISystemService.Instance.TabActiveStyle : GUISystemService.Instance.TabStyle))
                    _tabIndex = i;
            }
            y += 26f;
        }

        private void ToggleMenu()
        {
            _o = !_o;
            try
            {
                Cursor.visible = _o;
                Cursor.lockState = _o ? CursorLockMode.None : CursorLockMode.Locked;
                var camera = PlayerCamera.Instance;
                camera?.SetCanLook(!_o && !CameraService.Instance.ThirdPersonEnabled);
            }
            catch { }
        }

        private void DrawCheatsTab(ref float y, float w)
        {
            var state = _cheatsState;
            state.GodMode = PlayerCheatService.Instance.GodMode;
            state.InfiniteStamina = PlayerCheatService.Instance.InfiniteStamina;
            state.InfiniteEnergy = PlayerCheatService.Instance.InfiniteEnergy;
            state.SpeedBoost = PlayerCheatService.Instance.SpeedBoost;
            state.SpeedMultiplier = PlayerCheatService.Instance.SpeedMultiplier;
            state.InfiniteAmmo = PlayerCheatService.Instance.InfiniteAmmo;
            state.NeverWanted = PlayerCheatService.Instance.NeverWanted;
            state.FlyEnabled = FlyingService.Instance.Enabled;
            state.FlySpeed = FlyingService.Instance.Speed;
            state.ThirdPerson = CameraService.Instance.ThirdPersonEnabled;
            state.CameraDistance = CameraService.Instance.Distance;
            state.CameraHeight = CameraService.Instance.Height;
            state.CameraShoulder = CameraService.Instance.ShoulderOffset;

            CheatsTabRenderer.Draw(ref y, w, GUISystemService.Instance.HeaderStyle,
                 GUISystemService.Instance.LabelStyle, GUISystemService.Instance.OnStyle,
                 GUISystemService.Instance.OffStyle, GUISystemService.Instance.ButtonStyle,
                 GUISystemService.Instance.BoxStyle, state,
                 TeleportAction, Heal, ClearWanted, SetSpeedMultiplier, ToggleFly, SetFlySpeed, ToggleCamera,
                 CameraService.Instance.SetDistance, CameraService.Instance.SetHeight, CameraService.Instance.SetShoulderOffset,
                 SavePosition, LoadPosition, TutorialTownTeleport);

            PlayerCheatService.Instance.GodMode = state.GodMode;
            PlayerCheatService.Instance.InfiniteStamina = state.InfiniteStamina;
            PlayerCheatService.Instance.InfiniteEnergy = state.InfiniteEnergy;
            PlayerCheatService.Instance.SpeedBoost = state.SpeedBoost;
            PlayerCheatService.Instance.SpeedMultiplier = state.SpeedMultiplier;
            PlayerCheatService.Instance.InfiniteAmmo = state.InfiniteAmmo;
            PlayerCheatService.Instance.NeverWanted = state.NeverWanted;
        }

        private void DrawMoneyTab(ref float y, float w)
        {
            MoneyTabRenderer.Draw(ref y, w, GUISystemService.Instance.HeaderStyle,
                GUISystemService.Instance.LabelStyle, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, ML, XLb, _moneyIndex, _xpIndex,
                i => _moneyIndex = i, i => _xpIndex = i,
                AddCash, AddOnlineBalance, AddXP);
        }

        private void AddCash() { try { EconomyService.Instance.AdjustCash(MA[_moneyIndex], true); Status($"+${MA[_moneyIndex]:N0} cash"); } catch { } }

        private void AddOnlineBalance() { try { EconomyService.Instance.AdjustOnlineBalance(MA[_moneyIndex]); Status($"+${MA[_moneyIndex]:N0} online"); } catch { } }

        private void AddXP() { try { GameManagerService.Instance.AddXP(XA[_xpIndex]); Status($"+{XA[_xpIndex]} XP"); } catch { } }

        private void DrawTimeTab(ref float y, float w)
        {
            TimeTabRenderer.Draw(ref y, w, GUISystemService.Instance.HeaderStyle,
                GUISystemService.Instance.LabelStyle, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle,
                TimeManagerService.Instance.SetTimeSpeed,
                i => TimeManagerService.Instance.SetTimeOfDay(i),
                () => WorldObjectService.Instance.GrowAllPlants(),
                () => WorldObjectService.Instance.CompleteDryingRacks());
        }

        private void DrawVehiclesTab(ref float y, float w)
        {
            VehicleTabRenderer.Draw(ref y, w, GUISystemService.Instance.HeaderStyle,
                GUISystemService.Instance.LabelStyle, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, VehicleService.Instance);
        }

        private void DrawItemsTab(ref float y, float w)
        {
            _qualityIndex = ItemService.Instance.GetQualityIndex();
            _itemsState.QualityIndex = _qualityIndex;
            _itemsState.SpawnQuantity = _itemQuantity;
            ItemsTabRenderer.Draw(ref y, w, GUISystemService.Instance.HeaderStyle,
                GUISystemService.Instance.LabelStyle, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, ItemService.Instance, _itemsState,
                i => _itemQuantity = i,
                i => { _qualityIndex = i; ItemService.Instance.SetQualityIndex(i); },
                i => ItemService.Instance.SetFilter(i));
        }

        private void DrawLobbyTab(ref float y, float w)
        {
            LobbyTabRenderer.Draw(ref y, w, GUISystemService.Instance.HeaderStyle,
                GUISystemService.Instance.LabelStyle, GUISystemService.Instance.OnStyle,
                GUISystemService.Instance.OffStyle, GUISystemService.Instance.ButtonStyle,
                GUISystemService.Instance.BoxStyle, _lobbyState,
                LobbyService.Instance.GetPlayerList(),
                LobbyService.Instance.TeleportPlayer,
                EffectsService.Instance.EffectIds,
                EffectsService.Instance.EffectLabels,
                effectId => EffectsService.Instance.ApplyEffect(effectId),
                () => LobbyService.Instance.TeleportPlayerUp(25f),
                () => LobbyService.Instance.SetRagdoll(true),
                () => LobbyService.Instance.SetRagdoll(false),
                () => { LobbyService.Instance.ClearEffects(); EffectsService.Instance.ClearAllEffects(); });
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

        private void ToggleCamera(bool enabled) { CameraService.Instance.ToggleThirdPerson(enabled, _o); }

        private void SavePosition() { TeleportService.Instance.SavePosition(); }
        private void LoadPosition() { TeleportService.Instance.LoadPosition(); }
        private void TutorialTownTeleport() { PlayerCheatService.Instance.TeleportToTutorialTown(); }

        private void DrawSettingsTab(ref float y, float w)
        {
             _settingsState.MenuKeybind = _prefKey.Value;
             _settingsState.UseGameStackLogic = ItemService.Instance.UseGameStackLogic;
             _settingsState.VerboseDebugLogging = DebugLogService.Instance.VerboseEnabled;
             _settingsState.PlaceAnywhere = BuildingService.Instance.PlaceAnywhere;
             SettingsTabRenderer.Draw(ref y, w, GUISystemService.Instance.HeaderStyle,
                 GUISystemService.Instance.LabelStyle, GUISystemService.Instance.ButtonStyle,
                 GUISystemService.Instance.BoxStyle, _settingsState, LobbyService.Instance.IsHost(),
                 Lobby.Instance, k => SetKeybind(k), JoinLanAddress, ForceExitToMainMenu, OpenSteamInviteUI,
                 v => ItemService.Instance.UseGameStackLogic = v, SetVerboseDebugLogging, v => BuildingService.Instance.SetPlaceAnywhere(v));
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
            _prefKey.Value = key;
            _bind = (KeyCode)Enum.Parse(typeof(KeyCode), key, true);
            _prefCat.SaveToFile(false);
            Status($"Keybind: {key}");
        }

        private void JoinLanAddress(string address)
        {
            if (Lobby.Instance != null && !string.IsNullOrEmpty(address)) { Lobby.Instance.JoinAsClient(address.Trim()); Status($"Joining: {address}"); }
        }

        private void SetVerboseDebugLogging(bool enabled)
        {
            _prefVerboseDebug.Value = enabled;
            _prefCat.SaveToFile(false);
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
