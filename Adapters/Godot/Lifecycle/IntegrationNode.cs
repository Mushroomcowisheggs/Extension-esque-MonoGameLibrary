using System;
using Godot;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;
using GodotInput = global::Godot.Input;
using GodotWindow = global::Godot.Window;

namespace MonoGameLibrary.Adapters.Godot.Lifecycle {
    /// <summary>
    /// Adapts the Godot scene tree to drive an <see cref="IGameHost"/>. This is the Godot
    /// host driver for a scene-tree application: the engine owns the frame loop, so the
    /// host is driven from node callbacks instead of from a blocking <c>Run</c>. Add this
    /// node to the scene tree (see
    /// <c>GameApplication.Run</c>) and Godot invokes Initialize, Update and Draw through
    /// <see cref="_Ready"/>, <see cref="_Process"/> and <see cref="RenderNode"/>.
    /// </summary>
    /// <remarks>
    /// The frame bracket is fixed: <see cref="_Process"/> runs the host update and requests
    /// a redraw, and Godot's draw pass invokes the host draw through <see cref="RenderNode"/>.
    /// Godot's scene-tree API is main-thread only, which matches the thread-affinity
    /// annotations on the Core contracts; background work must not touch Godot types.
    /// </remarks>
    public sealed partial class IntegrationNode : Node {
        private readonly GameApplicationOptions _options;
        private readonly Action<GameBuilder> _actionConfigure;
        private RenderNode _nodeRender;
        private IGameHost _host;
        private IContentService _serviceContent;
        private TimeSpan _spanTotalTime;
        private FrameTime _timeFrame;
        private bool _flagHostInitialized;
        private bool _flagDisposed;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationNode"/> class.
        /// </summary>
        /// <param name="options">The window and timing settings.</param>
        /// <param name="actionConfigure">Composes the host's services and modules.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="options"/> or <paramref name="actionConfigure"/> is null.
        /// </exception>
        public IntegrationNode(GameApplicationOptions options, Action<GameBuilder> actionConfigure) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            if (actionConfigure == null) {
                throw new ArgumentNullException(nameof(actionConfigure));
            }
            options.Validate();
            _options = options;
            _actionConfigure = actionConfigure;
        }
        
        /// <summary>Applies the window options, composes the host and initializes it.</summary>
        public override void _Ready() {
            base._Ready();
            GodotWindow window = GetTree().Root;
            window.Title = _options.Title;
            window.Size = new Vector2I(_options.Width, _options.Height);
            window.Mode = _options.IsFullScreen ? GodotWindow.ModeEnum.Fullscreen : GodotWindow.ModeEnum.Windowed;
            GodotInput.MouseMode = _options.IsMouseVisible
                ? GodotInput.MouseModeEnum.Visible
                : GodotInput.MouseModeEnum.Hidden;
            
            _nodeRender = new RenderNode();
            _nodeRender.ActionDraw = DrawFrame;
            
            GameBuilder builder = new GameBuilder();
            builder.FailurePolicy = FailurePolicy.Release;
            builder.RegisterService<Node>(this);
            builder.RegisterService<SceneTree>(GetTree());
            builder.RegisterService<GodotWindow>(window);
            builder.RegisterService<CanvasItem>(_nodeRender);
            builder.RegisterService<IGameApplicationService>(new GameApplicationService(this));
            
            _actionConfigure.Invoke(builder);
            _serviceContent = builder.GetService<IContentService>();
            if (_serviceContent == null) {
                throw new InvalidOperationException(
                    "No content service was registered. Load a content adapter module before building the game."
                );
            }
            _host = builder.Build();
            _host.Initialize(_serviceContent);
            _flagHostInitialized = true;
            
            AddChild(_nodeRender);
        }
        
        /// <summary>Advances the host by one frame and schedules the draw pass.</summary>
        /// <param name="delta">The engine-provided frame delta in seconds.</param>
        public override void _Process(double delta) {
            base._Process(delta);
            if (!_flagHostInitialized) {
                return;
            }
            TimeSpan spanDelta = TimeSpan.FromSeconds(delta);
            _spanTotalTime += spanDelta;
            _timeFrame = new FrameTime(_spanTotalTime, spanDelta);
            _host.Update(_timeFrame);
            _nodeRender.FrameTime = _timeFrame;
            _nodeRender.QueueRedraw();
        }
        
        /// <summary>Draws the host through the render node's draw pass.</summary>
        /// <param name="timeFrame">The timing snapshot of the frame being drawn.</param>
        private void DrawFrame(FrameTime timeFrame) {
            _host.Draw(timeFrame);
        }
        
        /// <summary>Releases the host when the node leaves the scene tree.</summary>
        public override void _ExitTree() {
            base._ExitTree();
            if (_flagDisposed) {
                return;
            }
            _flagDisposed = true;
            if (_host != null) {
                _host.Dispose();
                _host = null;
            }
            _flagHostInitialized = false;
        }
    }
}
