using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Executes one compiled sheet against one context once per frame.
    /// </summary>
    internal sealed class SheetRuntime {
        private readonly SheetProgram _program;
        private readonly SheetContext _context;
        private long _numberFrame;
        
        internal SheetRuntime(SheetProgram program, SheetContext context) {
            if (program == null) {
                throw new ArgumentNullException(nameof(program));
            }
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            _program = program;
            _context = context;
        }
        
        internal SheetContext Context {
            get { return _context; }
        }
        
        internal long FrameNumber {
            get { return _numberFrame; }
        }
        
        internal void Update(FrameTime timeFrame) {
            _numberFrame += 1;
            _context.BeginFrame(timeFrame, _numberFrame);
            _program.Execute(_context);
        }
    }
}
