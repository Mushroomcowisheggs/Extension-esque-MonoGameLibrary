using System;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame implementation of <see cref="IRenderTargetFactory"/>.
    /// Targets are created with the same color format as the current back buffer so that
    /// the final composition does not need a conversion pass.
    /// </summary>
    public sealed class RenderTargetFactory : IRenderTargetFactory {
        private readonly GraphicsDevice _device;
        
        /// <summary>Initializes a new instance of the <see cref="RenderTargetFactory"/> class.</summary>
        /// <param name="device">The graphics device that owns every created target.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="device"/> is null.</exception>
        public RenderTargetFactory(GraphicsDevice device) {
            if (device == null) { throw new ArgumentNullException(nameof(device)); }
            _device = device;
        }
        
        /// <inheritdoc />
        public IRenderTarget Create(int width, int height, bool flagPreserveContents) {
            if (width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if (height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
            RenderTargetUsage usage = RenderTargetUsage.DiscardContents;
            if (flagPreserveContents) {
                usage = RenderTargetUsage.PreserveContents;
            }
            RenderTarget2D target = new RenderTarget2D(
                _device,
                width,
                height,
                false,
                _device.PresentationParameters.BackBufferFormat,
                DepthFormat.None,
                0,
                usage
            );
            return new RenderTarget(target);
        }
    }
}