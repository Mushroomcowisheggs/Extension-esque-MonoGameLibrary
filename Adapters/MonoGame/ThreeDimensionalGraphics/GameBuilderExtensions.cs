using System;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Adapters.MonoGame.ThreeDimensionalGraphics {
    /// <summary>
    /// Extension methods for registering MonoGame three-dimensional graphics.
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>
        /// Registers the three-dimensional graphics module and its services.
        /// </summary>
        /// <param name="builder">The game builder.</param>
        /// <param name="orderUpdate">The renderer update order.</param>
        /// <param name="orderDraw">The renderer draw order.</param>
        /// <returns>The game builder.</returns>
        public static GameBuilder UseThreeDimensionalGraphics(
            this GameBuilder builder,
            int orderUpdate = 0,
            int orderDraw = 0
        ) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            ThreeDimensionalGraphicsModule module =
                new ThreeDimensionalGraphicsModule(orderUpdate, orderDraw);
            module.Register(builder);
            return builder;
        }
    }
}
