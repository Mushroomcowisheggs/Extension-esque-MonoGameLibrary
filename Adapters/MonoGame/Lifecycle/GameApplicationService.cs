using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Adapters.MonoGame.Lifecycle {
    /// <summary>Platform-neutral application controls backed by MonoGame.</summary>
    internal sealed class GameApplicationService : IGameApplicationService {
        private readonly IntegrationGame _game;
        
        public GameApplicationService(IntegrationGame game) {
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
