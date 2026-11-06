using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Identifies whether an instruction is evaluated or executed.
    /// </summary>
    public enum SheetInstructionCategory {
        Condition,
        Action
    }
    
    /// <summary>
    /// Describes a condition or action and the values it accepts.
    /// </summary>
    public sealed class SheetInstructionInfo {
        /// <summary>Creates instruction metadata.</summary>
        /// <param name="instructionId">The non-empty identifier used by the document.</param>
        /// <param name="label">The non-empty author-facing label; <c>{0}</c> stands for the first parameter.</param>
        /// <param name="category">Whether the instruction is a condition or an action.</param>
        /// <param name="listParameters">The listParameters the instruction accepts.</param>
        public SheetInstructionInfo(
            string instructionId,
            string label,
            SheetInstructionCategory category,
            IEnumerable<SheetParameterInfo> listParameters = null
        ) {
            if (string.IsNullOrWhiteSpace(instructionId)) {
                throw new ArgumentException("A non-empty instruction identifier is required.", nameof(instructionId));
            }
            if (string.IsNullOrWhiteSpace(label)) {
                throw new ArgumentException("A non-empty instruction label is required.", nameof(label));
            }
            InstructionId = instructionId;
            Label = label;
            Category = category;
            Parameters = CopyParameters(listParameters);
        }
        
        /// <summary>Gets the identifier used by the document.</summary>
        public string InstructionId { get; }
        
        /// <summary>Gets the author-facing label, with <c>{0}</c>-style parameter placeholders.</summary>
        public string Label { get; }
        
        /// <summary>Gets whether the instruction is a condition or an action.</summary>
        public SheetInstructionCategory Category { get; }
        
        /// <summary>Gets the listParameters the instruction accepts, in declaration order.</summary>
        public IReadOnlyList<SheetParameterInfo> Parameters { get; }
        
        private static SheetParameterInfo[] CopyParameters(IEnumerable<SheetParameterInfo> listParameters) {
            if (listParameters == null) {
                return new SheetParameterInfo[0];
            }
            List<SheetParameterInfo> infosCopy = new List<SheetParameterInfo>();
            foreach (SheetParameterInfo parameter in listParameters) {
                if (parameter == null) {
                    throw new ArgumentException("Instruction metadata cannot contain a null parameter.", nameof(listParameters));
                }
                infosCopy.Add(parameter);
            }
            return infosCopy.ToArray();
        }
    }
    
    /// <summary>
    /// A condition handler receives a read-only view over one instruction call.
    /// </summary>
    public delegate bool SheetConditionHandler(in SheetCall call);
    
    /// <summary>
    /// An action handler receives a read-only view over one instruction call.
    /// </summary>
    public delegate void SheetActionHandler(in SheetCall call);
}
