using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// The single translation table from platform-independent key codes to MonoGame keys.
    /// Every MonoGame-flavoured adapter resolves this service instead of keeping a private
    /// copy, so adding a key code cannot leave one of them behind.
    /// </summary>
    public sealed class MonoGameKeyCodeProvider : INativeKeyProvider<Keys> {
        /// <inheritdoc />
        public Keys GetNativeKey(KeyCode codeKey) {
            return KeyCodeConverter.ToMonoGameKey(codeKey);
        }
    }
}