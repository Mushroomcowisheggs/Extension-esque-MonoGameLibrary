using System;
using System.Collections.Generic;
using Godot;
using GodotWindow = global::Godot.Window;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.Godot.Input {
    /// <summary>
    /// Godot implementation of <see cref="ITextInputService"/>.
    /// Godot has no dedicated text signal: characters arrive as the Unicode value of
    /// the key events the window receives, so those events are filtered here and the
    /// characters are buffered and forwarded from <see cref="Update"/>; subscribers
    /// therefore always observe entered text on the update thread and never inside a
    /// platform callback. Key echoes carry the platform auto-repeat and are reported
    /// as entered characters, which matches how the window system repeats text.
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
        private readonly GodotWindow _window;
        private bool _flagEnabled;
        private bool _flagDisposed;
        
        /// <inheritdoc />
        public event EventHandler<TextEnteredEventArgs> TextEnteredHandler;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TextInputService"/> class.
        /// </summary>
        /// <param name="window">The running game window whose input signal supplies text input.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="window"/> is null.</exception>
        public TextInputService(GodotWindow window) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }
            _window = window;
            _queueCharacters = new Queue<char>();
            _flagEnabled = true;
            _window.WindowInput += OnWindowInput;
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
        
        private void OnWindowInput(InputEvent eventInput) {
            InputEventKey eventKey = eventInput as InputEventKey;
            if (eventKey == null || !eventKey.Pressed) {
                return;
            }
            long codePoint = eventKey.Unicode;
            if (codePoint == 0) {
                return;
            }
            lock (_lock) {
                if (_flagDisposed || !_flagEnabled) {
                    return;
                }
                if (codePoint <= char.MaxValue) {
                    _queueCharacters.Enqueue((char)codePoint);
                } else {
                    // Outside the basic multilingual plane the character arrives as a
                    // surrogate pair, and the contract carries one UTF-16 unit per event.
                    string text = char.ConvertFromUtf32((int)codePoint);
                    for (int index = 0; index < text.Length; index += 1) {
                        _queueCharacters.Enqueue(text[index]);
                    }
                }
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
        /// Unsubscribes from the window input signal and drops any buffered characters.
        /// </summary>
        public void Dispose() {
            lock (_lock) {
                if (_flagDisposed) {
                    return;
                }
                _flagDisposed = true;
                _queueCharacters.Clear();
            }
            _window.WindowInput -= OnWindowInput;
            GC.SuppressFinalize(this);
        }
    }
}
