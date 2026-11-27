using System.Collections.Generic;
using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Defines the instruction vocabulary understood by event sheets.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface ISheetVocabulary {
        IReadOnlyList<SheetInstructionInfo> Conditions { get; }
        IReadOnlyList<SheetInstructionInfo> Actions { get; }
        
        void RegisterCondition(SheetInstructionInfo info, SheetConditionHandler handler);
        void RegisterAction(SheetInstructionInfo info, SheetActionHandler handler);
        
        bool TryGetCondition(
            string idInstruction,
            out SheetInstructionInfo info,
            out SheetConditionHandler handler
        );
        
        bool TryGetAction(
            string idInstruction,
            out SheetInstructionInfo info,
            out SheetActionHandler handler
        );
    }
}
