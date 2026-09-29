using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.States {
    /// <summary>
    /// Default implementation of <see cref="IStateService"/> using a stack.
    /// </summary>
    /// <remarks>
    /// Thread safety: every member is guarded by a single lock, so states may be pushed, popped
    /// and changed from any thread while <see cref="Update"/> runs on another. State lifecycle
    /// callbacks (<c>Enter</c>, <c>Exit</c>, <c>Update</c>) run while that lock is held, so a
    /// callback must not block waiting on a thread that calls this service.
    /// </remarks>
    public sealed class StateService : IStateService {
        private readonly object _lock = new object();
        private readonly List<IState> _states = new List<IState>();
        
        /// <inheritdoc />
        public IState CurrentState {
            get {
                lock (_lock) {
                    if (_states.Count > 0) {
                        return _states[_states.Count - 1];
                    }
                    return null;
                }
            }
        }
        
        /// <inheritdoc />
        public void Push(IState state) {
            if (state == null) {
                throw new ArgumentNullException(nameof(state));
            }
            
            lock (_lock) {
                // Suspend current state
                if (_states.Count > 0) {
                    _states[_states.Count - 1].Exit();
                }
                
                _states.Add(state);
                state.Enter();
            }
        }
        
        /// <inheritdoc />
        public void Pop() {
            lock (_lock) {
                if (_states.Count == 0) {
                    return;
                }
                
                IState stateTop = _states[_states.Count - 1];
                stateTop.Exit();
                _states.RemoveAt(_states.Count - 1);
                
                // Resume previous state
                if (_states.Count > 0) {
                    _states[_states.Count - 1].Enter();
                }
            }
        }
        
        /// <inheritdoc />
        public void Change(IState state) {
            if (state == null) {
                throw new ArgumentNullException(nameof(state));
            }
            
            lock (_lock) {
                // Exit all states
                for (int i = _states.Count - 1; i >= 0; i -= 1) {
                    _states[i].Exit();
                }
                
                _states.Clear();
                _states.Add(state);
                state.Enter();
            }
        }
        
        /// <inheritdoc />
        public void Update(FrameTime timeFrame) {
            lock (_lock) {
                if (_states.Count > 0) {
                    // Update only the topmost state (no transparency concept here)
                    _states[_states.Count - 1].Update(timeFrame);
                }
            }
        }
    }
}
