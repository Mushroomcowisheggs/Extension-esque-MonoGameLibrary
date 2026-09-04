using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Bridge {
    /// <summary>
    /// Defines the cross-adapter contract for applying platform-neutral
    /// graphics values to Gum runtime targets. The generic target types keep
    /// Extensions independent of Gum and of any rendering backend.
    /// </summary>
    public interface IGumTextureBridge<
        in TNineSlice,
        in TColoredRectangle,
        in TText,
        in TTexture,
        in TTextureRegion,
        out TAnimationFrame
    > {
        void ApplyToNineSlice(
            TNineSlice target,
            TTexture texture
        );

        void ApplyColorToNineSlice(TNineSlice target, Color color);

        void ApplyColorToColoredRectangle(
            TColoredRectangle target,
            Color color
        );

        void ApplyColorToText(TText target, Color color);

        TAnimationFrame CreateAnimationFrame(
            TTextureRegion region,
            float lengthFrame
        );
    }
}
