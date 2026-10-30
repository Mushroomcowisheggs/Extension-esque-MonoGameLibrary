using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Fna.Graphics {
    /// <summary>
    /// Registers FNA graphics services, the runtime texture and render target
    /// factories, and graphics-owned asset loaders.
    /// It communicates with content only through Core contracts.
    /// </summary>
    [ModuleRegistration(-450)]
    public sealed class GraphicsModule : IModule, IDisposable {
        private SpriteBatch _batchSprite;
        private RenderContext _contextRender;
        private TextureFactory _factoryTexture;
        private RenderTargetFactory _factoryRenderTarget;
        private bool _flagDisposed;
        
        /// <summary>
        /// Registers the graphics services and asset loaders on the supplied builder.
        /// </summary>
        /// <param name="builder">The builder that receives the services and this module.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the game or the content backend service is not registered.
        /// </exception>
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            Game game = builder.GetService<Game>();
            if (game == null) {
                throw new InvalidOperationException("Game service is not registered.");
            }
            IContentBackend backendContent = builder.GetService<IContentBackend>();
            if (backendContent == null) {
                throw new InvalidOperationException("Content backend service is not registered.");
            }
            
            backendContent.RegisterScoped<ITwoDimensionalTexture>(
                GraphicsLoaders.Load<ITwoDimensionalTexture>
            );
            backendContent.RegisterScoped<IFont>(GraphicsLoaders.Load<IFont>);
            backendContent.RegisterScoped<ISpriteFont>(GraphicsLoaders.Load<ISpriteFont>);
            backendContent.RegisterScoped<IEffect>(GraphicsLoaders.Load<IEffect>);
            backendContent.RegisterScoped<TextureAtlas>(GraphicsLoaders.Load<TextureAtlas>);
            backendContent.RegisterScoped<Tilemap>(GraphicsLoaders.Load<Tilemap>);
            
            _batchSprite = new SpriteBatch(game.GraphicsDevice);
            _contextRender = new RenderContext(_batchSprite);
            _factoryTexture = new TextureFactory(game.GraphicsDevice);
            _factoryRenderTarget = new RenderTargetFactory(game.GraphicsDevice);
            builder.RegisterService<IRenderContext>(_contextRender);
            builder.RegisterService<ITextureFactory>(_factoryTexture);
            builder.RegisterService<IRenderTargetFactory>(_factoryRenderTarget);
            builder.AddModule(this);
        }
        
        /// <summary>
        /// Releases the sprite batch and render context this module created.
        /// The operation is idempotent.
        /// </summary>
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
            _factoryTexture = null;
            _factoryRenderTarget = null;
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}