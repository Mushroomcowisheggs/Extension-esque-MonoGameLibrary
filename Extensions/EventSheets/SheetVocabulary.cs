using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Thread-safe instruction catalog for one event-sheet runtime.
    /// </summary>
    public sealed class SheetVocabulary : ISheetVocabulary {
        private readonly object _lock = new object();
        private readonly Dictionary<string, ConditionRegistration> _conditions =
            new Dictionary<string, ConditionRegistration>(StringComparer.Ordinal);
        private readonly Dictionary<string, ActionRegistration> _actions =
            new Dictionary<string, ActionRegistration>(StringComparer.Ordinal);
        
        public IReadOnlyList<SheetInstructionInfo> Conditions {
            get {
                lock (_lock) {
                    List<SheetInstructionInfo> infosResult = new List<SheetInstructionInfo>();
                    foreach (ConditionRegistration registration in _conditions.Values) {
                        infosResult.Add(registration.Info);
                    }
                    return infosResult.ToArray();
                }
            }
        }
        
        public IReadOnlyList<SheetInstructionInfo> Actions {
            get {
                lock (_lock) {
                    List<SheetInstructionInfo> infosResult = new List<SheetInstructionInfo>();
                    foreach (ActionRegistration registration in _actions.Values) {
                        infosResult.Add(registration.Info);
                    }
                    return infosResult.ToArray();
                }
            }
        }
        
        public void RegisterCondition(SheetInstructionInfo info, SheetConditionHandler handler) {
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }
            if (info.Category != SheetInstructionCategory.Condition) {
                throw new ArgumentException("The instruction metadata is not a condition.", nameof(info));
            }
            lock (_lock) {
                if (_conditions.ContainsKey(info.InstructionId)) {
                    throw new InvalidOperationException(
                        "Condition '" + info.InstructionId + "' is already registered."
                    );
                }
                _conditions.Add(info.InstructionId, new ConditionRegistration(info, handler));
            }
        }
        
        public void RegisterAction(SheetInstructionInfo info, SheetActionHandler handler) {
            if (info == null) {
                throw new ArgumentNullException(nameof(info));
            }
            if (handler == null) {
                throw new ArgumentNullException(nameof(handler));
            }
            if (info.Category != SheetInstructionCategory.Action) {
                throw new ArgumentException("The instruction metadata is not an action.", nameof(info));
            }
            lock (_lock) {
                if (_actions.ContainsKey(info.InstructionId)) {
                    throw new InvalidOperationException(
                        "Action '" + info.InstructionId + "' is already registered."
                    );
                }
                _actions.Add(info.InstructionId, new ActionRegistration(info, handler));
            }
        }
        
        public bool TryGetCondition(
            string instructionId,
            out SheetInstructionInfo info,
            out SheetConditionHandler handler
        ) {
            if (string.IsNullOrWhiteSpace(instructionId)) {
                info = null;
                handler = null;
                return false;
            }
            lock (_lock) {
                ConditionRegistration registration;
                if (_conditions.TryGetValue(instructionId, out registration)) {
                    info = registration.Info;
                    handler = registration.Handler;
                    return true;
                }
            }
            info = null;
            handler = null;
            return false;
        }
        
        public bool TryGetAction(
            string instructionId,
            out SheetInstructionInfo info,
            out SheetActionHandler handler
        ) {
            if (string.IsNullOrWhiteSpace(instructionId)) {
                info = null;
                handler = null;
                return false;
            }
            lock (_lock) {
                ActionRegistration registration;
                if (_actions.TryGetValue(instructionId, out registration)) {
                    info = registration.Info;
                    handler = registration.Handler;
                    return true;
                }
            }
            info = null;
            handler = null;
            return false;
        }
        
        private readonly struct ConditionRegistration {
            internal ConditionRegistration(SheetInstructionInfo info, SheetConditionHandler handler) {
                if (info == null) {
                    throw new ArgumentNullException(nameof(info));
                }
                if (handler == null) {
                    throw new ArgumentNullException(nameof(handler));
                }
                Info = info;
                Handler = handler;
            }
            
            internal SheetInstructionInfo Info { get; }
            internal SheetConditionHandler Handler { get; }
        }
        
        private readonly struct ActionRegistration {
            internal ActionRegistration(SheetInstructionInfo info, SheetActionHandler handler) {
                if (info == null) {
                    throw new ArgumentNullException(nameof(info));
                }
                if (handler == null) {
                    throw new ArgumentNullException(nameof(handler));
                }
                Info = info;
                Handler = handler;
            }
            
            internal SheetInstructionInfo Info { get; }
            internal SheetActionHandler Handler { get; }
        }
    }
}
