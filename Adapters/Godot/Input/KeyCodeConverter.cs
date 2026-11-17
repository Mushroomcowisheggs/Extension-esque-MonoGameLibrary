using Godot;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Godot.Input {
    /// <summary>
    /// Converts between platform‑independent <see cref="KeyCode"/> and Godot <see cref="Key"/>.
    /// </summary>
    public static class KeyCodeConverter {
        /// <summary>
        /// Converts a platform-independent key code to the Godot key it corresponds to.
        /// Godot collapses the left and right variants of the modifier keys into the
        /// single <c>Shift</c>, <c>Ctrl</c>, <c>Alt</c> and <c>Meta</c> values, so both
        /// sides of a modifier map to the same native key.
        /// </summary>
        /// <param name="codeKey">The platform-independent key code.</param>
        /// <returns>The matching Godot key, or <c>Key.None</c> when there is no match.</returns>
        public static Key ToGodotKey(KeyCode codeKey) {
            switch (codeKey) {
                case KeyCode.None: return Key.None;
                case KeyCode.A: return Key.A;
                case KeyCode.B: return Key.B;
                case KeyCode.C: return Key.C;
                case KeyCode.D: return Key.D;
                case KeyCode.E: return Key.E;
                case KeyCode.F: return Key.F;
                case KeyCode.G: return Key.G;
                case KeyCode.H: return Key.H;
                case KeyCode.I: return Key.I;
                case KeyCode.J: return Key.J;
                case KeyCode.K: return Key.K;
                case KeyCode.L: return Key.L;
                case KeyCode.M: return Key.M;
                case KeyCode.N: return Key.N;
                case KeyCode.O: return Key.O;
                case KeyCode.P: return Key.P;
                case KeyCode.Q: return Key.Q;
                case KeyCode.R: return Key.R;
                case KeyCode.S: return Key.S;
                case KeyCode.T: return Key.T;
                case KeyCode.U: return Key.U;
                case KeyCode.V: return Key.V;
                case KeyCode.W: return Key.W;
                case KeyCode.X: return Key.X;
                case KeyCode.Y: return Key.Y;
                case KeyCode.Z: return Key.Z;
                case KeyCode.Space: return Key.Space;
                case KeyCode.Enter: return Key.Enter;
                case KeyCode.Escape: return Key.Escape;
                case KeyCode.Tab: return Key.Tab;
                case KeyCode.Backspace: return Key.Backspace;
                case KeyCode.Up: return Key.Up;
                case KeyCode.Down: return Key.Down;
                case KeyCode.Left: return Key.Left;
                case KeyCode.Right: return Key.Right;
                case KeyCode.F1: return Key.F1;
                case KeyCode.F2: return Key.F2;
                case KeyCode.F3: return Key.F3;
                case KeyCode.F4: return Key.F4;
                case KeyCode.F5: return Key.F5;
                case KeyCode.F6: return Key.F6;
                case KeyCode.F7: return Key.F7;
                case KeyCode.F8: return Key.F8;
                case KeyCode.F9: return Key.F9;
                case KeyCode.F10: return Key.F10;
                case KeyCode.F11: return Key.F11;
                case KeyCode.F12: return Key.F12;
                case KeyCode.LeftShift: return Key.Shift;
                case KeyCode.RightShift: return Key.Shift;
                case KeyCode.LeftControl: return Key.Ctrl;
                case KeyCode.RightControl: return Key.Ctrl;
                case KeyCode.LeftAlt: return Key.Alt;
                case KeyCode.RightAlt: return Key.Alt;
                case KeyCode.LeftSystem: return Key.Meta;
                case KeyCode.RightSystem: return Key.Meta;
                case KeyCode.CapsLock: return Key.Capslock;
                case KeyCode.Number0: return Key.Key0;
                case KeyCode.Number1: return Key.Key1;
                case KeyCode.Number2: return Key.Key2;
                case KeyCode.Number3: return Key.Key3;
                case KeyCode.Number4: return Key.Key4;
                case KeyCode.Number5: return Key.Key5;
                case KeyCode.Number6: return Key.Key6;
                case KeyCode.Number7: return Key.Key7;
                case KeyCode.Number8: return Key.Key8;
                case KeyCode.Number9: return Key.Key9;
                case KeyCode.Insert: return Key.Insert;
                case KeyCode.Delete: return Key.Delete;
                case KeyCode.Home: return Key.Home;
                case KeyCode.End: return Key.End;
                case KeyCode.PageUp: return Key.Pageup;
                case KeyCode.PageDown: return Key.Pagedown;
                case KeyCode.Minus: return Key.Minus;
                case KeyCode.Equals: return Key.Equal;
                case KeyCode.LeftBracket: return Key.Bracketleft;
                case KeyCode.RightBracket: return Key.Bracketright;
                case KeyCode.Backslash: return Key.Backslash;
                case KeyCode.Semicolon: return Key.Semicolon;
                case KeyCode.Apostrophe: return Key.Apostrophe;
                case KeyCode.Grave: return Key.Quoteleft;
                case KeyCode.Comma: return Key.Comma;
                case KeyCode.Period: return Key.Period;
                case KeyCode.Slash: return Key.Slash;
                default: return Key.None;
            }
        }
        
        /// <summary>
        /// Converts a Godot key to the platform-independent key code it corresponds to.
        /// The collapsed modifier keys report the left variant, so <c>Key.Shift</c>
        /// maps to <see cref="KeyCode.LeftShift"/> and its right counterpart is not
        /// reachable through this direction.
        /// </summary>
        /// <param name="keyNative">The Godot key.</param>
        /// <returns>The matching key code, or <see cref="KeyCode.None"/> when there is no match.</returns>
        public static KeyCode ToKeyCode(Key keyNative) {
            switch (keyNative) {
                case Key.None: return KeyCode.None;
                case Key.A: return KeyCode.A;
                case Key.B: return KeyCode.B;
                case Key.C: return KeyCode.C;
                case Key.D: return KeyCode.D;
                case Key.E: return KeyCode.E;
                case Key.F: return KeyCode.F;
                case Key.G: return KeyCode.G;
                case Key.H: return KeyCode.H;
                case Key.I: return KeyCode.I;
                case Key.J: return KeyCode.J;
                case Key.K: return KeyCode.K;
                case Key.L: return KeyCode.L;
                case Key.M: return KeyCode.M;
                case Key.N: return KeyCode.N;
                case Key.O: return KeyCode.O;
                case Key.P: return KeyCode.P;
                case Key.Q: return KeyCode.Q;
                case Key.R: return KeyCode.R;
                case Key.S: return KeyCode.S;
                case Key.T: return KeyCode.T;
                case Key.U: return KeyCode.U;
                case Key.V: return KeyCode.V;
                case Key.W: return KeyCode.W;
                case Key.X: return KeyCode.X;
                case Key.Y: return KeyCode.Y;
                case Key.Z: return KeyCode.Z;
                case Key.Space: return KeyCode.Space;
                case Key.Enter: return KeyCode.Enter;
                case Key.Escape: return KeyCode.Escape;
                case Key.Tab: return KeyCode.Tab;
                case Key.Backspace: return KeyCode.Backspace;
                case Key.Up: return KeyCode.Up;
                case Key.Down: return KeyCode.Down;
                case Key.Left: return KeyCode.Left;
                case Key.Right: return KeyCode.Right;
                case Key.F1: return KeyCode.F1;
                case Key.F2: return KeyCode.F2;
                case Key.F3: return KeyCode.F3;
                case Key.F4: return KeyCode.F4;
                case Key.F5: return KeyCode.F5;
                case Key.F6: return KeyCode.F6;
                case Key.F7: return KeyCode.F7;
                case Key.F8: return KeyCode.F8;
                case Key.F9: return KeyCode.F9;
                case Key.F10: return KeyCode.F10;
                case Key.F11: return KeyCode.F11;
                case Key.F12: return KeyCode.F12;
                case Key.Shift: return KeyCode.LeftShift;
                case Key.Ctrl: return KeyCode.LeftControl;
                case Key.Alt: return KeyCode.LeftAlt;
                case Key.Meta: return KeyCode.LeftSystem;
                case Key.Capslock: return KeyCode.CapsLock;
                case Key.Key0: return KeyCode.Number0;
                case Key.Key1: return KeyCode.Number1;
                case Key.Key2: return KeyCode.Number2;
                case Key.Key3: return KeyCode.Number3;
                case Key.Key4: return KeyCode.Number4;
                case Key.Key5: return KeyCode.Number5;
                case Key.Key6: return KeyCode.Number6;
                case Key.Key7: return KeyCode.Number7;
                case Key.Key8: return KeyCode.Number8;
                case Key.Key9: return KeyCode.Number9;
                case Key.Insert: return KeyCode.Insert;
                case Key.Delete: return KeyCode.Delete;
                case Key.Home: return KeyCode.Home;
                case Key.End: return KeyCode.End;
                case Key.Pageup: return KeyCode.PageUp;
                case Key.Pagedown: return KeyCode.PageDown;
                case Key.Minus: return KeyCode.Minus;
                case Key.Equal: return KeyCode.Equals;
                case Key.Bracketleft: return KeyCode.LeftBracket;
                case Key.Bracketright: return KeyCode.RightBracket;
                case Key.Backslash: return KeyCode.Backslash;
                case Key.Semicolon: return KeyCode.Semicolon;
                case Key.Apostrophe: return KeyCode.Apostrophe;
                case Key.Quoteleft: return KeyCode.Grave;
                case Key.Comma: return KeyCode.Comma;
                case Key.Period: return KeyCode.Period;
                case Key.Slash: return KeyCode.Slash;
                default: return KeyCode.None;
            }
        }
    }
}
