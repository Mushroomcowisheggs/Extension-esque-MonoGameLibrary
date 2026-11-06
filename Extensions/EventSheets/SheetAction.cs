using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// An action instruction in an event.
    /// </summary>
    public sealed class SheetAction {
        /// <summary>Creates an action from an instruction identifier and its listParameters.</summary>
        /// <param name="instructionId">The non-empty identifier the vocabulary registers.</param>
        /// <param name="listParameters">The instruction arguments, in the order the vocabulary declares.</param>
        public SheetAction(string instructionId, IEnumerable<SheetValue> listParameters = null) {
            if (instructionId == null) {
                throw new ArgumentNullException(nameof(instructionId));
            }
            InstructionId = ValidateInstructionId(instructionId);
            Parameters = CopyParameters(listParameters);
        }
        
        /// <summary>Gets the identifier of the registered action.</summary>
        public string InstructionId { get; }
        
        /// <summary>Gets the instruction arguments, in declaration order.</summary>
        public IReadOnlyList<SheetValue> Parameters { get; }
        
        private static string ValidateInstructionId(string instructionId) {
            if (string.IsNullOrWhiteSpace(instructionId)) {
                throw new ArgumentException("A non-empty instruction identifier is required.", nameof(instructionId));
            }
            return instructionId;
        }
        
        private static SheetValue[] CopyParameters(IEnumerable<SheetValue> listParameters) {
            if (listParameters == null) {
                return new SheetValue[0];
            }
            return new List<SheetValue>(listParameters).ToArray();
        }
    }
}
