using System;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>
    /// Registers the MonoGame bridge implementations before the Gum module runs.
    /// Existing registrations are retained so a composition root can replace either bridge.
    /// Registering the MonoGame input module first is required, because it is the module
    /// that owns the key translation table this bridge consumes.
    /// </summary>
    [ModuleRegistration(-400)]
    public sealed class MonoGameGumBridgeModule : IModule {
        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no MonoGame key translation table is registered.
        /// </exception>
        public void Register(GameBuilder builder) {
            if (builder == null) {
                throw new ArgumentNullException(nameof(builder));
            }
            
            IGumTextureBridge<
                NineSliceRuntime,
                ColoredRectangleRuntime,
                TextRuntime,
                ITwoDimensionalTexture,
                TextureRegion,
                AnimationFrame
            > bridgeTexture;
            if (!builder.TryGetService(out bridgeTexture)) {
                bridgeTexture = new MonoGameGumTextureBridge();
                builder.RegisterService(bridgeTexture);
            }
            
            IGumInputBridge<KeyEventArgs, KeyCode> bridgeInput;
            if (!builder.TryGetService(out bridgeInput)) {
                INativeKeyProvider<Keys> providerKey = builder.GetService<INativeKeyProvider<Keys>>();
                if (providerKey == null) {
                    throw new InvalidOperationException(
                        "No MonoGame key translation table is registered. Register the MonoGame input module before the Gum bridge module."
                    );
                }
                bridgeInput = new MonoGameGumInputBridge(providerKey);
                builder.RegisterService(bridgeInput);
            }
        }
    }
}