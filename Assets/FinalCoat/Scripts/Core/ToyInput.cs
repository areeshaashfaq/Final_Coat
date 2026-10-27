using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

#endif
namespace FinalCoat{
    public enum ControlScheme { WASD, Arrows }
    public enum GameKey { Left, Right, Jump, Tug, Hold }
    public static class ToyInput{
        enum RawKey { A, D, W, S, LeftShift, LeftArrow, RightArrow, UpArrow, DownArrow, RightShift, Escape, R, F1 }
        public static float Horizontal(ControlScheme scheme){
            float x = 0f;
            if (Held(scheme, GameKey.Left)) x -= 1f;
            if (Held(scheme, GameKey.Right)) x += 1f;
            return x;
        }

        public static bool Held(ControlScheme scheme, GameKey key) => KeyHeld(Map(scheme, key));
        public static bool Pressed(ControlScheme scheme, GameKey key) => KeyPressed(Map(scheme, key));
        public static bool Released(ControlScheme scheme, GameKey key) => KeyReleased(Map(scheme, key));
        public static bool PausePressed => KeyPressed(RawKey.Escape);
        public static bool RestartPressed => KeyPressed(RawKey.R);
        public static bool HelpPressed => KeyPressed(RawKey.F1);
        static RawKey Map(ControlScheme scheme, GameKey key){
            if (scheme == ControlScheme.WASD){
                switch (key){
                    case GameKey.Left: return RawKey.A;
                    case GameKey.Right: return RawKey.D;
                    case GameKey.Jump: return RawKey.W;
                    case GameKey.Tug: return RawKey.LeftShift;
                    default: return RawKey.S;
                }
            }
            switch (key){
                case GameKey.Left: return RawKey.LeftArrow;
                case GameKey.Right: return RawKey.RightArrow;
                case GameKey.Jump: return RawKey.UpArrow;
                case GameKey.Tug: return RawKey.RightShift;
                default: return RawKey.DownArrow;
            }
        }

#if ENABLE_INPUT_SYSTEM
        static Key ToKey(RawKey key){
            switch (key){
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
                default: return Key.F1;
            }
        }

        static bool KeyHeld(RawKey key){
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[ToKey(key)].isPressed;
        }

        static bool KeyPressed(RawKey key){
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[ToKey(key)].wasPressedThisFrame;
        }

        static bool KeyReleased(RawKey key){
            var keyboard = Keyboard.current;
            return keyboard != null && keyboard[ToKey(key)].wasReleasedThisFrame;
        }

#else
        static KeyCode ToKeyCode(RawKey key){
            switch (key){
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
                default: return KeyCode.F1;
            }
        }

        static bool KeyHeld(RawKey key) => Input.GetKey(ToKeyCode(key));
        static bool KeyPressed(RawKey key) => Input.GetKeyDown(ToKeyCode(key));
        static bool KeyReleased(RawKey key) => Input.GetKeyUp(ToKeyCode(key));
#endif
    }
}
