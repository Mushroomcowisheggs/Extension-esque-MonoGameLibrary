using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// A condition instruction in an event.
    /// </summary>
    public sealed class SheetCondition {
        /// <summary>Creates a condition from an instruction identifier and its listParameters.</summary>
        /// <param name="instructionId">The non-empty identifier the vocabulary registers.</param>
        /// <param name="listParameters">The instruction arguments, in the order the vocabulary declares.</param>
        public SheetCondition(string instructionId, IEnumerable<SheetValue> listParameters = null) {
            if (instructionId == null) {
                throw new ArgumentNullException(nameof(instructionId));
            }
            InstructionId = ValidateInstructionId(instructionId);
            Parameters = CopyParameters(listParameters);
        }
        
        /// <summary>Gets the identifier of the registered condition.</summary>
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
