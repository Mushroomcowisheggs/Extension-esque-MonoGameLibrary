using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Fna.Input {
    /// <summary>
    /// FNA implementation of <see cref="ITextInputService"/>.
    ///
    /// XNA4 puts no text input on <see cref="GameWindow"/>, and FNA keeps that surface: what it
    /// offers instead is <see cref="TextInputEXT"/>, a direct binding to SDL's text events, which also
    /// has to be switched on before the platform raises anything at all. So this service starts text
    /// input with the game, buffers what SDL reports, stops it again on dispose, and forwards the
    /// characters from <see cref="Update"/>; subscribers still never observe text inside a platform
    /// callback.
    /// </summary>
    public sealed class TextInputService : ITextInputService, IDisposable {
        private readonly object _lock = new object();
        private readonly Queue<char> _queueCharacters;
        
        /// <summary>
        /// Reusable drain buffer. The queue is emptied every frame, so allocating a fresh array here
        /// allocated on every frame that carried input; the buffer grows to the largest burst seen and
        /// is then reused, which is what removes the per-frame allocation rather than the array itself.
        /// </summary>
        private char[] _bufferCharacters = new char[0];
        private bool _flagSubscribed;
        private bool _flagEnabled;
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public event EventHandler<TextEnteredEventArgs> TextEnteredHandler;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TextInputService"/> class.
        /// </summary>
        /// <param name="game">The running game this service belongs to.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
        public TextInputService(Game game) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            _queueCharacters = new Queue<char>();
            _flagEnabled = true;
            TextInputEXT.TextInput += OnTextInput;
            TextInputEXT.StartTextInput();
            _flagSubscribed = true;
        }
        
        /// <inheritdoc />
        public bool IsEnabled {
            get {
                lock (_lock) {
                    return _flagEnabled;
                }
            } set {
                lock (_lock) {
                    _flagEnabled = value;
                    if (!value) {
                        _queueCharacters.Clear();
                    }
                }
            }
        }
        
        private void OnTextInput(char character) {
            lock (_lock) {
                if (_flagDisposed || !_flagEnabled) {
                    return;
                }
                _queueCharacters.Enqueue(character);
            }
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            int countCharacters;
            lock (_lock) {
                if (_flagDisposed || _queueCharacters.Count == 0) {
                    return;
                }
                countCharacters = _queueCharacters.Count;
                if (_bufferCharacters.Length < countCharacters) {
                    _bufferCharacters = new char[countCharacters];
                }
                _queueCharacters.CopyTo(_bufferCharacters, 0);
                _queueCharacters.Clear();
            }
            EventHandler<TextEnteredEventArgs> handler = TextEnteredHandler;
            if (handler == null) {
                return;
            }
            for (int index = 0; index < countCharacters; index += 1) {
                handler(this, new TextEnteredEventArgs(_bufferCharacters[index]));
            }
        }
        
        /// <summary>
        /// Unsubscribes from the platform and turns text input off again.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                _queueCharacters.Clear();
            }
            if (_flagSubscribed) {
                TextInputEXT.TextInput -= OnTextInput;
                TextInputEXT.StopTextInput();
                _flagSubscribed = false;
            }
            GC.SuppressFinalize(this);
        }
    }
}
