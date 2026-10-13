using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Adapters.Gum.MonoGame.Bridge {
    /// <summary>
    /// Applies platform-neutral graphics values to Gum's MonoGame runtime types. The
    /// contract lives with the Gum adapter that both implements and consumes it; the
    /// generic target types keep it free of concrete Gum and rendering-backend types,
    /// so a composition root can substitute another implementation.
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
