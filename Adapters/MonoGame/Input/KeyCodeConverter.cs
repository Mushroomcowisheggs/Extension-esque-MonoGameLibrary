using System;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// Converts between platform‑independent <see cref="KeyCode"/> and MonoGame <see cref="Keys"/>.
    /// </summary>
    public static class KeyCodeConverter {
        /// <summary>
        /// Converts a platform-independent key code to the MonoGame key it corresponds to.
        /// </summary>
        /// <param name="codeKey">The platform-independent key code.</param>
        /// <returns>The matching MonoGame key, or <c>Keys.None</c> when there is no match.</returns>
        public static Keys ToMonoGameKey(KeyCode codeKey) {
            switch (codeKey) {
                case KeyCode.None: return Keys.None;
                case KeyCode.A: return Keys.A;
                case KeyCode.B: return Keys.B;
                case KeyCode.C: return Keys.C;
                case KeyCode.D: return Keys.D;
                case KeyCode.E: return Keys.E;
                case KeyCode.F: return Keys.F;
                case KeyCode.G: return Keys.G;
                case KeyCode.H: return Keys.H;
                case KeyCode.I: return Keys.I;
                case KeyCode.J: return Keys.J;
                case KeyCode.K: return Keys.K;
                case KeyCode.L: return Keys.L;
                case KeyCode.M: return Keys.M;
                case KeyCode.N: return Keys.N;
                case KeyCode.O: return Keys.O;
                case KeyCode.P: return Keys.P;
                case KeyCode.Q: return Keys.Q;
                case KeyCode.R: return Keys.R;
                case KeyCode.S: return Keys.S;
                case KeyCode.T: return Keys.T;
                case KeyCode.U: return Keys.U;
                case KeyCode.V: return Keys.V;
                case KeyCode.W: return Keys.W;
                case KeyCode.X: return Keys.X;
                case KeyCode.Y: return Keys.Y;
                case KeyCode.Z: return Keys.Z;
                case KeyCode.Space: return Keys.Space;
                case KeyCode.Enter: return Keys.Enter;
                case KeyCode.Escape: return Keys.Escape;
                case KeyCode.Tab: return Keys.Tab;
                case KeyCode.Backspace: return Keys.Back;
                case KeyCode.Up: return Keys.Up;
                case KeyCode.Down: return Keys.Down;
                case KeyCode.Left: return Keys.Left;
                case KeyCode.Right: return Keys.Right;
                case KeyCode.F1: return Keys.F1;
                case KeyCode.F2: return Keys.F2;
                case KeyCode.F3: return Keys.F3;
                case KeyCode.F4: return Keys.F4;
                case KeyCode.F5: return Keys.F5;
                case KeyCode.F6: return Keys.F6;
                case KeyCode.F7: return Keys.F7;
                case KeyCode.F8: return Keys.F8;
                case KeyCode.F9: return Keys.F9;
                case KeyCode.F10: return Keys.F10;
                case KeyCode.F11: return Keys.F11;
                case KeyCode.F12: return Keys.F12;
                case KeyCode.LeftShift: return Keys.LeftShift;
                case KeyCode.RightShift: return Keys.RightShift;
                case KeyCode.LeftControl: return Keys.LeftControl;
                case KeyCode.RightControl: return Keys.RightControl;
                case KeyCode.LeftAlt: return Keys.LeftAlt;
                case KeyCode.RightAlt: return Keys.RightAlt;
                case KeyCode.LeftSystem: return Keys.LeftWindows;
                case KeyCode.RightSystem: return Keys.RightWindows;
                case KeyCode.CapsLock: return Keys.CapsLock;
                case KeyCode.Number0: return Keys.D0;
                case KeyCode.Number1: return Keys.D1;
                case KeyCode.Number2: return Keys.D2;
                case KeyCode.Number3: return Keys.D3;
                case KeyCode.Number4: return Keys.D4;
                case KeyCode.Number5: return Keys.D5;
                case KeyCode.Number6: return Keys.D6;
                case KeyCode.Number7: return Keys.D7;
                case KeyCode.Number8: return Keys.D8;
                case KeyCode.Number9: return Keys.D9;
                case KeyCode.Insert: return Keys.Insert;
                case KeyCode.Delete: return Keys.Delete;
                case KeyCode.Home: return Keys.Home;
                case KeyCode.End: return Keys.End;
                case KeyCode.PageUp: return Keys.PageUp;
                case KeyCode.PageDown: return Keys.PageDown;
                case KeyCode.Minus: return Keys.OemMinus;
                case KeyCode.Equals: return Keys.OemPlus;
                case KeyCode.LeftBracket: return Keys.OemOpenBrackets;
                case KeyCode.RightBracket: return Keys.OemCloseBrackets;
                case KeyCode.Backslash: return Keys.OemPipe;
                case KeyCode.Semicolon: return Keys.OemSemicolon;
                case KeyCode.Apostrophe: return Keys.OemQuotes;
                case KeyCode.Grave: return Keys.OemTilde;
                case KeyCode.Comma: return Keys.OemComma;
                case KeyCode.Period: return Keys.OemPeriod;
                case KeyCode.Slash: return Keys.OemQuestion;
                default: return Keys.None;
            }
        }
        
        /// <summary>
        /// Converts a MonoGame key to the platform-independent key code it corresponds to.
        /// </summary>
        /// <param name="keyMonoGame">The MonoGame key.</param>
        /// <returns>The matching key code, or <c>KeyCode.None</c> when there is no match.</returns>
        public static KeyCode ToKeyCode(Keys keyMonoGame) {
            switch (keyMonoGame) {
                case Keys.None: return KeyCode.None;
                case Keys.A: return KeyCode.A;
                case Keys.B: return KeyCode.B;
                case Keys.C: return KeyCode.C;
                case Keys.D: return KeyCode.D;
                case Keys.E: return KeyCode.E;
                case Keys.F: return KeyCode.F;
                case Keys.G: return KeyCode.G;
                case Keys.H: return KeyCode.H;
                case Keys.I: return KeyCode.I;
                case Keys.J: return KeyCode.J;
                case Keys.K: return KeyCode.K;
                case Keys.L: return KeyCode.L;
                case Keys.M: return KeyCode.M;
                case Keys.N: return KeyCode.N;
                case Keys.O: return KeyCode.O;
                case Keys.P: return KeyCode.P;
                case Keys.Q: return KeyCode.Q;
                case Keys.R: return KeyCode.R;
                case Keys.S: return KeyCode.S;
                case Keys.T: return KeyCode.T;
                case Keys.U: return KeyCode.U;
                case Keys.V: return KeyCode.V;
                case Keys.W: return KeyCode.W;
                case Keys.X: return KeyCode.X;
                case Keys.Y: return KeyCode.Y;
                case Keys.Z: return KeyCode.Z;
                case Keys.Space: return KeyCode.Space;
                case Keys.Enter: return KeyCode.Enter;
                case Keys.Escape: return KeyCode.Escape;
                case Keys.Tab: return KeyCode.Tab;
                case Keys.Back: return KeyCode.Backspace;
                case Keys.Up: return KeyCode.Up;
                case Keys.Down: return KeyCode.Down;
                case Keys.Left: return KeyCode.Left;
                case Keys.Right: return KeyCode.Right;
                case Keys.F1: return KeyCode.F1;
                case Keys.F2: return KeyCode.F2;
                case Keys.F3: return KeyCode.F3;
                case Keys.F4: return KeyCode.F4;
                case Keys.F5: return KeyCode.F5;
                case Keys.F6: return KeyCode.F6;
                case Keys.F7: return KeyCode.F7;
                case Keys.F8: return KeyCode.F8;
                case Keys.F9: return KeyCode.F9;
                case Keys.F10: return KeyCode.F10;
                case Keys.F11: return KeyCode.F11;
                case Keys.F12: return KeyCode.F12;
                case Keys.LeftShift: return KeyCode.LeftShift;
                case Keys.RightShift: return KeyCode.RightShift;
                case Keys.LeftControl: return KeyCode.LeftControl;
                case Keys.RightControl: return KeyCode.RightControl;
                case Keys.LeftAlt: return KeyCode.LeftAlt;
                case Keys.RightAlt: return KeyCode.RightAlt;
                case Keys.LeftWindows: return KeyCode.LeftSystem;
                case Keys.RightWindows: return KeyCode.RightSystem;
                case Keys.CapsLock: return KeyCode.CapsLock;
                case Keys.D0: return KeyCode.Number0;
                case Keys.D1: return KeyCode.Number1;
                case Keys.D2: return KeyCode.Number2;
                case Keys.D3: return KeyCode.Number3;
                case Keys.D4: return KeyCode.Number4;
                case Keys.D5: return KeyCode.Number5;
                case Keys.D6: return KeyCode.Number6;
                case Keys.D7: return KeyCode.Number7;
                case Keys.D8: return KeyCode.Number8;
                case Keys.D9: return KeyCode.Number9;
                case Keys.Insert: return KeyCode.Insert;
                case Keys.Delete: return KeyCode.Delete;
                case Keys.Home: return KeyCode.Home;
                case Keys.End: return KeyCode.End;
                case Keys.PageUp: return KeyCode.PageUp;
                case Keys.PageDown: return KeyCode.PageDown;
                case Keys.OemMinus: return KeyCode.Minus;
                case Keys.OemPlus: return KeyCode.Equals;
                case Keys.OemOpenBrackets: return KeyCode.LeftBracket;
                case Keys.OemCloseBrackets: return KeyCode.RightBracket;
                case Keys.OemPipe: return KeyCode.Backslash;
                case Keys.OemSemicolon: return KeyCode.Semicolon;
                case Keys.OemQuotes: return KeyCode.Apostrophe;
                case Keys.OemTilde: return KeyCode.Grave;
                case Keys.OemComma: return KeyCode.Comma;
                case Keys.OemPeriod: return KeyCode.Period;
                case Keys.OemQuestion: return KeyCode.Slash;
                default: return KeyCode.None;
            }
        }
    }
}