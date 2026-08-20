using System;
using System.Collections.Generic;
using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Gum {
    public static class GumBuilderExtensions {
        /// <summary>
        /// Configures Gum UI framework as the implementation of <see cref="IUserInterfaceService"/>.
        /// </summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="game">The running game instance.</param>
        /// <param name="managerContent">The ContentManager used by Gum to load assets.</param>
        /// <param name="version">Gum visual version.</param>
        /// <returns>The builder. </returns>
        /// <exception cref="ArgumentNullException">Thrown if builder, game, or managerContent is null.</exception>
        public static GameBuilder UseGum(
            this GameBuilder builder, 
            Game game, 
            ContentManager managerContent, 
            DefaultVisualsVersion version, 
            IEnumerable<Keys> keysTabForward = null, 
            IEnumerable<Keys> keysTabReverse = null
        ) {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }
            if (game == null) { throw new ArgumentNullException(nameof(game)); }
            if (managerContent == null) { throw new ArgumentNullException(nameof(managerContent)); }
            
            // Create the Gum service with its dependencies
            var serviceGum = new GumService(
                game, 
                version, 
                keysTabForward, 
                keysTabReverse
            );
            
            // Register the service so other modules can inject IUserInterfaceService if needed
            builder.RegisterService<IUserInterfaceService>(serviceGum);
            
            // Add an internal module that will call Initialize during LoadContent phase
            builder.AddModule(new GumInitializationModule(serviceGum, managerContent));
            
            // Public forwarding module (lives in Extensions)
            builder.AddModule(new UserInterfaceModule(serviceGum));
            
            return builder;
        }
    }
}