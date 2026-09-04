using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Adapters.MonoGame.Lifecycle {
    /// <summary>Creates platform services before configuring and building the host.</summary>
    internal sealed class IntegrationGame : Game {
        private readonly GameApplicationOptions _options;
        private readonly Action<GameBuilder> _actionConfigure;
        private readonly GraphicsDeviceManager _managerGraphics;
        private IGameHost _host;
        private IContentService _serviceContent;
        private bool _flagHostInitialized;
        private bool _flagDisposed;
        
        public IntegrationGame(GameApplicationOptions options, Action<GameBuilder> actionConfigure) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            if (actionConfigure == null) {
                throw new ArgumentNullException(nameof(actionConfigure));
            }
            options.Validate();
            _options = options;
            _actionConfigure = actionConfigure;
            _managerGraphics = new GraphicsDeviceManager(this);
            _managerGraphics.PreferredBackBufferWidth = options.Width;
            _managerGraphics.PreferredBackBufferHeight = options.Height;
            _managerGraphics.IsFullScreen = options.IsFullScreen;
            _managerGraphics.SynchronizeWithVerticalRetrace = options.IsVerticalSyncEnabled;
            Content.RootDirectory = options.ContentRootDirectory;
            Window.Title = options.Title;
            IsMouseVisible = options.IsMouseVisible;
            IsFixedTimeStep = options.IsFixedTimeStep;
        }
        
        protected override void LoadContent() {
            GameBuilder builder = new GameBuilder();
            builder.RegisterService<Game>(this);
            builder.RegisterService<ContentManager>(Content);
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
            base.LoadContent();
        }
        
        protected override void Update(GameTime timeGame) {
            if (_flagHostInitialized) {
                _host.Update(new FrameTime(timeGame.TotalGameTime, timeGame.ElapsedGameTime));
            }
            base.Update(timeGame);
        }
        
        protected override void Draw(GameTime timeGame) {
            if (_flagHostInitialized) {
                _host.Draw(new FrameTime(timeGame.TotalGameTime, timeGame.ElapsedGameTime));
            }
            base.Draw(timeGame);
        }
        
        protected override void Dispose(bool flagDisposing) {
            if (flagDisposing && !_flagDisposed) {
                _flagDisposed = true;
                if (_host != null) {
                    _host.Dispose();
                }
            }
            base.Dispose(flagDisposing);
        }
    }
}
