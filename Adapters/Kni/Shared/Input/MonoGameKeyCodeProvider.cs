using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Kni.Input {
    /// <summary>
    /// The translation table from platform-independent key codes to KNI keys, registered as a service.
    /// One table in the container means a key code cannot be added while a stale copy of the mapping
    /// stays behind.
    /// </summary>
    public sealed class MonoGameKeyCodeProvider : INativeKeyProvider<Keys> {
        /// <inheritdoc />
        public Keys GetNativeKey(KeyCode codeKey) {
            return KeyCodeConverter.ToMonoGameKey(codeKey);
        }
    }
}