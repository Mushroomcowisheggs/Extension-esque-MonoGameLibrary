using System;
using Godot;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Godot.ThreeDimensionalGraphics {
    /// <summary>
    /// Reads the Godot viewport for the platform-neutral camera. The Godot window exposes a
    /// single size space in pixels, so the aspect ratio comes straight from the viewport's
    /// visible rect.
    /// </summary>
    internal sealed class ThreeDimensionalViewport : IThreeDimensionalViewport {
        private readonly Viewport _viewport;
        
        /// <summary>
        /// Initializes a viewport source over a Godot viewport.
        /// </summary>
        /// <param name="viewport">The Godot viewport.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="viewport"/> is null.</exception>
        public ThreeDimensionalViewport(Viewport viewport) {
            if (viewport == null) {
                throw new ArgumentNullException(nameof(viewport));
            }
            _viewport = viewport;
        }
        
        /// <inheritdoc />
        public float AspectRatio {
            get {
                Rect2 rect = _viewport.GetVisibleRect();
                if (rect.Size.Y <= 0f) {
                    return 1f;
                }
                return rect.Size.X / rect.Size.Y;
            }
        }
    }
}
