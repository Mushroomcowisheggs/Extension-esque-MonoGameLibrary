using System;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Gum {
    /// <summary>
    /// Applies platform-neutral key codes to Gum's MonoGame input types.
    /// </summary>
    public sealed class MonoGameGumInputBridge :
        IGumInputBridge<KeyEventArgs, KeyCode> {
        public bool IsKeyPressed(
            KeyEventArgs arguments,
            KeyCode codeKey
        ) {
            if (arguments == null) {
                throw new ArgumentNullException(nameof(arguments));
            }
            return arguments.Key == ToMonoGameKey(codeKey);
        }
        
        private static Keys ToMonoGameKey(KeyCode codeKey) {
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
                default: return Keys.None;
            }
        }
    }
}
