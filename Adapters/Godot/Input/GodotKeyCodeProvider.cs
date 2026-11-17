using Godot;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Godot.Input {
    /// <summary>
    /// The single translation table from platform-independent key codes to Godot keys.
    /// Every Godot-flavoured adapter resolves this service instead of keeping a private
    /// copy, so adding a key code cannot leave one of them behind.
    /// </summary>
    public sealed class GodotKeyCodeProvider : INativeKeyProvider<Key> {
        /// <inheritdoc />
        public Key GetNativeKey(KeyCode codeKey) {
            return KeyCodeConverter.ToGodotKey(codeKey);
        }
    }
}
