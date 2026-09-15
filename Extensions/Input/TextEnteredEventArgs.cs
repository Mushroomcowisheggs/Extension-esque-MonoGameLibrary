using System;

namespace MonoGameLibrary.Extensions.Input {
    /// <summary>
    /// Carries a single character reported by the platform text input system.
    /// Control characters are forwarded unchanged so that callers can decide
    /// whether they are meaningful for their own input field.
    /// </summary>
    public sealed class TextEnteredEventArgs : EventArgs {
        private readonly char _character;
        
        /// <summary>Gets the character that was entered.</summary>
        public char Character {
            get {
                return _character;
            }
        }
        
        /// <summary>Initializes a new instance for the supplied character.</summary>
        /// <param name="character">The entered character.</param>
        public TextEnteredEventArgs(char character) {
            _character = character;
        }
    }
}