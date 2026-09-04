using System;
using Gum.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.MonoGame.Gum {
    /// <summary>
    /// Applies platform-neutral graphics values to Gum's MonoGame runtime types.
    /// </summary>
    public sealed class MonoGameGumTextureBridge : IGumTextureBridge<
        NineSliceRuntime,
        ColoredRectangleRuntime,
        TextRuntime,
        ITwoDimensionalTexture,
        TextureRegion,
        AnimationFrame
    > {
        public void ApplyToNineSlice(
            NineSliceRuntime target,
            ITwoDimensionalTexture texture
        ) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            target.Texture = GetTexture(texture, nameof(texture));
        }
        
        public void ApplyColorToNineSlice(
            NineSliceRuntime target,
            Color color
        ) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            target.Color = ToMonoGameColor(color);
        }
        
        public void ApplyColorToColoredRectangle(
            ColoredRectangleRuntime target,
            Color color
        ) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            target.Color = ToMonoGameColor(color);
        }
        
        public void ApplyColorToText(TextRuntime target, Color color) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            target.Color = ToMonoGameColor(color);
        }
        
        public AnimationFrame CreateAnimationFrame(
            TextureRegion region,
            float lengthFrame
        ) {
            if (region == null) {
                throw new ArgumentNullException(nameof(region));
            }
            Texture2D texture = GetTexture(
                region.Texture,
                nameof(region)
            );
            return new AnimationFrame {
                TopCoordinate = region.TopTextureCoordinate,
                BottomCoordinate = region.BottomTextureCoordinate,
                LeftCoordinate = region.LeftTextureCoordinate,
                RightCoordinate = region.RightTextureCoordinate,
                FrameLength = lengthFrame,
                Texture = texture
            };
        }
        
        private static Texture2D GetTexture(
            ITwoDimensionalTexture texture,
            string nameParameter
        ) {
            INativeTextureProvider<Texture2D> provider =
                texture as INativeTextureProvider<Texture2D>;
            if (provider == null) {
                throw new ArgumentException(
                    "Texture must expose a MonoGame Texture2D through the neutral native-texture contract.",
                    nameParameter
                );
            }
            return provider.GetNativeTexture();
        }
        
        private static Microsoft.Xna.Framework.Color ToMonoGameColor(
            Color color
        ) {
            return new Microsoft.Xna.Framework.Color(
                color.R,
                color.G,
                color.B,
                color.A
            );
        }
    }
}
