using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// Registers MonoGame graphics services and graphics-owned asset loaders.
    /// It communicates with content only through Core contracts.
    /// </summary>
    [ModuleRegistration(-450)]
    public sealed class GraphicsModule : IModule, IDisposable {
        private SpriteBatch _batchSprite;
        private RenderContext _contextRender;
        private bool _flagDisposed;
        
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            Game game = builder.GetService<Game>();
            if (game == null) {
                throw new InvalidOperationException("Game service is not registered.");
            }
            IContentBackend content = builder.GetService<IContentBackend>();
            if (content == null) {
                throw new InvalidOperationException("Content backend service is not registered.");
            }
            
            content.RegisterScoped<ITwoDimensionalTexture>(
                GraphicsLoaders.Load<ITwoDimensionalTexture>
            );
            content.RegisterScoped<IFont>(GraphicsLoaders.Load<IFont>);
            content.RegisterScoped<ISpriteFont>(GraphicsLoaders.Load<ISpriteFont>);
            content.RegisterScoped<IEffect>(GraphicsLoaders.Load<IEffect>);
            content.RegisterScoped<TextureAtlas>(GraphicsLoaders.Load<TextureAtlas>);
            content.RegisterScoped<Tilemap>(GraphicsLoaders.Load<Tilemap>);
            
            _batchSprite = new SpriteBatch(game.GraphicsDevice);
            _contextRender = new RenderContext(_batchSprite);
            builder.RegisterService<IRenderContext>(_contextRender);
            builder.AddModule(this);
        }
        
        public void Dispose() {
            if (_flagDisposed) {
                return;
            }
            if (_contextRender != null) {
                _contextRender.Dispose();
                _contextRender = null;
            }
            if (_batchSprite != null) {
                _batchSprite.Dispose();
                _batchSprite = null;
            }
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
