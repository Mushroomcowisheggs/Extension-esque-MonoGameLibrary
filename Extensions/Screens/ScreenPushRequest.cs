using System;

namespace MonoGameLibrary.Extensions.Screens {
    /// <summary>
    /// A request to push a screen onto the stack.
    /// </summary>
    public sealed class ScreenPushRequest : ScreenTransitionRequest {
        private readonly Screen _screen;
        
        /// <summary>
        /// Initializes a push request.
        /// </summary>
        /// <param name="screen">The screen to push. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="screen"/> is null. </exception>
        public ScreenPushRequest(Screen screen) {
            if (screen == null) {
                throw new ArgumentNullException(nameof(screen));
            }
            _screen = screen;
        }
        
        /// <summary>
        /// Gets the screen to push.
        /// </summary>
        public Screen Screen {
            get { return _screen; }
        }
    }
}
