using System;
using Godot;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Adapters.Godot.Lifecycle {
    /// <summary>
    /// The draw seam of the Godot hosting integration. Godot only accepts immediate-mode
    /// draw submissions inside the <see cref="CanvasItem._Draw"/> callback, so this node
    /// receives one redraw request per frame and forwards the callback to the host driver.
    /// The node derives from <see cref="Node2D"/> because <see cref="CanvasItem"/> itself
    /// cannot be constructed from managed code (its constructor is internal); the 2D
    /// transform stays at identity and carries no meaning here.
    /// </summary>
    public sealed partial class RenderNode : Node2D {
        /// <summary>Gets or sets the timing snapshot forwarded to the draw callback.</summary>
        public FrameTime FrameTime { get; set; }
        
        /// <summary>Gets or sets the callback invoked when Godot issues this frame's draw pass.</summary>
        public Action<FrameTime> ActionDraw { get; set; }
        
        /// <summary>Invokes the draw callback for the current frame.</summary>
        public override void _Draw() {
            Action<FrameTime> action = ActionDraw;
            if (action != null) {
                action.Invoke(FrameTime);
            }
        }
    }
}
