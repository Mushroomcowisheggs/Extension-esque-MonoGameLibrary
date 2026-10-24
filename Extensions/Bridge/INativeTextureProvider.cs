namespace MonoGameLibrary.Extensions.Bridge {
    /// <summary>
    /// Exposes a backend-native texture through the neutral cross-adapter
    /// contract anchor without naming any adapter implementation.
    /// </summary>
    /// <typeparam name="TTexture">The native texture type.</typeparam>
    public interface INativeTextureProvider<out TTexture> {
        /// <summary>
        /// Returns the wrapped backend-native texture, or <c>null</c> when this provider no longer holds
        /// one because it was released.
        /// </summary>
        /// <remarks>
        /// The type test that reaches this member establishes the capability, not the value. A consumer in
        /// another adapter must test the result before using it: the failure of a released adapter has to
        /// surface at the call site rather than inside the backend.
        /// </remarks>
        TTexture GetNativeTexture();
    }
}
