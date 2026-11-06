using System;
using System.Globalization;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Identifies the value stored in a sheet parameter.
    /// </summary>
    public enum SheetValueKind {
        Number,
        Text,
        Boolean,
        Object,
        Variable,
        Property
    }
    
    /// <summary>
    /// A small tagged value used by conditions and actions. Object, variable, and property values
    /// contain names rather than object instances, so a document remains portable and serializable.
    /// </summary>
    public readonly struct SheetValue : IEquatable<SheetValue> {
        private readonly SheetValueKind _kind;
        private readonly double _number;
        private readonly bool _flagBoolean;
        private readonly string _text;
        
        private SheetValue(SheetValueKind kind, double number, bool flagBoolean, string text) {
            if (kind == SheetValueKind.Text && text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            _kind = kind;
            _number = number;
            _flagBoolean = flagBoolean;
            _text = text;
        }
        
        /// <summary>Creates a numeric value.</summary>
        /// <param name="number">The number to store.</param>
        /// <returns>The numeric value.</returns>
        public static SheetValue Number(double number) {
            return new SheetValue(SheetValueKind.Number, number, false, null);
        }
        
        /// <summary>Creates a text value.</summary>
        /// <param name="text">The text to store.</param>
        /// <returns>The text value.</returns>
        public static SheetValue Text(string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            return new SheetValue(SheetValueKind.Text, 0.0, false, text);
        }
        
        /// <summary>Creates a Boolean value.</summary>
        /// <param name="flagBoolean">The Boolean to store.</param>
        /// <returns>The Boolean value.</returns>
        public static SheetValue Boolean(bool flagBoolean) {
            return new SheetValue(SheetValueKind.Boolean, 0.0, flagBoolean, null);
        }
        
        /// <summary>Creates an object-name value.</summary>
        /// <param name="nameObject">The object or group name.</param>
        /// <returns>The object-name value.</returns>
        public static SheetValue Object(string nameObject) {
            return CreateNamed(SheetValueKind.Object, nameObject, nameof(nameObject));
        }
        
        /// <summary>Creates a variable-name value.</summary>
        /// <param name="nameVariable">The variable name.</param>
        /// <returns>The variable-name value.</returns>
        public static SheetValue Variable(string nameVariable) {
            return CreateNamed(SheetValueKind.Variable, nameVariable, nameof(nameVariable));
        }
        
        /// <summary>Creates a property-path value.</summary>
        /// <param name="pathProperty">The property path, such as <c>Player.X</c>.</param>
        /// <returns>The property-path value.</returns>
        public static SheetValue Property(string pathProperty) {
            return CreateNamed(SheetValueKind.Property, pathProperty, nameof(pathProperty));
        }
        
        /// <summary>Gets the value kind.</summary>
        public SheetValueKind Kind {
            get { return _kind; }
        }
        
        /// <summary>Gets the numeric payload.</summary>
        public double NumberValue {
            get {
                EnsureKind(SheetValueKind.Number);
                return _number;
            }
        }
        
        /// <summary>Gets the text payload.</summary>
        public string TextValue {
            get {
                if (_kind != SheetValueKind.Text) {
                    throw new InvalidOperationException("The sheet value is not text.");
                }
                return _text;
            }
        }
        
        /// <summary>Gets the Boolean payload.</summary>
        public bool BooleanValue {
            get {
                EnsureKind(SheetValueKind.Boolean);
                return _flagBoolean;
            }
        }
        
        /// <summary>Gets an object, variable, or property name.</summary>
        public string NameValue {
            get {
                if (_kind != SheetValueKind.Object &&
                    _kind != SheetValueKind.Variable &&
                    _kind != SheetValueKind.Property) {
                    throw new InvalidOperationException("The sheet value does not contain a name.");
                }
                return _text;
            }
        }
        
        private static SheetValue CreateNamed(SheetValueKind kind, string value, string parameterName) {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new ArgumentException("A non-empty name is required.", parameterName);
            }
            return new SheetValue(kind, 0.0, false, value);
        }
        
        private void EnsureKind(SheetValueKind kindExpected) {
            if (_kind != kindExpected) {
                throw new InvalidOperationException(
                    "The sheet value has kind '" + _kind + "', not '" + kindExpected + "'."
                );
            }
        }
        
        /// <inheritdoc />
        public bool Equals(SheetValue other) {
            return _kind == other._kind &&
                   _number.Equals(other._number) &&
                   _flagBoolean == other._flagBoolean &&
                   string.Equals(_text, other._text, StringComparison.Ordinal);
        }
        
        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (obj is SheetValue) {
                return Equals((SheetValue)obj);
            }
            return false;
        }
        
        /// <inheritdoc />
        public override int GetHashCode() {
            return HashCode.Combine(_kind, _number, _flagBoolean, _text);
        }
        
        /// <inheritdoc />
        public override string ToString() {
            switch (_kind) {
                case SheetValueKind.Number:
                    return _number.ToString(CultureInfo.InvariantCulture);
                case SheetValueKind.Text:
                    return "\"" + _text + "\"";
                case SheetValueKind.Boolean:
                    return _flagBoolean ? "true" : "false";
                case SheetValueKind.Object:
                    return "Object(" + _text + ")";
                case SheetValueKind.Variable:
                    return "Variable(" + _text + ")";
                case SheetValueKind.Property:
                    return "Property(" + _text + ")";
                default:
                    return string.Empty;
            }
        }
        
        public static bool operator ==(SheetValue left, SheetValue right) {
            return left.Equals(right);
        }
        
        public static bool operator !=(SheetValue left, SheetValue right) {
            return !left.Equals(right);
        }
    }
}
