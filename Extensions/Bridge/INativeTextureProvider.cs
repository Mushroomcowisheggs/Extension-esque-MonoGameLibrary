namespace MonoGameLibrary.Extensions.Bridge {
    /// <summary>
    /// Exposes a backend-native texture through the neutral cross-adapter
    /// contract anchor without naming any adapter implementation.
    /// </summary>
    /// <typeparam name="TTexture">The native texture type.</typeparam>
    public interface INativeTextureProvider<out TTexture> {
        /// <summary>Returns the wrapped backend-native texture.</summary>
        TTexture GetNativeTexture();
    }
}
