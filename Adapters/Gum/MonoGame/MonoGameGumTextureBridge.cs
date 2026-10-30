using System;
using Gum.Graphics.Animation;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.GueDeriving;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Graphics;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>
    /// Applies platform-neutral graphics values to Gum's MonoGame runtime types.
    /// </summary>
    public sealed class MonoGameGumTextureBridge : ITextureBridge<
        NineSliceRuntime,
        ColoredRectangleRuntime,
        TextRuntime,
        ITwoDimensionalTexture,
        TextureRegion,
        AnimationFrame
    > {
        /// <summary>Applies a neutral texture to a nine-slice's texture property.</summary>
        /// <param name="target">The nine-slice to apply it to.</param>
        /// <param name="texture">The texture to apply; it must expose a MonoGame <c>Texture2D</c>.</param>
        public void ApplyToNineSlice(
            NineSliceRuntime target,
            ITwoDimensionalTexture texture
        ) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            target.Texture = GetTexture(texture, nameof(texture));
        }
        
        /// <summary>Applies a neutral colour to a nine-slice.</summary>
        /// <param name="target">The nine-slice to apply it to.</param>
        /// <param name="color">The colour to apply.</param>
        public void ApplyColorToNineSlice(
            NineSliceRuntime target,
            Color color
        ) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            target.Color = ToMonoGameColor(color);
        }
        
        /// <summary>Applies a neutral colour to a coloured rectangle.</summary>
        /// <param name="target">The rectangle to apply it to.</param>
        /// <param name="color">The colour to apply.</param>
        public void ApplyColorToColoredRectangle(
            ColoredRectangleRuntime target,
            Color color
        ) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            target.Color = ToMonoGameColor(color);
        }
        
        /// <summary>Applies a neutral colour to a text runtime.</summary>
        /// <param name="target">The text runtime to apply it to.</param>
        /// <param name="color">The colour to apply.</param>
        public void ApplyColorToText(TextRuntime target, Color color) {
            if (target == null) {
                throw new ArgumentNullException(nameof(target));
            }
            target.Color = ToMonoGameColor(color);
        }
        
        /// <summary>Creates a Gum animation frame from a neutral texture region.</summary>
        /// <param name="region">The region the frame displays.</param>
        /// <param name="lengthFrame">How long the frame is displayed.</param>
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
            // The type test establishes that the capability exists, not that it still holds a value: a
            // released wrapper resolves to null. Rejecting it here keeps the failure at the call site
            // instead of letting a null reach Gum's runtime, where it would surface later and elsewhere.
            Texture2D textureNative = provider.GetNativeTexture();
            if (textureNative == null) {
                throw new ArgumentException(
                    "Texture no longer resolves a MonoGame Texture2D: the adapter behind it was released.",
                    nameParameter
                );
            }
            return textureNative;
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
