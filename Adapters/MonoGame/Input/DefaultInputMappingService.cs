using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Core.Time;
using MonoGameLibrary.Extensions.Input;

namespace MonoGameLibrary.Adapters.MonoGame.Input {
    /// <summary>
    /// Default implementation of <see cref="IInputMappingService"/>.
    /// </summary>
    public sealed class DefaultInputMappingService : IInputMappingService {
        private readonly IInputService _serviceInput;
        private readonly Dictionary<Enum, List<KeyCode>> _dictionaryKeyBindings;
        private readonly Dictionary<Enum, List<(PlayerIndex Player, GamePadButton Button)>> _dictionaryButtonBindings;
        
        /// <summary>Initializes the service with no bindings.</summary>
        /// <param name="serviceInput">The input service every query is forwarded to.</param>
        public DefaultInputMappingService(IInputService serviceInput) {
            if (serviceInput == null) {
                throw new ArgumentNullException(nameof(serviceInput));
            }
            _serviceInput = serviceInput;
            _dictionaryKeyBindings = new Dictionary<Enum, List<KeyCode>>();
            _dictionaryButtonBindings = new Dictionary<Enum, List<(PlayerIndex, GamePadButton)>>();
        }
        
        /// <summary>Binds a key to an action. Binding the same key twice is a no-op.</summary>
        /// <param name="action">The action the key triggers.</param>
        /// <param name="code">The key to bind.</param>
        public void BindKey<T>(T action, KeyCode code) where T : Enum {
            if (!_dictionaryKeyBindings.TryGetValue(action, out var keys)) {
                keys = new List<KeyCode>();
                _dictionaryKeyBindings[action] = keys;
            }
            if (!keys.Contains(code)) {
                keys.Add(code);
            }
        }
        
        /// <summary>Binds a gamepad button to an action. Binding the same pair twice is a no-op.</summary>
        /// <param name="action">The action the button triggers.</param>
        /// <param name="indexPlayer">The player whose controller is read.</param>
        /// <param name="button">The button to bind.</param>
        public void BindButton<T>(T action, PlayerIndex indexPlayer, GamePadButton button) where T : Enum {
            if (!_dictionaryButtonBindings.TryGetValue(action, out var buttons)) {
                buttons = new List<(PlayerIndex, GamePadButton)>();
                _dictionaryButtonBindings[action] = buttons;
            }
            var tuple = (indexPlayer, button);
            if (!buttons.Contains(tuple)) {
                buttons.Add(tuple);
            }
        }
        
        /// <summary>Returns whether any key or button bound to the action was pressed this frame.</summary>
        /// <param name="action">The action to test.</param>
        public bool IsActionPressed<T>(T action) where T : Enum {
            if (_dictionaryKeyBindings.TryGetValue(action, out var keys)) {
                foreach (var key in keys) {
                    if (_serviceInput.WasKeyJustPressed(key)) {
                        return true;
                    }
                }
            }
            if (_dictionaryButtonBindings.TryGetValue(action, out var buttons)) {
                foreach (var (indexPlayer, button) in buttons) {
                    if (_serviceInput.WasButtonJustPressed(indexPlayer, button)) {
                        return true;
                    }
                }
            }
            return false;
        }
        
        /// <summary>Returns whether any key or button bound to the action is currently held.</summary>
        /// <param name="action">The action to test.</param>
        public bool IsActionHeld<T>(T action) where T : Enum {
            if (_dictionaryKeyBindings.TryGetValue(action, out var keys)) {
                foreach (var key in keys) {
                    if (_serviceInput.IsKeyDown(key)) {
                        return true;
                    }
                }
            }
            if (_dictionaryButtonBindings.TryGetValue(action, out var buttons)) {
                foreach (var (indexPlayer, button) in buttons) {
                    if (_serviceInput.IsButtonDown(indexPlayer, button)) {
                        return true;
                    }
                }
            }
            return false;
        }
        
        /// <summary>Returns a normalised direction from four held actions, or the zero vector when none is held.</summary>
        /// <param name="up">The action that decreases Y.</param>
        /// <param name="down">The action that increases Y.</param>
        /// <param name="left">The action that decreases X.</param>
        /// <param name="right">The action that increases X.</param>
        public TwoDimensionalVector GetActionDirection<T>(T up, T down, T left, T right) where T : Enum {
            TwoDimensionalVector direction = TwoDimensionalVector.Zero;
            if (IsActionHeld(up)) {
                direction.Y -= 1f;
            }
            if (IsActionHeld(down)) {
                direction.Y += 1f;
            }
            if (IsActionHeld(left)) {
                direction.X -= 1f;
            }
            if (IsActionHeld(right)) {
                direction.X += 1f;
            }
            if (direction.LengthSquared() > 0f) {
                direction = TwoDimensionalVector.Normalize(direction);
            }
            return direction;
        }
        
        /// <summary>Does nothing. Queries are forwarded to the input service, so there is no cached state to advance; the method exists for the lifecycle contract.</summary>
        /// <param name="timeFrame">Unused.</param>
        public void Update(FrameTime timeFrame) {
            // No state to update; all queries are forwarded to IInputService.
            // This method is kept for future extensibility (e.g., debouncing, auto-repeat).
        }
    }
}
