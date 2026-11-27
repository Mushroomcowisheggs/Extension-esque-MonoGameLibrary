using System;
using Godot;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Core.Primitives;
using GodotWindow = global::Godot.Window;

namespace MonoGameLibrary.Adapters.Godot.Lifecycle {
    /// <summary>Platform-neutral application controls backed by Godot.</summary>
    internal sealed class GameApplicationService : IGameApplicationService {
        private readonly IntegrationNode _nodeGame;
        
        public GameApplicationService(IntegrationNode nodeGame) {
            if (nodeGame == null) {
                throw new ArgumentNullException(nameof(nodeGame));
            }
            _nodeGame = nodeGame;
        }
        
        public Rectangle ClientBoundsRectangle {
            get {
                GodotWindow window = _nodeGame.GetTree().Root;
                Vector2I size = window.Size;
                return new Rectangle(0, 0, size.X, size.Y);
            }
        }
        
        public void Exit() {
            _nodeGame.GetTree().Quit();
        }
        
        public void Exit(int codeExit) {
            _nodeGame.GetTree().Quit(codeExit);
        }
    }
}
