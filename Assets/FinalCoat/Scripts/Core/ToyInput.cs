using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace FinalCoat
{
    /// <summary>The device a player controls their toy with. Chosen in Settings.</summary>
    public enum ControlScheme { WASD, Arrows, Gamepad1, Gamepad2 }

    public enum GameKey { Left, Right, Jump, Tug, Hold }

    /// <summary>
    /// Reads input for one player from the device they picked.
    ///
    /// Keyboard left:  A / D move, W jump, S hold on, Left Shift strand
    /// Keyboard right: Arrows move, Up jump, Down hold on, Right Shift strand
    /// Controller:     Left stick or d-pad move, A jump, B hold on, X strand
    ///
    /// Shared: Esc or Start pause, R or Y restart, M or Select main menu, F1 help.
    /// Controllers need the new Input System, which this project uses.
    /// </summary>
    public static class ToyInput
    {
        public const float StickDeadZone = 0.25f;

        enum RawKey { A, D, W, S, LeftShift, LeftArrow, RightArrow, UpArrow, DownArrow, RightShift, Escape, R, M, F1 }

        // ---------------- Per player ----------------

        public static float Horizontal(ControlScheme scheme)
        {
            if (IsGamepad(scheme)) return PadHorizontal(PadIndex(scheme));

            float x = 0f;
            if (Held(scheme, GameKey.Left)) x -= 1f;
            if (Held(scheme, GameKey.Right)) x += 1f;
            return x;
        }

        public static bool Held(ControlScheme scheme, GameKey key)
        {
            if (IsGamepad(scheme)) return PadHeld(PadIndex(scheme), key);
            return KeyHeld(Map(scheme, key));
        }

        public static bool Pressed(ControlScheme scheme, GameKey key)
        {
            if (IsGamepad(scheme)) return PadPressed(PadIndex(scheme), key);
            return KeyPressed(Map(scheme, key));
        }

        public static bool Released(ControlScheme scheme, GameKey key)
        {
            if (IsGamepad(scheme)) return PadReleased(PadIndex(scheme), key);
            return KeyReleased(Map(scheme, key));
        }

        public static bool IsGamepad(ControlScheme scheme) => scheme == ControlScheme.Gamepad1 || scheme == ControlScheme.Gamepad2;
        static int PadIndex(ControlScheme scheme) => scheme == ControlScheme.Gamepad2 ? 1 : 0;

        // ---------------- Shared buttons ----------------

        public static bool PausePressed => KeyPressed(RawKey.Escape) || AnyPad(PadButton.Start);
        public static bool RestartPressed => KeyPressed(RawKey.R) || AnyPad(PadButton.North);
        public static bool MenuPressed => KeyPressed(RawKey.M) || AnyPad(PadButton.Select);
        public static bool HelpPressed => KeyPressed(RawKey.F1);
        /// <summary>Back out of a menu screen: Esc or B.</summary>
        public static bool BackPressed => KeyPressed(RawKey.Escape) || AnyPad(PadButton.East);

        // ---------------- Controllers ----------------

        enum PadButton { South, East, West, North, Start, Select }

        public static int ConnectedGamepads
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Gamepad.all.Count;
#else
                return 0;
#endif
            }
        }

        public static bool IsConnected(ControlScheme scheme) => !IsGamepad(scheme) || PadIndex(scheme) < ConnectedGamepads;

#if ENABLE_INPUT_SYSTEM
        static Gamepad Pad(int index)
        {
            var all = Gamepad.all;
            return index < all.Count ? all[index] : null;
        }

        static ButtonControl Button(Gamepad pad, PadButton button)
        {
            switch (button)
            {
                case PadButton.South: return pad.buttonSouth;
                case PadButton.East: return pad.buttonEast;
                case PadButton.West: return pad.buttonWest;
                case PadButton.North: return pad.buttonNorth;
                case PadButton.Start: return pad.startButton;
                default: return pad.selectButton;
            }
        }

        static float PadHorizontal(int index)
        {
            var pad = Pad(index);
            if (pad == null) return 0f;
            float x = pad.leftStick.x.ReadValue();
            if (Mathf.Abs(x) < StickDeadZone) x = 0f;
            if (pad.dpad.left.isPressed) x = -1f;
            if (pad.dpad.right.isPressed) x = 1f;
            return Mathf.Clamp(x, -1f, 1f);
        }

        static ButtonControl PadKey(Gamepad pad, GameKey key)
        {
            switch (key)
            {
                case GameKey.Jump: return pad.buttonSouth; // A
                case GameKey.Hold: return pad.buttonEast;  // B
                case GameKey.Tug: return pad.buttonWest;   // X
                case GameKey.Left: return pad.dpad.left;
                default: return pad.dpad.right;
            }
        }

        static bool PadHeld(int index, GameKey key)
        {
            var pad = Pad(index);
            if (pad == null) return false;
            if (key == GameKey.Left) return PadHorizontal(index) < 0f;
            if (key == GameKey.Right) return PadHorizontal(index) > 0f;
            return PadKey(pad, key).isPressed;
        }

        static bool PadPressed(int index, GameKey key)
        {
            var pad = Pad(index);
            return pad != null && PadKey(pad, key).wasPressedThisFrame;
        }

        static bool PadReleased(int index, GameKey key)
        {
            var pad = Pad(index);
            return pad != null && PadKey(pad, key).wasReleasedThisFrame;
        }

        static bool AnyPad(PadButton button)
        {
            foreach (var pad in Gamepad.all)
                if (Button(pad, button).wasPressedThisFrame) return true;
            return false;
        }
