using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NugzzMenu.Services
{
    public sealed class ControllerInputService
    {
        private const float AxisThreshold = 0.62f;
        private const float InitialRepeatDelay = 0.34f;
        private const float RepeatDelay = 0.11f;

        private static readonly ControllerInputService _instance = new ControllerInputService();

        private bool _playStationLayout;
        private bool _controllerActive;
        private bool _controllerConnected;
        private bool _legacyControllerConnected;
        private bool _menuOpen;
        private bool _controllerOwnsMenu;
        private bool _menuComboHeldLastFrame;
        private bool _thirdPersonComboHeldLastFrame;
        private bool _menuToggle;
        private bool _submit;
        private bool _cancel;
        private bool _reset;
        private bool _help;
        private bool _previousTab;
        private bool _nextTab;
        private bool _thirdPersonToggle;
        private int _verticalMove;
        private int _horizontalMove;
        private int _lastVerticalDirection;
        private int _lastHorizontalDirection;
        private float _nextVerticalRepeat;
        private float _nextHorizontalRepeat;
        private float _nextDeviceScan;
        private int _currentGamepadDeviceId = -1;
        private bool _gameplayJumpPressed;
        private bool _gameplayAscend;
        private bool _gameplayDescend;
        private Vector2 _gameplayMove;
        private Vector2 _gameplayLook;

        public static ControllerInputService Instance => _instance;

        public bool ControllerActive => _menuOpen ? _controllerOwnsMenu : _controllerActive;
        public bool ControllerConnected => _controllerConnected;
        public bool IsPlayStation => _playStationLayout;
        public string ConfirmPrompt => _playStationLayout ? "X" : "A";
        public string CancelPrompt => _playStationLayout ? "O" : "B";
        public string ResetPrompt => _playStationLayout ? "Square" : "X";
        public string HelpPrompt => _playStationLayout ? "Triangle" : "Y";
        public string LeftShoulderPrompt => _playStationLayout ? "L1" : "LB";
        public string RightShoulderPrompt => _playStationLayout ? "R1" : "RB";
        public string ConfirmGlyph => _playStationLayout ? "\u00d7" : "A";
        public string CancelGlyph => _playStationLayout ? "\u25cb" : "B";
        public string ResetGlyph => _playStationLayout ? "\u25a1" : "X";
        public string HelpGlyph => _playStationLayout ? "\u25b3" : "Y";
        public string DPadGlyph => "\u271a";
        public string LeftStickGlyph => "\u25c9";
        public Vector2 GameplayMove => _gameplayMove;
        public Vector2 GameplayLook => _gameplayLook;
        public bool GameplayAscend => _gameplayAscend;
        public bool GameplayDescend => _gameplayDescend;

        private ControllerInputService()
        {
        }

        public void Update()
        {
            _menuToggle = false;
            _submit = false;
            _cancel = false;
            _reset = false;
            _help = false;
            _previousTab = false;
            _nextTab = false;
            _thirdPersonToggle = false;
            _verticalMove = 0;
            _horizontalMove = 0;
            _gameplayJumpPressed = false;
            _gameplayAscend = false;
            _gameplayDescend = false;
            _gameplayMove = Vector2.zero;
            _gameplayLook = Vector2.zero;

            bool controllerActivity;
            Gamepad gamepad = GetCurrentGamepad();
            ScanLegacyDevices(gamepad == null);
            _controllerConnected = gamepad != null || _legacyControllerConnected;
            if (!_controllerConnected)
            {
                _controllerActive = false;
                _controllerOwnsMenu = false;
            }
            if (gamepad != null)
            {
                if (gamepad.deviceId != _currentGamepadDeviceId)
                {
                    _currentGamepadDeviceId = gamepad.deviceId;
                    UpdateControllerLayout(gamepad);
                }
                controllerActivity = ReadInputSystemGamepad(gamepad);
            }
            else
            {
                _currentGamepadDeviceId = -1;
                controllerActivity = ReadLegacyController();
            }

            if (controllerActivity)
            {
                _controllerActive = true;
                if (_menuOpen)
                    _controllerOwnsMenu = true;
            }
            else if (!_controllerOwnsMenu && HasKeyboardOrMouseActivity())
                _controllerActive = false;
        }

        public void SetMenuOpen(bool open, bool openedWithController)
        {
            _menuOpen = open;
            if (!open)
            {
                _controllerOwnsMenu = false;
                return;
            }

            // Opening with F8 must not inherit stale gamepad activity from gameplay.
            // Once a controller takes ownership during this menu session, mouse motion
            // cannot steal the selection highlight until the menu closes.
            _controllerActive = openedWithController;
            _controllerOwnsMenu = openedWithController;
        }

        public bool ConsumeMenuToggle() => Consume(ref _menuToggle);
        public bool ConsumeSubmit() => Consume(ref _submit);
        public bool ConsumeCancel() => Consume(ref _cancel);
        public bool ConsumeReset() => Consume(ref _reset);
        public bool ConsumeHelp() => Consume(ref _help);
        public bool ConsumePreviousTab() => Consume(ref _previousTab);
        public bool ConsumeNextTab() => Consume(ref _nextTab);
        public bool ConsumeThirdPersonToggle() => Consume(ref _thirdPersonToggle);
        public bool ConsumeGameplayJump() => Consume(ref _gameplayJumpPressed);

        public int ConsumeVerticalMove()
        {
            int value = _verticalMove;
            _verticalMove = 0;
            return value;
        }

        public int ConsumeHorizontalMove()
        {
            int value = _horizontalMove;
            _horizontalMove = 0;
            return value;
        }

        private bool ReadInputSystemGamepad(Gamepad gamepad)
        {
            try
            {
                bool leftShoulder = gamepad.leftShoulder.isPressed;
                bool rightShoulder = gamepad.rightShoulder.isPressed;
                bool menuComboHeld = leftShoulder && rightShoulder && gamepad.dpad.up.isPressed;
                bool thirdPersonComboHeld = leftShoulder && rightShoulder && gamepad.dpad.down.isPressed;
                if (menuComboHeld && !_menuComboHeldLastFrame)
                    _menuToggle = true;
                if (thirdPersonComboHeld && !_thirdPersonComboHeldLastFrame)
                    _thirdPersonToggle = true;
                _menuComboHeldLastFrame = menuComboHeld;
                _thirdPersonComboHeldLastFrame = thirdPersonComboHeld;

                if (!menuComboHeld && !thirdPersonComboHeld)
                {
                    _previousTab = gamepad.leftShoulder.wasPressedThisFrame && !rightShoulder;
                    _nextTab = gamepad.rightShoulder.wasPressedThisFrame && !leftShoulder;
                }

                _submit = gamepad.buttonSouth.wasPressedThisFrame;
                _cancel = gamepad.buttonEast.wasPressedThisFrame;
                _reset = gamepad.buttonWest.wasPressedThisFrame;
                _help = gamepad.buttonNorth.wasPressedThisFrame;
                Vector2 stick = gamepad.leftStick.ReadValue();
                Vector2 look = gamepad.rightStick.ReadValue();
                _gameplayJumpPressed = gamepad.buttonSouth.wasPressedThisFrame;
                _gameplayAscend = gamepad.buttonSouth.isPressed;
                _gameplayDescend = gamepad.buttonEast.isPressed;
                _gameplayMove = stick;
                _gameplayLook = look;
                int vertical = gamepad.dpad.up.isPressed ? 1 :
                    gamepad.dpad.down.isPressed ? -1 : AxisDirection(stick.y);
                int horizontal = gamepad.dpad.right.isPressed ? 1 :
                    gamepad.dpad.left.isPressed ? -1 : AxisDirection(stick.x);
                ApplyRepeatedMovement(menuComboHeld || thirdPersonComboHeld ? 0 : vertical, horizontal);

                return menuComboHeld || thirdPersonComboHeld || _menuToggle || _submit || _cancel ||
                       _reset || _help ||
                       _thirdPersonToggle ||
                       _previousTab || _nextTab || _verticalMove != 0 ||
                       _horizontalMove != 0 || stick.sqrMagnitude >= 0.2f ||
                       look.sqrMagnitude >= 0.2f;
            }
            catch
            {
                return ReadLegacyController();
            }
        }

        private bool ReadLegacyController()
        {
            if (!_legacyControllerConnected)
                return false;

            bool leftShoulder = GetButton(4);
            bool rightShoulder = GetButton(5);
            bool shouldersHeld = leftShoulder && rightShoulder;
            int vertical = ReadAxisDirection("Vertical");
            int horizontal = ReadAxisDirection("Horizontal");
            bool dpadUp = GetButton(12) || vertical > 0;
            bool dpadDown = GetButton(13) || vertical < 0;
            bool menuComboHeld = shouldersHeld && dpadUp;
            bool thirdPersonComboHeld = shouldersHeld && dpadDown;

            if (menuComboHeld && !_menuComboHeldLastFrame)
                _menuToggle = true;
            if (thirdPersonComboHeld && !_thirdPersonComboHeldLastFrame)
                _thirdPersonToggle = true;
            _menuComboHeldLastFrame = menuComboHeld;
            _thirdPersonComboHeldLastFrame = thirdPersonComboHeld;

            if (!rightShoulder && GetButtonDown(4))
                _previousTab = true;
            if (!leftShoulder && GetButtonDown(5))
                _nextTab = true;

            int confirmButton = _playStationLayout ? 1 : 0;
            int cancelButton = _playStationLayout ? 2 : 1;
            int resetButton = _playStationLayout ? 0 : 2;
            int helpButton = 3;

            _submit = GetButtonDown(confirmButton);
            _cancel = GetButtonDown(cancelButton);
            _reset = GetButtonDown(resetButton);
            _help = GetButtonDown(helpButton);
            _gameplayJumpPressed = GetButtonDown(confirmButton);
            _gameplayAscend = GetButton(confirmButton);
            _gameplayDescend = GetButton(cancelButton);
            _gameplayMove = new Vector2(ReadAxis("Horizontal"), ReadAxis("Vertical"));

            ApplyRepeatedMovement(menuComboHeld || thirdPersonComboHeld ? 0 : vertical, horizontal);
            return _menuToggle || _submit || _cancel || _reset || _help || _thirdPersonToggle ||
                   _previousTab ||
                   _nextTab || _verticalMove != 0 || _horizontalMove != 0 ||
                   AnyControllerButtonDown();
        }

        private void ScanLegacyDevices(bool scanLegacy)
        {
            if (Time.realtimeSinceStartup < _nextDeviceScan)
                return;

            _nextDeviceScan = Time.realtimeSinceStartup + 2f;
            _legacyControllerConnected = false;
            if (!scanLegacy)
                return;

            _playStationLayout = false;
            string[] names = Input.GetJoystickNames();
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i] ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name))
                    _legacyControllerConnected = true;
                if (name.IndexOf("PlayStation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("DualShock", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Wireless Controller", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _playStationLayout = true;
                    break;
                }
            }
        }

        private void UpdateControllerLayout(Gamepad gamepad)
        {
            try
            {
                string identity = (gamepad.name ?? string.Empty) + " " +
                                  (gamepad.displayName ?? string.Empty) + " " +
                                  (gamepad.layout ?? string.Empty) + " " +
                                  (gamepad.description.product ?? string.Empty) + " " +
                                  (gamepad.description.manufacturer ?? string.Empty);
                _playStationLayout = IsPlayStationDevice(identity);
            }
            catch
            {
                // Keep the legacy joystick-name result when device metadata is unavailable.
            }
        }

        private static bool IsPlayStationDevice(string identity)
        {
            return identity.IndexOf("PlayStation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   identity.IndexOf("DualSense", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   identity.IndexOf("DualShock", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   identity.IndexOf("Wireless Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   identity.IndexOf("Sony", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int ReadRepeatedDirection(int direction, ref int lastDirection,
            ref float nextRepeat, float now)
        {
            if (direction == 0)
            {
                lastDirection = 0;
                nextRepeat = 0f;
                return 0;
            }

            if (direction != lastDirection)
            {
                lastDirection = direction;
                nextRepeat = now + InitialRepeatDelay;
                return direction;
            }

            if (now < nextRepeat)
                return 0;

            nextRepeat = now + RepeatDelay;
            return direction;
        }

        private void ApplyRepeatedMovement(int vertical, int horizontal)
        {
            float now = Time.realtimeSinceStartup;
            _verticalMove = ReadRepeatedDirection(vertical, ref _lastVerticalDirection,
                ref _nextVerticalRepeat, now);
            _horizontalMove = ReadRepeatedDirection(horizontal, ref _lastHorizontalDirection,
                ref _nextHorizontalRepeat, now);
        }

        private static int AxisDirection(float value)
        {
            if (value >= AxisThreshold)
                return 1;
            if (value <= -AxisThreshold)
                return -1;
            return 0;
        }

        private static int ReadAxisDirection(string axis)
        {
            float value;
            try
            {
                value = Input.GetAxisRaw(axis);
            }
            catch
            {
                return 0;
            }

            return AxisDirection(value);
        }

        private static float ReadAxis(string axis)
        {
            try { return Input.GetAxisRaw(axis); }
            catch { return 0f; }
        }

        private static bool HasKeyboardOrMouseActivity()
        {
            try
            {
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                    return true;

                Mouse mouse = Mouse.current;
                if (mouse != null &&
                    (mouse.leftButton.wasPressedThisFrame ||
                     mouse.rightButton.wasPressedThisFrame ||
                     mouse.middleButton.wasPressedThisFrame ||
                     mouse.delta.ReadValue().sqrMagnitude > 1f))
                    return true;
            }
            catch { }

            return false;
        }

        private static Gamepad GetCurrentGamepad()
        {
            try
            {
                return Gamepad.current;
            }
            catch
            {
                return null;
            }
        }

        private static bool GetButton(int index) => Input.GetKey(ButtonKey(index));
        private static bool GetButtonDown(int index) => Input.GetKeyDown(ButtonKey(index));

        private static KeyCode ButtonKey(int index)
        {
            return (KeyCode)((int)KeyCode.JoystickButton0 + index);
        }

        private static bool AnyControllerButtonDown()
        {
            for (int i = 0; i < 16; i++)
            {
                if (GetButtonDown(i))
                    return true;
            }
            return false;
        }

        private static bool Consume(ref bool value)
        {
            if (!value)
                return false;
            value = false;
            return true;
        }
    }
}
