using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne;
using Il2CppScheduleOne.AvatarFramework.Customization;
using Il2CppScheduleOne.UI;

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

        private const string InputBlockHarmonyId = "com.xunfairx.nugzzmenu.inputblock";
        private static HarmonyLib.Harmony _inputBlockHarmony;
        private static bool _inputBlockPatchesApplied;

        private GameplayStateGateService() { }

        public void Initialize()
        {
            MenuOpen = false;
            ApplyInputBlockPatches();
        }

        public void SetMenuOpen(bool open)
        {
            MenuOpen = open;
        }

        public void Shutdown()
        {
            MenuOpen = false;
            RemoveInputBlockPatches();
        }

        private static void ApplyInputBlockPatches()
        {
            if (_inputBlockPatchesApplied)
                return;

            try
            {
                _inputBlockHarmony = new HarmonyLib.Harmony(InputBlockHarmonyId);

                PatchInputMethod(
                    typeof(PauseMenu),
                    nameof(PauseMenu.Pause),
                    nameof(PauseMenuPrefix));

                string[] blockedCallbacks =
                {
                    "OnMotion", "OnPrimaryClick", "OnSecondaryClick", "OnTertiaryClick",
                    "OnJump", "OnCrouch", "OnSprint", "OnEscape", "OnBack",
                    "OnInteract", "OnSubmit", "OnTogglePhone", "OnVehicleToggleLights",
                    "OnVehicleHandbrake", "OnRotateLeft", "OnRotateRight", "OnManagementMode",
                    "OnOpenMap", "OnOpenJournal", "OnOpenTexts", "OnQuickMove",
                    "OnToggleFlashlight", "OnViewAvatar", "OnReload",
                    "OnScrollWheel", "OnInventoryLeft", "OnInventoryRight", "OnHolster",
                    "OnControllerCombo",
                    "OnVehicleDrive", "OnSkateboardDismount", "OnSkateboardMount",
                    "OnTogglePauseMenu", "OnUINavigationDirection",
                    "OnUICyclePanelDirection", "OnUITabNavigationPrimary",
                    "OnUITabNavigationSecondary", "OnUITabNavigationTertiary",
                    "OnUIScrollbar", "OnUIMapNavigationDirection", "OnUIMapZoom",
                    "OnUIModifyAmountIncrementTierOne", "OnUIModifyAmountIncrementTierTwo",
                    "OnUIModifyAmountIncrementTierThree"
                };

                foreach (string callback in blockedCallbacks)
                {
                    PatchInputMethod(
                        typeof(GameInput),
                        callback,
                        nameof(GameInputCallbackPrefix));
                }

                _inputBlockPatchesApplied = true;
            }
            catch (System.Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Failed to apply input block patches: " + ex.Message);
                try { _inputBlockHarmony?.UnpatchSelf(); } catch { }
                _inputBlockHarmony = null;
                _inputBlockPatchesApplied = false;
            }
        }

        private static void PatchInputMethod(
            System.Type targetType,
            string targetName,
            string prefixName)
        {
            try
            {
                MethodInfo target = targetType.GetMethod(
                    targetName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo prefix = typeof(GameplayStateGateService).GetMethod(
                    prefixName,
                    BindingFlags.Static | BindingFlags.NonPublic);

                if (target != null && prefix != null)
                    _inputBlockHarmony.Patch(target, prefix: new HarmonyMethod(prefix));
            }
            catch (System.NotSupportedException ex)
            {
                DebugLogService.Instance.Verbose(
                    targetType.Name + "." + targetName +
                    " patch skipped (method stripped): " + ex.Message);
            }
            catch (System.Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    targetType.Name + "." + targetName +
                    " patch failed: " + ex.Message);
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

        private static bool PauseMenuPrefix()
        {
            if (!Instance.MenuOpen)
                return true;

            NotificationService.Instance.Status("Close Nugzz before pausing");
            return false;
        }

        private static bool GameInputCallbackPrefix()
        {
            return !Instance.MenuOpen;
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
