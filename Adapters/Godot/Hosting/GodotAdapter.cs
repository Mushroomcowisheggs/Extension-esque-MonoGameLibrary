using System;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Adapters.Godot.Hosting {
    /// <summary>
    /// Adapts the Godot frame callbacks to work with an <see cref="IGameHost"/>.
    /// </summary>
    /// <remarks>
    /// This is a low-level, manual driving helper; the higher-level entry point is
    /// <c>GameApplication</c>/<c>IntegrationNode</c>, which drives the host automatically
    /// from the scene tree. Who brackets the frame depends on the modules being driven:
    /// modules render through <c>IRenderContext</c> and call its Begin/End inside their own
    /// Draw, and Godot only accepts immediate-mode draw submissions inside a
    /// <c>CanvasItem</c> draw pass, so <see cref="Draw(FrameTime)"/> must be called from a
    /// <c>_Draw</c> callback (or an equivalent draw pass), never from <c>_Process</c>.
    /// The <see cref="double"/> overloads accumulate the total time internally, converting
    /// the engine's frame delta into a timing snapshot.
    /// </remarks>
    public class GodotAdapter : IDisposable {
        private readonly IGameHost _host;
        private TimeSpan _spanTotalTime;
        private bool _flagDisposed = false;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GodotAdapter"/> class.
        /// </summary>
        /// <param name="host">The <see cref="IGameHost"/> to drive. </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="host"/> is null.
        /// </exception>
        public GodotAdapter(IGameHost host) {
            if (host == null) { throw new ArgumentNullException(nameof(host)); }
            _host = host;
        }
        
        /// <summary>
        /// Loads all module content using the provided <see cref="IContentService"/>.
        /// </summary>
        /// <param name="serviceContent">The content service to use for loading. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceContent"/> is null. </exception>
        public void LoadContent(IContentService serviceContent) {
            if (serviceContent == null) { throw new ArgumentNullException(nameof(serviceContent)); }
            _host.Initialize(serviceContent);
        }
        
        /// <summary>
        /// Updates all modules for the current frame using a Godot frame delta in seconds.
        /// The total time is accumulated internally by this adapter.
        /// </summary>
        /// <param name="deltaSeconds">The engine-provided frame delta in seconds. </param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="deltaSeconds"/> is negative. </exception>
        public void Update(double deltaSeconds) {
            if (deltaSeconds < 0) { throw new ArgumentOutOfRangeException(nameof(deltaSeconds)); }
            TimeSpan spanDelta = TimeSpan.FromSeconds(deltaSeconds);
            _spanTotalTime += spanDelta;
            Update(new FrameTime(_spanTotalTime, spanDelta));
        }
        
        /// <summary>
        /// Updates all modules for the current frame with an explicit timing snapshot.
        /// </summary>
        /// <param name="timeFrame">The frame timing snapshot. </param>
        public void Update(FrameTime timeFrame) {
            _host.Update(timeFrame);
        }
        
        /// <summary>
        /// Draws all modules using a Godot frame delta in seconds. Must be called inside a
        /// <c>CanvasItem</c> draw pass. The total time is accumulated internally by this adapter.
        /// </summary>
        /// <param name="deltaSeconds">The engine-provided frame delta in seconds. </param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="deltaSeconds"/> is negative. </exception>
        public void Draw(double deltaSeconds) {
            if (deltaSeconds < 0) { throw new ArgumentOutOfRangeException(nameof(deltaSeconds)); }
            TimeSpan spanDelta = TimeSpan.FromSeconds(deltaSeconds);
            Draw(new FrameTime(_spanTotalTime, spanDelta));
        }
        
        /// <summary>
        /// Draws all modules with an explicit timing snapshot. Must be called inside a
        /// <c>CanvasItem</c> draw pass.
        /// </summary>
        /// <param name="timeFrame">The frame timing snapshot. </param>
        public void Draw(FrameTime timeFrame) {
            _host.Draw(timeFrame);
        }
        
        /// <summary>
        /// Disposes the adapter and the underlying host.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) { return; }
            _host.Dispose();
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
