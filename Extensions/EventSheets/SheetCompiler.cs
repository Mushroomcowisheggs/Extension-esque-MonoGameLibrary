using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Resolves a document's instruction identifiers into a compact delegate tree.
    /// </summary>
    internal sealed class SheetCompiler {
        private readonly ISheetVocabulary _vocabulary;
        
        internal SheetCompiler(ISheetVocabulary vocabulary) {
            if (vocabulary == null) {
                throw new ArgumentNullException(nameof(vocabulary));
            }
            _vocabulary = vocabulary;
        }
        
        internal SheetProgram Compile(SheetDocument document) {
            if (document == null) {
                throw new ArgumentNullException(nameof(document));
            }
            SheetValidator.Validate(document, _vocabulary);
            List<CompiledEvent> events = new List<CompiledEvent>();
            for (int i = 0; i < document.Events.Count; i += 1) {
                events.Add(CompileEvent(document.Events[i]));
            }
            return new SheetProgram(events.ToArray());
        }
        
        private CompiledEvent CompileEvent(SheetEvent sheetEvent) {
            List<CompiledCondition> conditions = new List<CompiledCondition>();
            for (int i = 0; i < sheetEvent.Conditions.Count; i += 1) {
                SheetCondition condition = sheetEvent.Conditions[i];
                SheetInstructionInfo info;
                SheetConditionHandler handler;
                _vocabulary.TryGetCondition(condition.InstructionId, out info, out handler);
                conditions.Add(new CompiledCondition(handler, condition.Parameters));
            }
            
            List<CompiledAction> actions = new List<CompiledAction>();
            for (int i = 0; i < sheetEvent.Actions.Count; i += 1) {
                SheetAction action = sheetEvent.Actions[i];
                SheetInstructionInfo info;
                SheetActionHandler handler;
                _vocabulary.TryGetAction(action.InstructionId, out info, out handler);
                actions.Add(new CompiledAction(handler, action.Parameters));
            }
            
            List<CompiledEvent> eventsSub = new List<CompiledEvent>();
            for (int i = 0; i < sheetEvent.SubEvents.Count; i += 1) {
                eventsSub.Add(CompileEvent(sheetEvent.SubEvents[i]));
            }
            
            return new CompiledEvent(
                conditions.ToArray(),
                actions.ToArray(),
                eventsSub.ToArray(),
                sheetEvent.Once
            );
        }
    }
}
