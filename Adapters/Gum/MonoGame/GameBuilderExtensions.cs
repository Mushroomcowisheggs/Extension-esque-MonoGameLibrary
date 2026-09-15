using System;
using System.Collections.Generic;
using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    public static class GameBuilderExtensions {
        /// <summary>
        /// Configures Gum UI framework as the implementation of <see cref="IUserInterfaceService"/>. 
        /// </summary>
        /// <param name="builder">The game builder. </param>
        /// <param name="version">Gum visual version. </param>
        /// <returns>The builder. </returns>
        /// <exception cref="ArgumentNullException">Thrown if builder, game, or managerContent is null.</exception>
        public static GameBuilder UseGum(
            this GameBuilder builder, 
            DefaultVisualsVersion version, 
            IEnumerable<Keys> keysTabForward = null, 
            IEnumerable<Keys> keysTabReverse = null
        ) {
            if (builder == null) { throw new ArgumentNullException(nameof(builder)); }
            
            var module = new GumModule(version, keysTabForward, keysTabReverse);
            module.Register(builder);
            
            return builder;
        }
    }
}