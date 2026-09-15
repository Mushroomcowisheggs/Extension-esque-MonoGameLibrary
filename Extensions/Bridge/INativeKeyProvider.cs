using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Extensions.Bridge {
    /// <summary>
    /// Exposes the key type of a backend through the neutral cross-adapter contract anchor
    /// without naming that backend in Extensions. Adapters that need to translate a
    /// platform-independent key code, such as the Gum integration hosted by MonoGame,
    /// depend on this contract instead of on the adapter that owns the translation table.
    /// </summary>
    /// <typeparam name="TKey">The backend key type.</typeparam>
    public interface INativeKeyProvider<out TKey> {
        /// <summary>Returns the backend key that corresponds to the supplied key code.</summary>
        /// <param name="codeKey">The platform-independent key code.</param>
        /// <returns>The backend key, or the backend's "no key" value when there is no match.</returns>
        TKey GetNativeKey(KeyCode codeKey);
    }
}