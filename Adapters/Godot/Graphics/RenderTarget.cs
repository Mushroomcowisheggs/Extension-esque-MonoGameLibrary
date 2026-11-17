using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Godot.Graphics {
    /// <summary>
    /// Godot adapter for <see cref="IRenderTarget"/>.
    /// Wraps a <see cref="SubViewport"/>, whose <see cref="ViewportTexture"/> is itself a
    /// texture, so the target can be drawn into through <see cref="IRenderContext.SetRenderTarget"/>
    /// and drawn from with any API that accepts <see cref="ITwoDimensionalTexture"/>.
    /// Godot accepts immediate-mode draw commands only inside the draw callback of the node
    /// that owns them, so drawing into a bound target is recorded and replayed by a
    /// <see cref="TargetNode"/> living inside the viewport. The replayed commands persist as
    /// that node's command list and are re-rendered every frame until the target is cleared
    /// or released.
    /// </summary>
    public sealed class RenderTarget :
        IRenderTarget,
        INativeTextureProvider<Texture2D> {
        /// <summary>The wrapped target viewport, released and cleared by <see cref="Dispose"/>.</summary>
        private SubViewport _viewport;
        private readonly TargetNode _nodeTarget;
        private readonly List<Action<CanvasItem>> _commandsReplay = new List<Action<CanvasItem>>();
        private bool _flagDisposed;
        
        /// <summary>
        /// Returns the native Godot texture of the render target for use inside this adapter
        /// assembly, or null after this instance was disposed.
        /// </summary>
        public Texture2D GetNativeTexture() {
            if (_viewport == null) { return null; }
            return _viewport.GetTexture();
        }
        
        /// <summary>Gets the native Godot render target viewport so the render context can bind it.</summary>
        /// <exception cref="ObjectDisposedException">Thrown when the target has already been released.</exception>
        internal SubViewport NativeTarget {
            get {
                if (_viewport == null) {
                    throw new ObjectDisposedException(nameof(RenderTarget));
                }
                return _viewport;
            }
        }
        
        /// <summary>
        /// Gets the canvas item that replays the recorded draw commands inside the target
        /// viewport. The render context reads its state when a rendering block begins.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the target has already been released.</exception>
        internal CanvasItem ReplayItem {
            get {
                if (_flagDisposed) {
                    throw new ObjectDisposedException(nameof(RenderTarget));
                }
                return _nodeTarget;
            }
        }
        
        /// <inheritdoc />
        public int Width {
            get {
                if (_viewport == null) { return 0; }
                return _viewport.Size.X;
            }
        }
        
        /// <inheritdoc />
        public int Height {
            get {
                if (_viewport == null) { return 0; }
                return _viewport.Size.Y;
            }
        }
        
        /// <inheritdoc />
        public bool IsPreservingContents {
            get {
                if (_viewport == null) { return false; }
                return _viewport.RenderTargetClearMode == SubViewport.ClearMode.Never;
            }
        }
        
        /// <summary>Initializes a new instance of the <see cref="RenderTarget"/> class.</summary>
        /// <param name="viewport">The Godot render target viewport to wrap. Ownership transfers to this instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="viewport"/> is null.</exception>
        public RenderTarget(SubViewport viewport) {
            if (viewport == null) { throw new ArgumentNullException(nameof(viewport)); }
            _viewport = viewport;
            _nodeTarget = new TargetNode();
            _nodeTarget.ActionReplay = ReplayCommands;
            viewport.AddChild(_nodeTarget);
        }
        
        /// <summary>
        /// Appends one recorded draw command to the replay list of this target.
        /// </summary>
        /// <param name="commandReplay">The command to replay inside the target node's draw callback.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="commandReplay"/> is null.</exception>
        internal void RecordCommand(Action<CanvasItem> commandReplay) {
            if (commandReplay == null) { throw new ArgumentNullException(nameof(commandReplay)); }
            _commandsReplay.Add(commandReplay);
        }
        
        /// <summary>
        /// Discards every recorded command. Used by <see cref="IRenderContext.Clear"/>,
        /// which replaces the whole contents of the target.
        /// </summary>
        internal void ClearCommands() {
            _commandsReplay.Clear();
        }
        
        /// <summary>
        /// Asks the target node to replay its command list on the next deferred redraw pass.
        /// </summary>
        internal void CommitCommands() {
            _nodeTarget.QueueRedraw();
        }
        
        private void ReplayCommands() {
            for (int index = 0; index < _commandsReplay.Count; index += 1) {
                _commandsReplay[index].Invoke(_nodeTarget);
            }
        }
        
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="contextRender"/> is null.</exception>
        public void DrawInto(
            IRenderContext contextRender,
            TwoDimensionalVector position,
            OptionalValue<Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector origin,
            TwoDimensionalVector scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            if (contextRender == null) {
                throw new ArgumentNullException(nameof(contextRender));
            }
            contextRender.DrawTexture(
                this, position, rectangleSource, color, rotation, origin, scale, effectsSprite, depthLayer
            );
        }
        
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="streamData"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the target has already been released.</exception>
        public void SavePng(Stream streamData) {
            if (streamData == null) { throw new ArgumentNullException(nameof(streamData)); }
            if (_flagDisposed || _viewport == null) { throw new ObjectDisposedException(nameof(RenderTarget)); }
            Texture2D texture = _viewport.GetTexture();
            Image image = texture.GetImage();
            try {
                byte[] bufferImage = image.SavePngToBuffer();
                streamData.Write(bufferImage, 0, bufferImage.Length);
            } finally {
                if (!ReferenceEquals(image, texture)) {
                    image.Dispose();
                }
            }
        }
        
        /// <summary>
        /// Releases the underlying render target viewport and its replay node.
        /// The operation is idempotent.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            _commandsReplay.Clear();
            if (_viewport != null) {
                _nodeTarget.ActionReplay = null;
                _viewport.QueueFree();
                _viewport = null;
            }
            GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// The replay node inside the target viewport. Godot only accepts immediate-mode draw
        /// submissions inside the receiving node's draw callback, so the render context records
        /// commands while the target is bound and this node executes them during its own draw
        /// callback. The node derives from <see cref="Node2D"/> because <see cref="CanvasItem"/>
        /// itself cannot be constructed from managed code (its constructor is internal).
        /// </summary>
        private sealed partial class TargetNode : Node2D {
            /// <summary>Gets or sets the callback that replays the recorded draw commands.</summary>
            public Action ActionReplay { get; set; }
            
            /// <summary>Replays the recorded draw commands for this target.</summary>
            public override void _Draw() {
                Action action = ActionReplay;
                if (action != null) {
                    action.Invoke();
                }
            }
        }
    }
}
