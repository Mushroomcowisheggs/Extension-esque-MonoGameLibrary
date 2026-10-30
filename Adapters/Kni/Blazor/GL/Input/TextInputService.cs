using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
// KNI declares TextInputEventArgs in Microsoft.Xna.Framework.Input.
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;
using MonoGameLibrary.Adapters.Kni.Input;
using MonoGameLibrary.Adapters.Kni.Lifecycle;

namespace MonoGameLibrary.Adapters.Kni.Blazor.GL.Input {
    /// <summary>
    /// KNI implementation of <see cref="ITextInputService"/>.
    /// 
    /// It buffers characters and forwards them from <see cref="Update"/>, so subscribers always
    /// observe entered text on the update thread and never inside a platform callback.
    /// 
    /// The characters cannot come from <see cref="GameWindow.TextInput"/> on this platform. KNI's
    /// Blazor build forwards key codes and nothing else -- nkast.Wasm.Dom exposes OnKeyDown, OnKeyUp
    /// and a key collection, with no character event behind them -- so the window event is subscribed
    /// because the contract has one, and is never raised in a browser. The character has to be
    /// captured where the browser actually has one, the page's keypress event, and handed to
    /// <see cref="ReportCharacter"/>.
    /// 
    /// That sink is static because this instance is created by InputModule, which the host neither
    /// owns nor can reach: the page has a callback, not an object reference, so the reference has to
    /// be the type itself.
    /// </summary>
    public sealed class TextInputService : ITextInputService, IDisposable {
        private readonly object _lock = new object();
        private readonly Queue<char> _queueCharacters;
        
        /// <summary>Characters the host reported, waiting to be drained on the update thread.</summary>
        private static readonly object _lockReported = new object();
        private static readonly Queue<char> _reported = new Queue<char>();
        
        /// <summary>
        /// Reusable drain buffer. The queue is emptied every frame, so allocating a fresh array here
        /// allocated on every frame that carried input; the buffer grows to the largest burst seen and
        /// is then reused, which is what removes the per-frame allocation rather than the array itself.
        /// </summary>
        private char[] _bufferCharacters = new char[0];
        private readonly Game _game;
        private bool _flagEnabled;
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public event EventHandler<TextEnteredEventArgs> TextEnteredHandler;
        
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
        
        /// <summary>
        /// Reports one character typed in the page.
        ///
        /// The host calls this from its keyboard handler, once per character. It only enqueues; the
        /// character reaches subscribers from the update thread, in the frame after it was typed,
        /// which is the same contract the platform event would have had. A character typed before the
        /// game exists is therefore kept until the first frame drains it.
        /// </summary>
        /// <param name="character">The character the page captured.</param>
        public static void ReportCharacter(char character) {
            lock (_lockReported) {
                _reported.Enqueue(character);
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
            CollectReportedCharacters();
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
        /// Moves reported characters into this instance's queue.
        ///
        /// A disabled or disposed service drops them instead of holding them: a character typed into
        /// a surface that is not accepting input must not arrive later, in some other frame, as if it
        /// had just been typed.
        /// </summary>
        private void CollectReportedCharacters() {
            while (true) {
                char character;
                lock (_lockReported) {
                    if (_reported.Count == 0) {
                        return;
                    }
                    character = _reported.Dequeue();
                }
                lock (_lock) {
                    if (_flagDisposed) {
                        return;
                    }
                    if (!_flagEnabled) {
                        continue;
                    }
                    _queueCharacters.Enqueue(character);
                }
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
            lock (_lockReported) {
                _reported.Clear();
            }
            _game.Window.TextInput -= OnTextInput;
            GC.SuppressFinalize(this);
        }
    }
}
