using System;
using Gum.Forms.Controls;
using Gum.Graphics.Animation;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>
    /// Holds the backend bridges used by the Gum adapter and its consumers.
    /// </summary>
    public sealed class GumBridgesService {
        /// <summary>Gets the bridge that applies neutral graphics values to Gum's MonoGame runtime types.</summary>
        public ITextureBridge<
            NineSliceRuntime,
            ColoredRectangleRuntime,
            TextRuntime,
            ITwoDimensionalTexture,
            TextureRegion,
            AnimationFrame
        > TextureBridge { get; private set; }
        
        /// <summary>Gets the bridge that translates neutral key codes for Gum.</summary>
        public IInputBridge<KeyEventArgs, KeyCode> InputBridge { get; private set; }
        
        /// <summary>Initializes the service around the two bridges the Gum adapter resolved during composition.</summary>
        /// <param name="bridgeTexture">The graphics bridge.</param>
        /// <param name="bridgeInput">The input bridge.</param>
        public GumBridgesService(
            ITextureBridge<
                NineSliceRuntime,
                ColoredRectangleRuntime,
                TextRuntime,
                ITwoDimensionalTexture,
                TextureRegion,
                AnimationFrame
            > bridgeTexture,
            IInputBridge<KeyEventArgs, KeyCode> bridgeInput
        ) {
            if (bridgeTexture == null) {
                throw new ArgumentNullException(nameof(bridgeTexture));
            }
            if (bridgeInput == null) {
                throw new ArgumentNullException(nameof(bridgeInput));
            }
            TextureBridge = bridgeTexture;
            InputBridge = bridgeInput;
        }
    }
}
