using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Identifies the value stored in a named sheet variable.
    /// </summary>
    public enum SheetVariableKind {
        Number,
        Text,
        Boolean
    }
    
    /// <summary>
    /// A boxed-free variable value. The dictionary stores this struct directly.
    /// </summary>
    public readonly struct SheetVariableValue : IEquatable<SheetVariableValue> {
        private readonly SheetVariableKind _kind;
        private readonly double _number;
        private readonly bool _flagBoolean;
        private readonly string _text;
        
        private SheetVariableValue(SheetVariableKind kind, double number, bool flagBoolean, string text) {
            if (kind == SheetVariableKind.Text && text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            _kind = kind;
            _number = number;
            _flagBoolean = flagBoolean;
            _text = text;
        }
        
        /// <summary>Creates a numeric variable value.</summary>
        /// <param name="number">The number to store.</param>
        /// <returns>The numeric value.</returns>
        public static SheetVariableValue Number(double number) {
            return new SheetVariableValue(SheetVariableKind.Number, number, false, null);
        }
        
        /// <summary>Creates a text variable value.</summary>
        /// <param name="text">The text to store.</param>
        /// <returns>The text value.</returns>
        public static SheetVariableValue Text(string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text));
            }
            return new SheetVariableValue(SheetVariableKind.Text, 0.0, false, text);
        }
        
        /// <summary>Creates a Boolean variable value.</summary>
        /// <param name="flagBoolean">The Boolean to store.</param>
        /// <returns>The Boolean value.</returns>
        public static SheetVariableValue Boolean(bool flagBoolean) {
            return new SheetVariableValue(SheetVariableKind.Boolean, 0.0, flagBoolean, null);
        }
        
        /// <summary>Gets the value kind.</summary>
        public SheetVariableKind Kind {
            get { return _kind; }
        }
        
        /// <summary>Gets the numeric payload; throws when the value is not numeric.</summary>
        public double NumberValue {
            get {
                if (_kind != SheetVariableKind.Number) {
                    throw new InvalidOperationException("The variable is not numeric.");
                }
                return _number;
            }
        }
        
        /// <summary>Gets the text payload; throws when the value is not text.</summary>
        public string TextValue {
            get {
                if (_kind != SheetVariableKind.Text) {
                    throw new InvalidOperationException("The variable is not text.");
                }
                return _text;
            }
        }
        
        /// <summary>Gets the Boolean payload; throws when the value is not Boolean.</summary>
        public bool BooleanValue {
            get {
                if (_kind != SheetVariableKind.Boolean) {
                    throw new InvalidOperationException("The variable is not Boolean.");
                }
                return _flagBoolean;
            }
        }
        
        /// <inheritdoc />
        public bool Equals(SheetVariableValue other) {
            return _kind == other._kind &&
                   _number.Equals(other._number) &&
                   _flagBoolean == other._flagBoolean &&
                   string.Equals(_text, other._text, StringComparison.Ordinal);
        }
        
        /// <inheritdoc />
        public override bool Equals(object obj) {
            if (obj is SheetVariableValue) {
                return Equals((SheetVariableValue)obj);
            }
            return false;
        }
        
        /// <inheritdoc />
        public override int GetHashCode() {
            return HashCode.Combine(_kind, _number, _flagBoolean, _text);
        }
    }
    
    /// <summary>
    /// Thread-safe storage for global or object-local sheet variables.
    /// </summary>
    public sealed class SheetVariables {
        private readonly object _lock = new object();
        private readonly Dictionary<string, SheetVariableValue> _values =
            new Dictionary<string, SheetVariableValue>(StringComparer.Ordinal);
        
        /// <summary>Gets the number of stored variables.</summary>
        public int Count {
            get {
                lock (_lock) {
                    return _values.Count;
                }
            }
        }
        
        /// <summary>Stores a numeric value under the supplied name.</summary>
        /// <param name="nameVariable">The non-empty variable name.</param>
        /// <param name="number">The number to store.</param>
        public void SetNumber(string nameVariable, double number) {
            Set(nameVariable, SheetVariableValue.Number(number));
        }
        
        /// <summary>Stores a text value under the supplied name.</summary>
        /// <param name="nameVariable">The non-empty variable name.</param>
        /// <param name="text">The text to store.</param>
        public void SetText(string nameVariable, string text) {
            Set(nameVariable, SheetVariableValue.Text(text));
        }
        
        /// <summary>Stores a Boolean value under the supplied name.</summary>
        /// <param name="nameVariable">The non-empty variable name.</param>
        /// <param name="flagBoolean">The Boolean to store.</param>
        public void SetBoolean(string nameVariable, bool flagBoolean) {
            Set(nameVariable, SheetVariableValue.Boolean(flagBoolean));
        }
        
        /// <summary>Stores one value under the supplied name.</summary>
        /// <param name="nameVariable">The non-empty variable name.</param>
        /// <param name="value">The value to store.</param>
        public void Set(string nameVariable, SheetVariableValue value) {
            ValidateName(nameVariable);
            lock (_lock) {
                _values[nameVariable] = value;
            }
        }
        
        /// <summary>Tries to read one value under the supplied name.</summary>
        /// <param name="nameVariable">The non-empty variable name.</param>
        /// <param name="value">The stored value, or the default when absent.</param>
        /// <returns>True when the variable exists; otherwise false.</returns>
        public bool TryGet(string nameVariable, out SheetVariableValue value) {
            ValidateName(nameVariable);
            lock (_lock) {
                return _values.TryGetValue(nameVariable, out value);
            }
        }
        
        /// <summary>Tries to read one numeric value under the supplied name.</summary>
        /// <param name="nameVariable">The non-empty variable name.</param>
        /// <param name="number">The stored number, or zero when absent or not numeric.</param>
        /// <returns>True when a numeric variable exists; otherwise false.</returns>
        public bool TryGetNumber(string nameVariable, out double number) {
            SheetVariableValue value;
            if (TryGet(nameVariable, out value) && value.Kind == SheetVariableKind.Number) {
                number = value.NumberValue;
                return true;
            }
            number = 0.0;
            return false;
        }
        
        /// <summary>Tries to read one text value under the supplied name.</summary>
        /// <param name="nameVariable">The non-empty variable name.</param>
        /// <param name="text">The stored text, or an empty string when absent or not text.</param>
        /// <returns>True when a text variable exists; otherwise false.</returns>
        public bool TryGetText(string nameVariable, out string text) {
            SheetVariableValue value;
            if (TryGet(nameVariable, out value) && value.Kind == SheetVariableKind.Text) {
                text = value.TextValue;
                return true;
            }
            text = string.Empty;
            return false;
        }
        
        /// <summary>Tries to read one Boolean value under the supplied name.</summary>
        /// <param name="nameVariable">The non-empty variable name.</param>
        /// <param name="flagBoolean">The stored Boolean, or false when absent or not Boolean.</param>
        /// <returns>True when a Boolean variable exists; otherwise false.</returns>
        public bool TryGetBoolean(string nameVariable, out bool flagBoolean) {
            SheetVariableValue value;
            if (TryGet(nameVariable, out value) && value.Kind == SheetVariableKind.Boolean) {
                flagBoolean = value.BooleanValue;
                return true;
            }
            flagBoolean = false;
            return false;
        }
        
        /// <summary>Creates an independent copy of the stored variables.</summary>
        /// <returns>The copied variable storage.</returns>
        public SheetVariables Clone() {
            SheetVariables variables = new SheetVariables();
            lock (_lock) {
                foreach (KeyValuePair<string, SheetVariableValue> pair in _values) {
                    variables._values.Add(pair.Key, pair.Value);
                }
            }
            return variables;
        }
        
        /// <summary>Gets a stable snapshot of every stored variable.</summary>
        /// <returns>The snapshot array, in unspecified order.</returns>
        public IReadOnlyList<KeyValuePair<string, SheetVariableValue>> Snapshot() {
            lock (_lock) {
                List<KeyValuePair<string, SheetVariableValue>> values =
                    new List<KeyValuePair<string, SheetVariableValue>>(_values);
                return values.ToArray();
            }
        }
        
        private static void ValidateName(string nameVariable) {
            if (string.IsNullOrWhiteSpace(nameVariable)) {
                throw new ArgumentException("A non-empty variable name is required.", nameof(nameVariable));
            }
        }
    }
}
