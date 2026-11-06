using System;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// The compiled, reusable delegate tree for one document.
    /// </summary>
    internal sealed class SheetProgram {
        private readonly CompiledEvent[] _events;
        
        internal SheetProgram(CompiledEvent[] eventsCompiled) {
            if (eventsCompiled == null) {
                throw new ArgumentNullException(nameof(eventsCompiled));
            }
            _events = eventsCompiled;
        }
        
        internal void Execute(SheetContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            for (int i = 0; i < _events.Length; i += 1) {
                _events[i].Execute(context, true);
            }
        }
    }
}
