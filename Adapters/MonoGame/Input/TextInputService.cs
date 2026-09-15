using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// MonoGame implementation of <see cref="ITextInputService"/>.
    /// The window raises text notifications from the platform event pump, so characters
    /// are buffered there and forwarded from <see cref="Update"/>; subscribers therefore
    /// always observe entered text on the update thread and never inside a platform callback.
    /// </summary>
    public sealed class TextInputService : ITextInputService, IDisposable {
        private readonly object _lock = new object();
        private readonly Queue<char> _queueCharacters;
        private readonly Game _game;
        private bool _flagEnabled;
        private bool _flagDisposed;

        /// <inheritdoc />
        public event EventHandler<TextEnteredEventArgs> TextEntered;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextInputService"/> class.
        /// </summary>
        /// <param name="game">The running game whose window supplies text input.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="game"/> is null.</exception>
        public TextInputService(Game game) {
            if (game == null) {
                throw new ArgumentNullException(nameof(game));
            }
            _game = game;
            _queueCharacters = new Queue<char>();
            _flagEnabled = true;
            _game.Window.TextInput += OnTextInput;
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

        private void OnTextInput(object sender, TextInputEventArgs arguments) {
            lock (_lock) {
                if (_flagDisposed || !_flagEnabled) {
                    return;
                }
                _queueCharacters.Enqueue(arguments.Character);
            }
        }

        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            char[] arrayCharacters;
            lock (_lock) {
                if (_flagDisposed || _queueCharacters.Count == 0) {
                    return;
                }
                arrayCharacters = _queueCharacters.ToArray();
                _queueCharacters.Clear();
            }
            EventHandler<TextEnteredEventArgs> handler = TextEntered;
            if (handler == null) {
                return;
            }
            for (int index = 0; index < arrayCharacters.Length; index += 1) {
                handler(this, new TextEnteredEventArgs(arrayCharacters[index]));
            }
        }

        /// <summary>
        /// Unsubscribes from the window and drops any buffered characters.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                _queueCharacters.Clear();
            }
            _game.Window.TextInput -= OnTextInput;
            GC.SuppressFinalize(this);
        }
    }
}