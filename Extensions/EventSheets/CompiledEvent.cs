using System;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// One compiled event: conditions, actions, sub-events, and the trigger-once state.
    /// </summary>
    internal sealed class CompiledEvent {
        private readonly CompiledCondition[] _conditions;
        private readonly CompiledAction[] _actions;
        private readonly CompiledEvent[] _eventsSub;
        private readonly bool _flagOnce;
        private bool _flagOnceConsumed;
        
        internal CompiledEvent(
            CompiledCondition[] conditionsCompiled,
            CompiledAction[] actionsCompiled,
            CompiledEvent[] eventsSub,
            bool flagOnce
        ) {
            if (conditionsCompiled == null) {
                throw new ArgumentNullException(nameof(conditionsCompiled));
            }
            if (actionsCompiled == null) {
                throw new ArgumentNullException(nameof(actionsCompiled));
            }
            if (eventsSub == null) {
                throw new ArgumentNullException(nameof(eventsSub));
            }
            _conditions = conditionsCompiled;
            _actions = actionsCompiled;
            _eventsSub = eventsSub;
            _flagOnce = flagOnce;
        }
        
        internal void Execute(SheetContext context, bool flagRootEvent) {
            if (flagRootEvent) {
                context.BeginRootEvent();
            } else {
                context.BeginSubEvent();
            }
            try {
                ExecuteInScope(context);
            } finally {
                context.EndEvent(flagRootEvent);
            }
        }
        
        private void ExecuteInScope(SheetContext context) {
            bool flagConditions = true;
            for (int i = 0; i < _conditions.Length; i += 1) {
                if (!_conditions[i].Evaluate(context)) {
                    flagConditions = false;
                    break;
                }
            }
            if (!flagConditions) {
                _flagOnceConsumed = false;
                return;
            }
            if (_flagOnce && _flagOnceConsumed) {
                return;
            }
            
            for (int i = 0; i < _actions.Length; i += 1) {
                _actions[i].Execute(context);
            }
            for (int i = 0; i < _eventsSub.Length; i += 1) {
                _eventsSub[i].Execute(context, false);
            }
            if (_flagOnce) {
                _flagOnceConsumed = true;
            }
        }
    }
}
