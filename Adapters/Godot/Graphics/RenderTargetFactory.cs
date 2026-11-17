using System;
using Godot;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Godot.Graphics {
    /// <summary>
    /// Godot implementation of <see cref="IRenderTargetFactory"/>.
    /// Each target is a <see cref="SubViewport"/> parented under the registered drawing
    /// anchor so that it stays inside the scene tree and renders every frame. Targets start
    /// with a transparent background, matching the contract of an empty off-screen texture.
    /// </summary>
    public sealed class RenderTargetFactory : IRenderTargetFactory {
        private readonly CanvasItem _itemAnchor;
        
        /// <summary>Initializes a new instance of the <see cref="RenderTargetFactory"/> class.</summary>
        /// <param name="itemAnchor">The drawing anchor that parents every created target viewport.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="itemAnchor"/> is null.</exception>
        public RenderTargetFactory(CanvasItem itemAnchor) {
            if (itemAnchor == null) { throw new ArgumentNullException(nameof(itemAnchor)); }
            _itemAnchor = itemAnchor;
        }
        
        /// <inheritdoc />
        public IRenderTarget Create(int width, int height, bool flagPreserveContents) {
            if (width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
            if (height <= 0) { throw new ArgumentOutOfRangeException(nameof(height)); }
            SubViewport.ClearMode clearMode = SubViewport.ClearMode.Always;
            if (flagPreserveContents) {
                clearMode = SubViewport.ClearMode.Never;
            }
            SubViewport viewport = new SubViewport();
            viewport.Size = new Vector2I(width, height);
            viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
            viewport.RenderTargetClearMode = clearMode;
            viewport.TransparentBg = true;
            viewport.Disable3D = true;
            _itemAnchor.AddChild(viewport);
            return new RenderTarget(viewport);
        }
    }
}
