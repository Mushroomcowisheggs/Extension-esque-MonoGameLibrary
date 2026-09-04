using System;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Core.Modularity;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Gum {
    /// <summary>
    /// Registers MonoGame bridge implementations before the Gum module runs.
    /// Existing registrations are retained so a composition root can replace
    /// either backend bridge.
    /// </summary>
    [ModuleRegistration(-400)]
    public sealed class MonoGameGumBridgeModule : IModule {
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
                bridgeInput = new MonoGameGumInputBridge();
                builder.RegisterService(bridgeInput);
            }
        }
    }
}
