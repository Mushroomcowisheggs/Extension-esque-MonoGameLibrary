using System;
using Gum.Forms.Controls;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>Bridges platform-neutral key codes into Gum event arguments.</summary>
    public static class GumInputExtensions {
        /// <summary>Returns whether the key event was raised for a neutral key code.</summary>
        /// <param name="arguments">The Gum key event to test.</param>
        /// <param name="codeKey">The neutral key code to compare against.</param>
        /// <param name="bridges">The bridges resolved at composition.</param>
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
