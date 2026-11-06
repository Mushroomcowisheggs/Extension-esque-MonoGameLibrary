using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// One action resolved to its vocabulary handler and parameter values.
    /// </summary>
    internal sealed class CompiledAction {
        private readonly SheetActionHandler _handler;
        private readonly IReadOnlyList<SheetValue> _listParameters;
        
        internal CompiledAction(
            SheetActionHandler handler,
            IReadOnlyList<SheetValue> listParameters
        ) {
            _handler = handler;
            _listParameters = listParameters;
        }
        
        internal void Execute(SheetContext context) {
            SheetCall call = new SheetCall(context, _listParameters);
            _handler(in call);
        }
    }
}
