using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Bridge {
    /// <summary>
    /// Applies platform-neutral graphics values to a Gum runtime. The contract is an
    /// adapter-facing bridge in the Extensions.Bridge anchor; the generic target types keep
    /// it free of concrete Gum and rendering-backend types, so an adapter can substitute its
    /// implementation.
    /// </summary>
    public interface ITextureBridge<
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
