using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Adapters.MonoGame.Hosting {
    /// <summary>
    /// Adapts a MonoGame <see cref="Game"/> to work with an <see cref="IGameHost"/>. 
    /// </summary>
    /// <remarks>
    /// This is a low-level, manual driving helper; the higher-level entry point is 
    /// <c>GameApplication</c>/<c>IntegrationGame</c>, which drives the host automatically. 
    /// Who brackets the frame with <see cref="SpriteBatch.Begin"/>/<see cref="SpriteBatch.End"/> 
    /// depends on the modules being driven: 
    /// modern modules render through <c>IRenderContext</c> and call its Begin/End inside their own 
    /// Draw, and the caller must then NOT wrap <see cref="Draw"/> in <see cref="SpriteBatch.Begin"/>/
    /// <see cref="SpriteBatch.End"/>, because the render context begins the same SpriteBatch and a 
    /// nested Begin would throw. Only the legacy pattern — modules drawing through a SpriteBatch 
    /// that the caller begins and ends externally, never calling <c>IRenderContext.Begin/End</c> 
    /// themselves — requires the caller to wrap <see cref="Draw"/>. The two styles must not be 
    /// mixed within one frame. 
    /// </remarks>
    public class MonoGameAdapter : IDisposable {
        private readonly IGameHost _host;
        private bool _flagDisposed = false;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MonoGameAdapter"/> class. 
        /// </summary>
        /// <param name="host">The <see cref="IGameHost"/> to drive. </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="host"/> is null. 
        /// </exception>
        public MonoGameAdapter(IGameHost host) {
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
        /// Updates all modules for the current frame. 
        /// </summary>
        /// <param name="timeGame">The MonoGame timing snapshot. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="timeGame"/> is null. </exception>
        public void Update(GameTime timeGame) {
            if (timeGame == null) { throw new ArgumentNullException(nameof(timeGame)); }
            _host.Update(new FrameTime(timeGame.TotalGameTime, timeGame.ElapsedGameTime));
        }
        
        /// <summary>
        /// Draws all modules. For modules that render through <c>IRenderContext</c> (which manage 
        /// Begin/End themselves) the caller must NOT wrap this call in 
        /// <see cref="SpriteBatch.Begin"/>/<see cref="SpriteBatch.End"/>; wrapping is only correct 
        /// for the legacy externally-begun SpriteBatch pattern described in the class remarks. 
        /// </summary>
        /// <param name="timeGame">The MonoGame timing snapshot. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="timeGame"/> is null. </exception>
        public void Draw(GameTime timeGame) {
            if (timeGame == null) { throw new ArgumentNullException(nameof(timeGame)); }
            _host.Draw(new FrameTime(timeGame.TotalGameTime, timeGame.ElapsedGameTime));
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