using System;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// Extension methods for registering input services with a <see cref="GameBuilder"/>.
    /// </summary>
    public static class GameBuilderExtensions {
        /// <summary>
        /// Registers the input service and module.
        /// </summary>
        /// <param name="builder">The game builder instance. </param>
        /// <returns>The game builder instance. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null. </exception>
        public static GameBuilder UseInput(this GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            var module = new InputModule();
            module.Register(builder);
            return builder;
        }
        
        /// <summary>
        /// Registers the default input mapping service with the builder. 
        /// Requires that <see cref="IInputService"/> has been registered first. 
        /// </summary>
        /// <param name="builder">The game builder instance. </param>
        /// <returns>The builder. </returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IInputService"/> is not registered.</exception>
        public static GameBuilder UseInputMapping(this GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            if (!builder.TryGetService<IInputService>(out var serviceInput)) {
                throw new InvalidOperationException(
                    "IInputService must be registered before calling UseInputMapping. " +
                    "Use builder.UseInput() or register manually. "
                );
            }
            
            var serviceMapping = new DefaultInputMappingService(serviceInput);
            builder.RegisterService<IInputMappingService>(serviceMapping);
            return builder;
        }
    }
}