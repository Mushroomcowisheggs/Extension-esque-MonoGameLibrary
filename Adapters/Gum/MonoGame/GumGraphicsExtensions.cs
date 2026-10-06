using System;
using Gum.Graphics.Animation;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>Bridges platform-neutral graphics values into Gum visuals.</summary>
    public static class GumGraphicsExtensions {
        /// <summary>Applies a neutral texture to a Gum nine-slice.</summary>
        /// <param name="runtime">The nine-slice to apply it to.</param>
        /// <param name="texture">The texture to apply.</param>
        /// <param name="bridges">The bridges resolved at composition.</param>
        public static void SetTexture(
            this NineSliceRuntime runtime,
            ITwoDimensionalTexture texture,
            GumBridgesService bridges
        ) {
            if (runtime == null) {
                throw new ArgumentNullException(nameof(runtime));
            }
            if (bridges == null) {
                throw new ArgumentNullException(nameof(bridges));
            }
            bridges.Texture.ApplyToNineSlice(runtime, texture);
        }
        
        /// <summary>Applies a neutral colour to a Gum nine-slice.</summary>
        /// <param name="runtime">The nine-slice to apply it to.</param>
        /// <param name="color">The colour to apply.</param>
        /// <param name="bridges">The bridges resolved at composition.</param>
        public static void SetColor(
            this NineSliceRuntime runtime,
            Color color,
            GumBridgesService bridges
        ) {
            if (bridges == null) {
                throw new ArgumentNullException(nameof(bridges));
            }
            bridges.Texture.ApplyColorToNineSlice(runtime, color);
        }
        
        /// <summary>Applies a neutral colour to a Gum coloured rectangle.</summary>
        /// <param name="runtime">The rectangle to apply it to.</param>
        /// <param name="color">The colour to apply.</param>
        /// <param name="bridges">The bridges resolved at composition.</param>
        public static void SetColor(
            this ColoredRectangleRuntime runtime,
            Color color,
            GumBridgesService bridges
        ) {
            if (bridges == null) {
                throw new ArgumentNullException(nameof(bridges));
            }
            bridges.Texture.ApplyColorToColoredRectangle(runtime, color);
        }
        
        /// <summary>Applies a neutral colour to a Gum text runtime.</summary>
        /// <param name="runtime">The text runtime to apply it to.</param>
        /// <param name="color">The colour to apply.</param>
        /// <param name="bridges">The bridges resolved at composition.</param>
        public static void SetColor(
            this TextRuntime runtime,
            Color color,
            GumBridgesService bridges
        ) {
            if (bridges == null) {
                throw new ArgumentNullException(nameof(bridges));
            }
            bridges.Texture.ApplyColorToText(runtime, color);
        }
        
        /// <summary>Creates a Gum animation frame from a neutral texture region.</summary>
        /// <param name="region">The region the frame displays.</param>
        /// <param name="lengthFrame">How long the frame is displayed.</param>
        /// <param name="bridges">The bridges resolved at composition.</param>
        public static AnimationFrame CreateAnimationFrame(
            TextureRegion region,
            float lengthFrame,
            GumBridgesService bridges
        ) {
            if (bridges == null) {
                throw new ArgumentNullException(nameof(bridges));
            }
            return bridges.Texture.CreateAnimationFrame(region, lengthFrame);
        }
    }
}
