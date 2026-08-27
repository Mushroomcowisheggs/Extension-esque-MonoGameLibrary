using System;
using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Gum {
    /// <summary>
    /// Platform-specific module that registers the Gum UI service. 
    /// Implements <see cref="IModule"/> for automatic discovery. 
    /// </summary>
    public sealed class GumModule : IModule {
        private global::Gum.Forms.DefaultVisualsVersion _version;
        private System.Collections.Generic.IEnumerable<Keys> _keysTabForward;
        private System.Collections.Generic.IEnumerable<Keys> _keysTabReverse;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="GumModule"/> class.
        /// </summary>
        /// <param name="version">Gum visual version. Defaults to V3.</param>
        /// <param name="keysTabForward">Keys to navigate forward (default: Tab).</param>
        /// <param name="keysTabReverse">Keys to navigate backward (default: Shift+Tab).</param>
        public GumModule(
            global::Gum.Forms.DefaultVisualsVersion version = global::Gum.Forms.DefaultVisualsVersion.V3,
            System.Collections.Generic.IEnumerable<Keys> keysTabForward = null,
            System.Collections.Generic.IEnumerable<Keys> keysTabReverse = null
        ) {
            _version = version;
            _keysTabForward = keysTabForward;
            _keysTabReverse = keysTabReverse;
        }
        
        /// <inheritdoc />
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            Game game = builder.GetService<Game>();
            if (game == null) {
                throw new InvalidOperationException("Game service not registered. Please register Game instance before loading GumModule.");
            }
            
            ContentManager manager = builder.GetService<ContentManager>();
            if (manager == null) {
                throw new InvalidOperationException("ContentManager service not registered. Please register ContentManager before loading GumModule.");
            }
            
            var serviceGum = new GumService(game, _version, _keysTabForward, _keysTabReverse);
            builder.RegisterService<IUserInterfaceService>(serviceGum);
            builder.AddModule(new GumInitializationModule(serviceGum, manager));
            builder.AddModule(new UserInterfaceModule(serviceGum));
        }
    }
}