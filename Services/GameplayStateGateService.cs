using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne;
using Il2CppScheduleOne.AvatarFramework.Customization;
using Il2CppScheduleOne.UI;
using MelonLoader;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Keeps Nugzz controls out of native screens and text fields that own input.
    /// </summary>
    public sealed class GameplayStateGateService
    {
        private static readonly GameplayStateGateService _instance = new GameplayStateGateService();
        public static GameplayStateGateService Instance => _instance;

        public bool MenuOpen { get; private set; }

        // Runtime Harmony patches for input blocking (replaces attribute-based patches to handle stripped methods)
        private static HarmonyLib.Harmony _inputBlockHarmony;
        private static bool _inputBlockPatchesApplied;
        private const string InputBlockHarmonyId = "com.xunfairx.nugzzmenu.inputblock";

        private GameplayStateGateService() { }

        public void SetMenuOpen(bool open)
        {
            MenuOpen = open;
            UpdateInputBlockPatches();
        }

        private void UpdateInputBlockPatches()
        {
            try
            {
                if (MenuOpen)
                    ApplyInputBlockPatches();
                else
                    RemoveInputBlockPatches();
            }
            catch (System.Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Input block patch update failed: " + ex.Message);
            }
        }

        private static void ApplyInputBlockPatches()
        {
            if (_inputBlockPatchesApplied)
                return;

            try
            {
                _inputBlockHarmony = new HarmonyLib.Harmony(InputBlockHarmonyId);

                // Patch PauseMenu.Pause
                try
                {
                    var pauseMethod = AccessTools.Method(typeof(PauseMenu), nameof(PauseMenu.Pause));
                    if (pauseMethod != null)
                    {
                        var prefix = AccessTools.Method(typeof(GameplayStateGateService), nameof(PauseMenuPrefix));
                        if (prefix != null)
                            _inputBlockHarmony.Patch(pauseMethod, prefix: new HarmonyMethod(prefix));
                    }
                }
                catch (System.NotSupportedException ex)
                {
                    DebugLogService.Instance.Verbose("PauseMenu.Pause patch skipped (method stripped): " + ex.Message);
                }
                catch (System.Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning("PauseMenu.Pause patch failed: " + ex.Message);
                }

                // Patch GameInput callbacks
                var blockedCallbacks = new[]
                {
                    "OnMotion", "OnPrimaryClick", "OnSecondaryClick", "OnTertiaryClick",
                    "OnJump", "OnCrouch", "OnSprint", "OnEscape", "OnBack",
                    "OnInteract", "OnSubmit", "OnTogglePhone", "OnVehicleToggleLights",
                    "OnVehicleHandbrake", "OnRotateLeft", "OnRotateRight",
                    "OnManagementMode", "OnOpenMap", "OnOpenJournal", "OnOpenTexts",
                    "OnQuickMove", "OnToggleFlashlight", "OnViewAvatar", "OnReload",
                    "OnCamera", "OnScrollWheel", "OnInventoryLeft", "OnInventoryRight",
                    "OnHolster", "OnControllerCombo", "OnVehicleResetCamera",
                    "OnVehicleDrive", "OnSkateboardDismount", "OnSkateboardMount",
                    "OnTogglePauseMenu", "OnUINavigationDirection",
                    "OnUICyclePanelDirection", "OnUITabNavigationPrimary",
                    "OnUITabNavigationSecondary", "OnUITabNavigationTertiary",
                    "OnUIScrollbar", "OnUIMapNavigationDirection", "OnUIMapZoom",
                    "OnUIModifyAmountIncrementTierOne", "OnUIModifyAmountIncrementTierTwo",
                    "OnUIModifyAmountIncrementTierThree"
                };

                var gameInputType = typeof(GameInput);
                var prefixMethod = AccessTools.Method(typeof(GameplayStateGateService), nameof(GameInputCallbackPrefix));

                foreach (var callback in blockedCallbacks)
                {
                    try
                    {
                        var method = gameInputType.GetMethod(callback, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null && prefixMethod != null)
                        {
                            _inputBlockHarmony.Patch(method, prefix: new HarmonyMethod(prefixMethod));
                        }
                    }
                    catch (System.NotSupportedException ex)
                    {
                        DebugLogService.Instance.Verbose($"GameInput.{callback} patch skipped (method stripped): " + ex.Message);
                    }
                    catch (System.Exception ex)
                    {
                        DebugLogService.Instance.VerboseWarning($"GameInput.{callback} patch failed: " + ex.Message);
                    }
                }

                _inputBlockPatchesApplied = true;
            }
            catch (System.Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Failed to apply input block patches: " + ex.Message);
                try { _inputBlockHarmony?.UnpatchSelf(); } catch { }
                _inputBlockHarmony = null;
            }
        }

        private static void RemoveInputBlockPatches()
        {
            if (!_inputBlockPatchesApplied)
                return;

            try
            {
                _inputBlockHarmony?.UnpatchSelf();
            }
            catch { }
            finally
            {
                _inputBlockHarmony = null;
                _inputBlockPatchesApplied = false;
            }
        }

        // Prefix methods for runtime patches
        private static bool PauseMenuPrefix()
        {
            if (!GameplayStateGateService.Instance.MenuOpen)
                return true;

            NotificationService.Instance.Status("Close Nugzz before pausing");
            return false;
        }

        private static bool GameInputCallbackPrefix()
        {
            return !GameplayStateGateService.Instance.MenuOpen;
        }

        public bool IsModControlBlocked(out string reason)
        {
            if (IsCharacterCreatorOpen())
            {
                reason = "character creation";
                return true;
            }

            // The main-menu save editor must remain accessible even though the
            // game's pause/UI state owns input there.
            if (SaveManagementService.Instance.IsMainMenu)
            {
                reason = null;
                return false;
            }

            if (IsPaused())
            {
                reason = "game paused";
                return true;
            }

            if (NativeActivityGateService.Instance.TryGetBlockReason(out reason))
                return true;

            // Once Nugzz is open it intentionally unlocks and shows the cursor.
            // Re-evaluating the pointer-based screen gate at that point would
            // mistake Nugzz itself for a native interface and close it again.
            if (!MenuOpen && IsExclusiveGameScreenActive())
            {
                reason = "another game interface is active";
                return true;
            }

            reason = null;
            return false;
        }

        public bool AreFeatureHotkeysBlocked()
        {
            if (AreGameplayActionsBlocked())
                return true;

            return IsExclusiveGameScreenActive();
        }

        private static bool IsExclusiveGameScreenActive()
        {
            try
            {
                if (UIScreenManager.Instance == null ||
                    !UIScreenManager.Instance.IsAnyScreenActive())
                    return false;

                // The updated game registers its normal HUD as a UIScreen.
                // Only block Nugzz when the active screen actually owns pointer input.
                return UnityEngine.Cursor.visible ||
                       UnityEngine.Cursor.lockState != UnityEngine.CursorLockMode.Locked;
            }
            catch
            {
                return false;
            }
        }

        public bool AreGameplayActionsBlocked()
        {
            return MenuOpen || GUIFit.IsTextFieldActive ||
                   ConsoleAutocompleteService.Instance.IsTyping ||
                   IsPaused() || IsCharacterCreatorOpen() ||
                   NativeActivityGateService.Instance.TryGetBlockReason(out _);
        }

        public static bool IsPaused()
        {
            try { return PauseMenu.Instance != null && PauseMenu.Instance.IsPaused; }
            catch { return false; }
        }

        public static bool IsCharacterCreatorOpen()
        {
            try
            {
                return CharacterCreator.Instance != null && CharacterCreator.Instance.IsOpen;
            }
            catch
            {
                return false;
            }
        }
    }
}
