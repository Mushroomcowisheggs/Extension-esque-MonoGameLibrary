using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Core.Lifecycle {
    /// <summary>Exposes platform-neutral application operations to the game layer.</summary>
    public interface IGameApplicationService {
        /// <summary>
        /// Gets the configured client-area bounds in render-surface coordinates.
        /// The top-left origin is (0, 0).
        /// </summary>
        Rectangle ClientBounds { get; }
        
        /// <summary>Requests that the running game application exit.</summary>
        void Exit();
    }
}
