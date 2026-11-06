using System;
using System.Collections.Generic;
using System.Globalization;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// A non-boxing parameter view passed to an instruction handler.
    /// </summary>
    public readonly struct SheetCall {
        private readonly IReadOnlyList<SheetValue> _listParameters;
        
        /// <summary>Creates a call view over one instruction's listParameters.</summary>
        /// <param name="context">The frame context the instruction runs in.</param>
        /// <param name="listParameters">The instruction listParameters.</param>
        public SheetCall(SheetContext context, IReadOnlyList<SheetValue> listParameters) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }
            if (listParameters == null) {
                throw new ArgumentNullException(nameof(listParameters));
            }
            Context = context;
            _listParameters = listParameters;
        }
        
        /// <summary>Gets the frame context the instruction runs in.</summary>
        public SheetContext Context { get; }
        
        /// <summary>Gets the raw listParameters of the instruction call.</summary>
        public IReadOnlyList<SheetValue> Parameters {
            get { return _listParameters; }
        }
        
        /// <summary>Gets the number of listParameters in the call.</summary>
        public int Count {
            get { return _listParameters.Count; }
        }
        
        /// <summary>Gets one raw parameter by position.</summary>
        /// <param name="index">The zero-based parameter position.</param>
        /// <returns>The parameter value.</returns>
        public SheetValue this[int index] {
            get { return _listParameters[index]; }
        }
        
        /// <summary>Resolves one parameter as a number.</summary>
        /// <param name="index">The zero-based parameter position.</param>
        /// <returns>The resolved number.</returns>
        public double GetNumber(int index) {
            SheetValue value = this[index];
            double number;
            if (Context.TryResolveNumber(value, out number)) {
                return number;
            }
            throw InvalidParameter(index, "number", value);
        }
        
        /// <summary>Resolves one parameter as text.</summary>
        /// <param name="index">The zero-based parameter position.</param>
        /// <returns>The resolved text.</returns>
        public string GetText(int index) {
            SheetValue value = this[index];
            string text;
            if (Context.TryResolveText(value, out text)) {
                return text;
            }
            throw InvalidParameter(index, "text", value);
        }
        
        /// <summary>Resolves one parameter as a Boolean.</summary>
        /// <param name="index">The zero-based parameter position.</param>
        /// <returns>The resolved Boolean.</returns>
        public bool GetBoolean(int index) {
            SheetValue value = this[index];
            bool flagBoolean;
            if (Context.TryResolveBoolean(value, out flagBoolean)) {
                return flagBoolean;
            }
            throw InvalidParameter(index, "Boolean", value);
        }
        
        /// <summary>Gets the variable name held by one parameter.</summary>
        /// <param name="index">The zero-based parameter position.</param>
        /// <returns>The variable name.</returns>
        public string GetVariableName(int index) {
            return GetNamed(index, SheetValueKind.Variable, "variable");
        }
        
        /// <summary>Gets the object or group name held by one parameter.</summary>
        /// <param name="index">The zero-based parameter position.</param>
        /// <returns>The object or group name.</returns>
        public string GetObjectName(int index) {
            return GetNamed(index, SheetValueKind.Object, "object");
        }
        
        /// <summary>Gets the property path held by one parameter.</summary>
        /// <param name="index">The zero-based parameter position.</param>
        /// <returns>The property path, such as <c>Player.X</c>.</returns>
        public string GetPropertyPath(int index) {
            return GetNamed(index, SheetValueKind.Property, "property");
        }
        
        /// <summary>Gets the currently picked objects named by one parameter.</summary>
        /// <param name="index">The zero-based parameter position.</param>
        /// <returns>The picked objects; every match when no condition narrowed them.</returns>
        public IReadOnlyList<SheetObject> GetObjects(int index) {
            return Context.GetObjects(GetObjectName(index));
        }
        
        private string GetNamed(int index, SheetValueKind kindExpected, string labelKind) {
            SheetValue value = this[index];
            if (value.Kind != kindExpected) {
                throw InvalidParameter(index, labelKind, value);
            }
            return value.NameValue;
        }
        
        private static InvalidOperationException InvalidParameter(
            int index,
            string expected,
            SheetValue value
        ) {
            return new InvalidOperationException(
                "Parameter " + index + " must resolve to " + expected +
                ", but contains " + value.Kind + "."
            );
        }
    }
}
