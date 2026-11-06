using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Defines the instruction vocabulary understood by event sheets.
    /// </summary>
    public interface ISheetVocabulary {
        IReadOnlyList<SheetInstructionInfo> Conditions { get; }
        IReadOnlyList<SheetInstructionInfo> Actions { get; }
        
        void RegisterCondition(SheetInstructionInfo info, SheetConditionHandler handler);
        void RegisterAction(SheetInstructionInfo info, SheetActionHandler handler);
        
        bool TryGetCondition(
            string instructionId,
            out SheetInstructionInfo info,
            out SheetConditionHandler handler
        );
        
        bool TryGetAction(
            string instructionId,
            out SheetInstructionInfo info,
            out SheetActionHandler handler
        );
    }
}
