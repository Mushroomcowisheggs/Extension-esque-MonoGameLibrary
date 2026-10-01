namespace MonoGameLibrary.Extensions.Input {
    /// <summary>
    /// Exposes a backend's key type through an Input-owned contract, so an adapter that needs
    /// to translate a platform-independent key code, such as the Gum integration hosted by
    /// MonoGame, depends on this contract instead of on the adapter that owns the translation
    /// table. The translation table stays the single responsibility of the backend adapter.
    /// </summary>
    /// <typeparam name="TKey">The backend key type.</typeparam>
    public interface INativeKeyProvider<out TKey> {
        /// <summary>Returns the backend key that corresponds to the supplied key code.</summary>
        /// <param name="codeKey">The platform-independent key code.</param>
        /// <returns>The backend key, or the backend's "no key" value when there is no match.</returns>
        TKey GetNativeKey(KeyCode codeKey);
    }
}
