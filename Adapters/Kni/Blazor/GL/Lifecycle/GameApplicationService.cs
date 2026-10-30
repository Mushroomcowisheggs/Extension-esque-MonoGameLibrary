using System;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Adapters.Kni.Input;
using MonoGameLibrary.Adapters.Kni.Lifecycle;

namespace MonoGameLibrary.Adapters.Kni.Blazor.GL.Lifecycle {
    /// <summary>Platform-neutral application controls backed by KNI.</summary>
    internal sealed class GameApplicationService : IGameApplicationService {
        private readonly IntegrationGame _game;
        
        public GameApplicationService(IntegrationGame game) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            _game = game;
        }
        
        public Rectangle ClientBounds {
            get {
                Microsoft.Xna.Framework.Rectangle bounds = _game.GraphicsDevice.PresentationParameters.Bounds;
                return new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }
        }
        
        public void Exit() {
            _game.Exit();
        }
    }
}
