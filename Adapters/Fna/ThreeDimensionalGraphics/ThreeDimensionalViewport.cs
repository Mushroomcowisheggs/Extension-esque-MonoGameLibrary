using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Extensions.ThreeDimensionalGraphics;

namespace MonoGameLibrary.Adapters.Fna.ThreeDimensionalGraphics {
    /// <summary>
    /// Reads the MonoGame graphics-device viewport for the platform-neutral camera.
    /// </summary>
    internal sealed class ThreeDimensionalViewport : IThreeDimensionalViewport {
        private readonly GraphicsDevice _deviceGraphics;
        
        /// <summary>
        /// Initializes a viewport source over the graphics device.
        /// </summary>
        /// <param name="deviceGraphics">The graphics device.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="deviceGraphics"/> is null.</exception>
        public ThreeDimensionalViewport(GraphicsDevice deviceGraphics) {
            if (deviceGraphics == null) {
                throw new ArgumentNullException(nameof(deviceGraphics));
            }
            _deviceGraphics = deviceGraphics;
        }
        
        /// <inheritdoc />
        public float AspectRatio {
            get {
                Viewport viewport = _deviceGraphics.Viewport;
                if (viewport.Height <= 0) {
                    return 1f;
                }
                return (float)viewport.Width / (float)viewport.Height;
            }
        }
    }
}
