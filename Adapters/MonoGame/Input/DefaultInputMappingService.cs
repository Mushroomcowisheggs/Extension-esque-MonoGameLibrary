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
        
        public DefaultInputMappingService(IInputService serviceInput) {
            if (serviceInput == null) {
                throw new ArgumentNullException(nameof(serviceInput));
            }
            _serviceInput = serviceInput;
            _dictionaryKeyBindings = new Dictionary<Enum, List<KeyCode>>();
            _dictionaryButtonBindings = new Dictionary<Enum, List<(PlayerIndex, GamePadButton)>>();
        }
        
        public void BindKey<T>(T action, KeyCode code) where T : Enum {
            if (!_dictionaryKeyBindings.TryGetValue(action, out var keys)) {
                keys = new List<KeyCode>();
                _dictionaryKeyBindings[action] = keys;
            }
            if (!keys.Contains(code)) {
                keys.Add(code);
            }
        }
        
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
        
        public void Update(FrameTime timeFrame) {
            // No state to update; all queries are forwarded to IInputService.
            // This method is kept for future extensibility (e.g., debouncing, auto-repeat).
        }
    }
}
