using System;
using Gum.Forms.Controls;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Gum {
    /// <summary>Bridges platform-neutral key codes into Gum event arguments.</summary>
    public static class GumInputExtensions {
        public static bool IsKey(
            this KeyEventArgs arguments,
            KeyCode codeKey,
            GumBridgesService bridges
        ) {
            if (arguments == null) {
                throw new ArgumentNullException(nameof(arguments));
            }
            if (bridges == null) {
                throw new ArgumentNullException(nameof(bridges));
            }
            return bridges.Input.IsKeyPressed(arguments, codeKey);
        }
    }
}