#else
        static float PadHorizontal(int index) => 0f;
        static bool PadHeld(int index, GameKey key) => false;
        static bool PadPressed(int index, GameKey key) => false;
        static bool PadReleased(int index, GameKey key) => false;
        static bool AnyPad(PadButton button) => false;
#endif

        // ---------------- Keyboard ----------------

        static RawKey Map(ControlScheme scheme, GameKey key)
        {
            if (scheme == ControlScheme.WASD)
            {
                switch (key)
                {
                    case GameKey.Left: return RawKey.A;
                    case GameKey.Right: return RawKey.D;
                    case GameKey.Jump: return RawKey.W;
                    case GameKey.Tug: return RawKey.LeftShift;
                    default: return RawKey.S;
                }
            }

            switch (key)
            {
                case GameKey.Left: return RawKey.LeftArrow;
                case GameKey.Right: return RawKey.RightArrow;
                case GameKey.Jump: return RawKey.UpArrow;
                case GameKey.Tug: return RawKey.RightShift;
                default: return RawKey.DownArrow;
            }
        }

#if ENABLE_INPUT_SYSTEM
        static Key ToKey(RawKey key)
        {
            switch (key)
            {
                case RawKey.A: return Key.A;
                case RawKey.D: return Key.D;
                case RawKey.W: return Key.W;
                case RawKey.S: return Key.S;
                case RawKey.LeftShift: return Key.LeftShift;
                case RawKey.LeftArrow: return Key.LeftArrow;
                case RawKey.RightArrow: return Key.RightArrow;
                case RawKey.UpArrow: return Key.UpArrow;
                case RawKey.DownArrow: return Key.DownArrow;
                case RawKey.RightShift: return Key.RightShift;
                case RawKey.Escape: return Key.Escape;
                case RawKey.R: return Key.R;
                case RawKey.M: return Key.M;
                default: return Key.F1;
            }
        }

        static bool KeyHeld(RawKey key)
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[ToKey(key)].isPressed;
        }

        static bool KeyPressed(RawKey key)
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[ToKey(key)].wasPressedThisFrame;
        }

        static bool KeyReleased(RawKey key)
        {
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[ToKey(key)].wasReleasedThisFrame;
        }
#else
        static KeyCode ToKeyCode(RawKey key)
        {
            switch (key)
            {
                case RawKey.A: return KeyCode.A;
                case RawKey.D: return KeyCode.D;
                case RawKey.W: return KeyCode.W;
                case RawKey.S: return KeyCode.S;
                case RawKey.LeftShift: return KeyCode.LeftShift;
                case RawKey.LeftArrow: return KeyCode.LeftArrow;
                case RawKey.RightArrow: return KeyCode.RightArrow;
                case RawKey.UpArrow: return KeyCode.UpArrow;
                case RawKey.DownArrow: return KeyCode.DownArrow;
                case RawKey.RightShift: return KeyCode.RightShift;
                case RawKey.Escape: return KeyCode.Escape;
                case RawKey.R: return KeyCode.R;
                case RawKey.M: return KeyCode.M;
                default: return KeyCode.F1;
            }
        }

        static bool KeyHeld(RawKey key) => Input.GetKey(ToKeyCode(key));
        static bool KeyPressed(RawKey key) => Input.GetKeyDown(ToKeyCode(key));
        static bool KeyReleased(RawKey key) => Input.GetKeyUp(ToKeyCode(key));
#endif
    }
}
