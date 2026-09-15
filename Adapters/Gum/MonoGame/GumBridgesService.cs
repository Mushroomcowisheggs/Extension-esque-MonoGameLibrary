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
        public IGumTextureBridge<
            NineSliceRuntime,
            ColoredRectangleRuntime,
            TextRuntime,
            ITwoDimensionalTexture,
            TextureRegion,
            AnimationFrame
        > Texture { get; private set; }
        
        public IGumInputBridge<KeyEventArgs, KeyCode> Input { get; private set; }
        
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
