using System;

namespace MonoGameLibrary.Core {
    /// <summary>
    /// Wraps a reference value that may be absent.  Replaces nullable reference types. 
    /// </summary>
    /// <typeparam name="T">The reference type to wrap. </typeparam>
    public readonly struct Optional<T> where T : class {
        private readonly T _value;
        
        /// <summary>
        /// Creates an Optional with a value. 
        /// </summary>
        /// <param name="value">The non-null value to store. </param>
        public Optional(T value) {
            if (value == null) {
                throw new ArgumentNullException(nameof(value));
            }
            _value = value;
        }
        
        /// <summary>
        /// Returns true if a value is present. 
        /// </summary>
        public bool HasValue {
            get { return _value != null; }
        }
        
        /// <summary>
        /// Gets the stored value.  Throws if no value is present. 
        /// </summary>
        /// <exception cref="InvalidOperationException">If the optional is empty. </exception>
        public T Value {
            get {
                if (!HasValue) {
                    throw new InvalidOperationException("Optional value is not present.");
                }
                return _value;
            }
        }
        
        /// <summary>
        /// Returns the value, or the provided default if empty. 
        /// </summary>
        public T GetValueOrDefaultValue(T valueDefault) {
            if (HasValue) {
                return _value;
            }
            return valueDefault;
        }
        
        /// <summary>
        /// Implicit conversion from T to Optional&lt;T&gt;.
        /// </summary>
        public static implicit operator Optional<T>(T value) {
            if (value == null) {
                return default;
            }
            return new Optional<T>(value);
        }
        
        /// <summary>
        /// Implicit conversion from Optional&lt;T&gt; to bool (true if has value).
        /// </summary>
        public static implicit operator bool(Optional<T> optional) {
            if (optional.HasValue) {
                return true;
            }
            return false;
        }
        
        public override string ToString() {
            if (HasValue) {
                return _value.ToString();
            }
            
            return "[No value]";
        }
    }
}