using System;
using Gum.Graphics.Animation;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>Bridges platform-neutral graphics values into Gum visuals.</summary>
    public static class GumGraphicsExtensions {
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
