using System;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Describes the value accepted by an instruction parameter.
    /// </summary>
    public enum SheetParameterKind {
        Any,
        Number,
        Text,
        Boolean,
        Object,
        Variable,
        Property
    }
    
    /// <summary>
    /// Metadata used by editors, validators, and renderers for one parameter.
    /// </summary>
    public sealed class SheetParameterInfo {
        /// <summary>Creates parameter metadata.</summary>
        /// <param name="nameParameter">The non-empty author-facing parameter name.</param>
        /// <param name="kind">The value kind the parameter accepts.</param>
        /// <param name="flagOptional">True when a document may omit the parameter.</param>
        public SheetParameterInfo(
            string nameParameter,
            SheetParameterKind kind,
            bool flagOptional = false
        ) {
            if (string.IsNullOrWhiteSpace(nameParameter)) {
                throw new ArgumentException("A non-empty parameter name is required.", nameof(nameParameter));
            }
            Name = nameParameter;
            Kind = kind;
            Optional = flagOptional;
        }
        
        /// <summary>Gets the author-facing parameter name.</summary>
        public string Name { get; }
        
        /// <summary>Gets the value kind the parameter accepts.</summary>
        public SheetParameterKind Kind { get; }
        
        /// <summary>Gets whether a document may omit the parameter.</summary>
        public bool Optional { get; }
    }
}
