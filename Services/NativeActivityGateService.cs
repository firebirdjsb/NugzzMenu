using HarmonyLib;
using Il2CppScheduleOne.Casino.UI;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.TV;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Tracks native activities that exclusively own player input.
    /// </summary>
    public sealed class NativeActivityGateService
    {
        private static readonly NativeActivityGateService _instance =
            new NativeActivityGateService();

        private TVInterface _tv;
        private JukeboxInterface _jukebox;
        private BlackjackInterface _blackjack;
        private RTBInterface _rtb;
        private CharacterInterface _characterInterface;
        private CharacterDisplay _characterDisplay;
        private Phone _phone;
        private GameplayMenu _gameplayMenu;
        private int _lastEvaluationFrame = -1;
        private bool _lastBlocked;
        private string _lastReason;

        public static NativeActivityGateService Instance => _instance;

        private NativeActivityGateService() { }

        public bool TryGetBlockReason(out string reason)
        {
            int frame = UnityEngine.Time.frameCount;
            if (_lastEvaluationFrame == frame)
            {
                reason = _lastReason;
                return _lastBlocked;
            }

            _lastEvaluationFrame = frame;
            _lastBlocked = EvaluateBlockReason(true, out _lastReason);
            reason = _lastReason;
            return _lastBlocked;
        }

        public bool IsPhoneCharacterScreenOpen
        {
            get
            {
                try
                {
                    GameplayMenu screen = _gameplayMenu ?? GameplayMenu.Instance;
                    return screen != null && screen.IsOpen;
                }
                catch { return false; }
            }
        }

        // The mod may overlay either native tab. World actions still use the
        // stricter gate, so clicks in either menu cannot water/harvest or move.
        public bool TryGetModBlockReason(out string reason) =>
            EvaluateBlockReason(false, out reason);

        private bool EvaluateBlockReason(bool includePhoneCharacter, out string reason)
        {
            if (IsOpen(ref _tv))
            {
                reason = "using the TV";
                return true;
            }

            if (IsOpen(ref _jukebox))
            {
                reason = "using the jukebox";
                return true;
            }

            if (IsOpen(ref _blackjack))
            {
                reason = "playing blackjack";
                return true;
            }

            if (IsOpen(ref _rtb))
            {
                reason = "playing a casino game";
                return true;
            }

            if (includePhoneCharacter)
            {
                if (IsPhoneCharacterScreenOpen)
                {
                    reason = "using the phone or character screen";
                    return true;
                }

                // The parent owns input/blur in 0.4.7. Child visibility can lag
                // during its animation and must not keep the menu locked out.
                bool hasParent;
                try { hasParent = (_gameplayMenu ?? GameplayMenu.Instance) != null; }
                catch { hasParent = false; }
                if (!hasParent && (IsOpen(ref _characterInterface) || IsOpen(ref _characterDisplay) || IsOpen(ref _phone)))
                {
                    reason = "using a phone or character interface";
                    return true;
                }
            }

            reason = null;
            return false;
        }

        internal void SetTV(TVInterface value) { _tv = value; Invalidate(); }
        internal void SetJukebox(JukeboxInterface value) { _jukebox = value; Invalidate(); }
        internal void SetBlackjack(BlackjackInterface value) { _blackjack = value; Invalidate(); }
        internal void SetRTB(RTBInterface value) { _rtb = value; Invalidate(); }
        internal void SetCharacterInterface(CharacterInterface value) =>
            SetCharacterInterfaceValue(value);
        internal void SetCharacterDisplay(CharacterDisplay value) =>
            SetCharacterDisplayValue(value);
        internal void SetPhoneState(Phone value) { _phone = value; Invalidate(); }
        internal void SetGameplayMenu(GameplayMenu value) { _gameplayMenu = value; Invalidate(); }

        private void SetCharacterInterfaceValue(CharacterInterface value)
        {
            _characterInterface = value;
            Invalidate();
        }

        private void SetCharacterDisplayValue(CharacterDisplay value)
        {
            _characterDisplay = value;
            Invalidate();
        }

        internal void ClearTV(TVInterface value)
        {
            if (_tv == value)
            {
                _tv = null;
                Invalidate();
            }
        }

        internal void ClearJukebox(JukeboxInterface value)
        {
            if (_jukebox == value)
            {
                _jukebox = null;
                Invalidate();
            }
        }

        internal void ClearBlackjack(BlackjackInterface value)
        {
            if (_blackjack == value)
            {
                _blackjack = null;
                Invalidate();
            }
        }

        internal void ClearRTB(RTBInterface value)
        {
            if (_rtb == value)
            {
                _rtb = null;
                Invalidate();
            }
        }

        internal void ClearCharacterInterface(CharacterInterface value)
        {
            if (_characterInterface == value)
            {
                _characterInterface = null;
                Invalidate();
            }
        }

        public void ResetForScene()
        {
            _tv = null; _jukebox = null; _blackjack = null; _rtb = null;
            _characterInterface = null; _characterDisplay = null; _phone = null; _gameplayMenu = null;
            Invalidate();
        }

        private void Invalidate()
        {
            _lastEvaluationFrame = -1;
        }

        private static bool IsOpen(ref TVInterface value)
        {
            try { return value != null && value.IsOpen; }
            catch { value = null; return false; }
        }

        private static bool IsOpen(ref JukeboxInterface value)
        {
            try { return value != null && value.IsOpen; }
            catch { value = null; return false; }
        }

        private static bool IsOpen(ref BlackjackInterface value)
        {
            try { return value != null && value.CurrentGame != null; }
            catch { value = null; return false; }
        }

        private static bool IsOpen(ref RTBInterface value)
        {
            try { return value != null && value.CurrentGame != null; }
            catch { value = null; return false; }
        }

        private static bool IsOpen(ref CharacterInterface value)
        {
            try { return value != null && value.IsOpen; }
            catch { value = null; return false; }
        }

        private static bool IsOpen(ref CharacterDisplay value)
        {
            try { return value != null && value.IsOpen; }
            catch { value = null; return false; }
        }

        private static bool IsOpen(ref Phone value)
        {
            try { return value != null && value.IsOpen; }
            catch { value = null; return false; }
        }
    }

    [HarmonyPatch(typeof(TVInterface), nameof(TVInterface.Open))]
    internal static class NugzzTVOpenPatch
    {
        private static void Postfix(TVInterface __instance) =>
            NativeActivityGateService.Instance.SetTV(__instance);
    }

    [HarmonyPatch(typeof(TVInterface), nameof(TVInterface.Close))]
    internal static class NugzzTVClosePatch
    {
        private static void Postfix(TVInterface __instance) =>
            NativeActivityGateService.Instance.ClearTV(__instance);
    }

    [HarmonyPatch(typeof(JukeboxInterface), nameof(JukeboxInterface.Open))]
    internal static class NugzzJukeboxOpenPatch
    {
        private static void Postfix(JukeboxInterface __instance) =>
            NativeActivityGateService.Instance.SetJukebox(__instance);
    }

    [HarmonyPatch(typeof(JukeboxInterface), nameof(JukeboxInterface.Close))]
    internal static class NugzzJukeboxClosePatch
    {
        private static void Postfix(JukeboxInterface __instance) =>
            NativeActivityGateService.Instance.ClearJukebox(__instance);
    }

    [HarmonyPatch(typeof(BlackjackInterface), nameof(BlackjackInterface.Open))]
    internal static class NugzzBlackjackOpenPatch
    {
        private static void Postfix(BlackjackInterface __instance) =>
            NativeActivityGateService.Instance.SetBlackjack(__instance);
    }

    [HarmonyPatch(typeof(BlackjackInterface), nameof(BlackjackInterface.Close))]
    internal static class NugzzBlackjackClosePatch
    {
        private static void Postfix(BlackjackInterface __instance) =>
            NativeActivityGateService.Instance.ClearBlackjack(__instance);
    }

    [HarmonyPatch(typeof(RTBInterface), nameof(RTBInterface.Open))]
    internal static class NugzzRTBOpenPatch
    {
        private static void Postfix(RTBInterface __instance) =>
            NativeActivityGateService.Instance.SetRTB(__instance);
    }

    [HarmonyPatch(typeof(RTBInterface), nameof(RTBInterface.Close))]
    internal static class NugzzRTBClosePatch
    {
        private static void Postfix(RTBInterface __instance) =>
            NativeActivityGateService.Instance.ClearRTB(__instance);
    }

    [HarmonyPatch(typeof(Phone), nameof(Phone.SetIsOpen))]
    internal static class NugzzPhoneActivityPatch
    {
        private static bool Prefix(bool o)
        {
            return !o || !GameplayStateGateService.Instance.MenuOpen;
        }

        private static void Postfix(Phone __instance, bool o)
        {
            NativeActivityGateService.Instance.SetPhoneState(__instance);
        }
    }

    [HarmonyPatch(typeof(GameplayMenu), "Update")]
    internal static class NugzzGameplayMenuInputPatch
    {
        // 0.4.7 polls InputActionReference directly, bypassing GameInput callbacks.
        // This Update handles input only; native state/animation cleanup continues.
        private static bool Prefix() => !GameplayStateGateService.Instance.MenuOpen;
    }

    [HarmonyPatch(typeof(GameplayMenu), nameof(GameplayMenu.Open))]
    internal static class NugzzGameplayMenuOpenPatch
    {
        private static bool Prefix() => !GameplayStateGateService.Instance.MenuOpen;
    }

    [HarmonyPatch(typeof(GameplayMenu), nameof(GameplayMenu.SetScreen))]
    internal static class NugzzGameplayMenuTabPatch
    {
        private static bool Prefix() => !GameplayStateGateService.Instance.MenuOpen;
    }

    [HarmonyPatch(typeof(GameplayMenu), "OnClose")]
    internal static class NugzzGameplayMenuCloseStatePatch
    {
        // Let the native parent/child stack restore HUD, blur and clothing slots.
        // In particular, Phone.SetIsOpen(false) is also a normal tab transition.
        private static void Postfix(GameplayMenu __instance) =>
            NativeActivityGateService.Instance.SetGameplayMenu(__instance);
    }

    [HarmonyPatch(typeof(GameplayMenu), "OnOpen")]
    internal static class NugzzGameplayMenuOpenStatePatch
    {
        private static void Postfix(GameplayMenu __instance) =>
            NativeActivityGateService.Instance.SetGameplayMenu(__instance);
    }

    [HarmonyPatch(typeof(CharacterInterface), nameof(CharacterInterface.Open))]
    internal static class NugzzCharacterInterfaceOpenPatch
    {
        private static void Postfix(CharacterInterface __instance) =>
            NativeActivityGateService.Instance.SetCharacterInterface(__instance);
    }

    [HarmonyPatch(typeof(CharacterInterface), nameof(CharacterInterface.Close))]
    internal static class NugzzCharacterInterfaceClosePatch
    {
        private static void Postfix(CharacterInterface __instance) =>
            NativeActivityGateService.Instance.ClearCharacterInterface(__instance);
    }

    [HarmonyPatch(typeof(CharacterInterface), nameof(CharacterInterface.SetIsActiveGameplayScreen))]
    internal static class NugzzCharacterInterfaceStatePatch
    {
        private static void Postfix(CharacterInterface __instance, bool isActive) =>
            NativeActivityGateService.Instance.SetCharacterInterface(__instance);
    }

    [HarmonyPatch(typeof(CharacterDisplay), nameof(CharacterDisplay.SetOpen))]
    internal static class NugzzCharacterDisplayActivityPatch
    {
        private static void Postfix(CharacterDisplay __instance, bool open) =>
            NativeActivityGateService.Instance.SetCharacterDisplay(__instance);
    }
}
