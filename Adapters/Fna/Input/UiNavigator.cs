using System;
using MonoGameLibrary.Extensions.Input;
using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Fna.Input {
    /// <summary>
    /// <see cref="IUiNavigator"/> over the adapter's input service: maps the neutral
    /// <see cref="NavigationKey"/> values to this platform's key and gamepad buttons
    /// and polls them for one-frame presses.
    /// </summary>
    public sealed class UiNavigator : IUiNavigator {
        private readonly IInputService _serviceInput;
        
        /// <summary>
        /// Initializes the navigator.
        /// </summary>
        /// <param name="serviceInput">The input service polled for navigation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceInput"/> is null.</exception>
        public UiNavigator(IInputService serviceInput) {
            if (serviceInput == null) {
                throw new ArgumentNullException(nameof(serviceInput));
            }
            _serviceInput = serviceInput;
        }
        
        /// <inheritdoc />
        public bool IsPressed(NavigationKey key) {
            KeyCode codeKey = ToKeyCode(key);
            if (codeKey != KeyCode.None) {
                if (_serviceInput.WasKeyJustPressed(codeKey)) {
                    return true;
                }
            }
            GamePadButton? buttonGamepad = ToGamePadButton(key);
            if (buttonGamepad == null) {
                return false;
            }
            return _serviceInput.WasButtonJustPressed(PlayerIndex.One, buttonGamepad.Value);
        }
        
        /// <inheritdoc />
        public bool IsConfirmPressed() {
            if (_serviceInput.WasKeyJustPressed(KeyCode.Enter)) {
                return true;
            }
            return _serviceInput.WasButtonJustPressed(PlayerIndex.One, GamePadButton.A);
        }
        
        private static KeyCode ToKeyCode(NavigationKey key) {
            if (key == NavigationKey.Tab) {
                return KeyCode.Tab;
            }
            if (key == NavigationKey.Up) {
                return KeyCode.Up;
            }
            if (key == NavigationKey.Down) {
                return KeyCode.Down;
            }
            if (key == NavigationKey.Left) {
                return KeyCode.Left;
            }
            if (key == NavigationKey.Right) {
                return KeyCode.Right;
            }
            if (key == NavigationKey.Enter) {
                return KeyCode.Enter;
            }
            if (key == NavigationKey.Escape) {
                return KeyCode.Escape;
            }
            return KeyCode.None;
        }
        
        private static GamePadButton? ToGamePadButton(NavigationKey key) {
            if (key == NavigationKey.Up) {
                return GamePadButton.DPadUp;
            }
            if (key == NavigationKey.Down) {
                return GamePadButton.DPadDown;
            }
            if (key == NavigationKey.Left) {
                return GamePadButton.DPadLeft;
            }
            if (key == NavigationKey.Right) {
                return GamePadButton.DPadRight;
            }
            return null;
        }
    }
}
