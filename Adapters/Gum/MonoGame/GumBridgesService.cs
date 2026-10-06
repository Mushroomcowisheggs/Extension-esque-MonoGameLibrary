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
        public IGumTextureBridge<
            NineSliceRuntime,
            ColoredRectangleRuntime,
            TextRuntime,
            ITwoDimensionalTexture,
            TextureRegion,
            AnimationFrame
        > Texture { get; private set; }
        
        /// <summary>Gets the bridge that translates neutral key codes for Gum.</summary>
        public IGumInputBridge<KeyEventArgs, KeyCode> Input { get; private set; }
        
        /// <summary>Initializes the service around the two bridges the Gum adapter resolved during composition.</summary>
        /// <param name="texture">The graphics bridge.</param>
        /// <param name="input">The input bridge.</param>
        public GumBridgesService(
            IGumTextureBridge<
                NineSliceRuntime,
                ColoredRectangleRuntime,
                TextRuntime,
                ITwoDimensionalTexture,
                TextureRegion,
                AnimationFrame
            > texture,
            IGumInputBridge<KeyEventArgs, KeyCode> input
        ) {
            if (texture == null) {
                throw new ArgumentNullException(nameof(texture));
            }
            if (input == null) {
                throw new ArgumentNullException(nameof(input));
            }
            Texture = texture;
            Input = input;
        }
    }
}
