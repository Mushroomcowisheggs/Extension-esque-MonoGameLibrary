using System;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>
    /// A request to replace the current screen with another one.
    /// </summary>
    public sealed class ScreenChangeRequest : ScreenTransitionRequest {
        private readonly Screen _screen;
        
        /// <summary>
        /// Initializes a change request.
        /// </summary>
        /// <param name="screen">The screen that replaces the current one. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="screen"/> is null. </exception>
        public ScreenChangeRequest(Screen screen) {
            if (screen == null) {
                throw new ArgumentNullException(nameof(screen));
            }
            _screen = screen;
        }
        
        /// <summary>
        /// Gets the screen that replaces the current one.
        /// </summary>
        public Screen Screen {
            get { return _screen; }
        }
    }
}
