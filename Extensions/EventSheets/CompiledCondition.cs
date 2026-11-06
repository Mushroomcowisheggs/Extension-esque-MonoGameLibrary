using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// One condition resolved to its vocabulary handler and parameter values.
    /// </summary>
    internal sealed class CompiledCondition {
        private readonly SheetConditionHandler _handler;
        private readonly IReadOnlyList<SheetValue> _listParameters;
        
        internal CompiledCondition(
            SheetConditionHandler handler,
            IReadOnlyList<SheetValue> listParameters
        ) {
            _handler = handler;
            _listParameters = listParameters;
        }
        
        internal bool Evaluate(SheetContext context) {
            SheetCall call = new SheetCall(context, _listParameters);
            return _handler(in call);
        }
    }
}
