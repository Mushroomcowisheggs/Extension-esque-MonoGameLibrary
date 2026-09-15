using System;

namespace MonoGameLibrary.Extensions.Input {
    /// <summary>
    /// Provides extension methods for <see cref="IInputService"/> to clarify 
    /// continuous vs. edge‑triggered input queries.
    /// </summary>
    public static class InputServiceExtensions {
        /// <summary>
        /// Returns true if the key is currently held down (continuous state).
        /// </summary>
        public static bool IsKeyHeld(this IInputService service, KeyCode code) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            return service.IsKeyDown(code);
        }
        
        /// <summary>
        /// Returns true only on the frame the key was pressed (edge‑triggered).
        /// </summary>
        public static bool IsKeyPressed(this IInputService service, KeyCode code) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            return service.WasKeyJustPressed(code);
        }
        
        /// <summary>
        /// Returns true if the button is currently held down (continuous state).
        /// </summary>
        public static bool IsButtonHeld(
            this IInputService service,
            PlayerIndex indexPlayer,
            GamePadButton button
        ) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            return service.IsButtonDown(indexPlayer, button);
        }
        
        /// <summary>
        /// Returns true only on the frame the button was pressed (edge‑triggered).
        /// </summary>
        public static bool IsButtonPressed(
            this IInputService service,
            PlayerIndex indexPlayer,
            GamePadButton button
        ) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            return service.WasButtonJustPressed(indexPlayer, button);
        }
        
        /// <summary>
        /// Returns true when either Control key is held down.
        /// </summary>
        /// <param name="service">The input service to query.</param>
        /// <returns>True when the left or right Control key is down.</returns>
        public static bool IsControlHeld(this IInputService service) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (service.IsKeyDown(KeyCode.LeftControl)) {
                return true;
            }
            return service.IsKeyDown(KeyCode.RightControl);
        }
        
        /// <summary>
        /// Returns true when either Shift key is held down.
        /// </summary>
        /// <param name="service">The input service to query.</param>
        /// <returns>True when the left or right Shift key is down.</returns>
        public static bool IsShiftHeld(this IInputService service) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (service.IsKeyDown(KeyCode.LeftShift)) {
                return true;
            }
            return service.IsKeyDown(KeyCode.RightShift);
        }
        
        /// <summary>
        /// Returns true when either Alt key is held down.
        /// </summary>
        /// <param name="service">The input service to query.</param>
        /// <returns>True when the left or right Alt key is down.</returns>
        public static bool IsAltHeld(this IInputService service) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (service.IsKeyDown(KeyCode.LeftAlt)) {
                return true;
            }
            return service.IsKeyDown(KeyCode.RightAlt);
        }
        
        /// <summary>
        /// Returns true when either system key (Windows, Command, or Super) is held down.
        /// </summary>
        /// <param name="service">The input service to query.</param>
        /// <returns>True when the left or right system key is down.</returns>
        public static bool IsSystemHeld(this IInputService service) {
            if (service == null) {
                throw new ArgumentNullException(nameof(service));
            }
            if (service.IsKeyDown(KeyCode.LeftSystem)) {
                return true;
            }
            return service.IsKeyDown(KeyCode.RightSystem);
        }
    }
}