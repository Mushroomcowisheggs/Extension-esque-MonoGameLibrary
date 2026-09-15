using System;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>
    /// Applies platform-neutral key codes to Gum's MonoGame input types.
    /// The key translation is supplied by the MonoGame adapter through
    /// <see cref="INativeKeyProvider{TKey}"/> so this assembly keeps no private key table.
    /// </summary>
    public sealed class MonoGameGumInputBridge :
        IGumInputBridge<KeyEventArgs, KeyCode> {
        private readonly INativeKeyProvider<Keys> _providerKey;
        
        /// <summary>Initializes a new instance of the <see cref="MonoGameGumInputBridge"/> class.</summary>
        /// <param name="providerKey">The MonoGame key translation table.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="providerKey"/> is null.</exception>
        public MonoGameGumInputBridge(INativeKeyProvider<Keys> providerKey) {
            if (providerKey == null) {
                throw new ArgumentNullException(nameof(providerKey));
            }
            _providerKey = providerKey;
        }
        
        /// <inheritdoc />
        public bool IsKeyPressed(KeyEventArgs arguments, KeyCode codeKey) {
            if (arguments == null) {
                throw new ArgumentNullException(nameof(arguments));
            }
            return arguments.Key == _providerKey.GetNativeKey(codeKey);
        }
    }
}