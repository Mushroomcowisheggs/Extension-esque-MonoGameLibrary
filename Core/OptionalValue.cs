using System;

namespace MonoGameLibrary.Core {
    public struct OptionalValue<T> where T : struct {
        private readonly T _value;
        private readonly bool _flagHasValue;
        
        /// <summary>Initializes a present value.</summary>
        /// <param name="value">The value to hold.</param>
        public OptionalValue(T value) { _value = value; _flagHasValue = true; }
        /// <summary>Initializes an absent value.</summary>
        public OptionalValue() { _value = default; _flagHasValue = false; }
        
        /// <summary>Gets whether a value is present.</summary>
        public bool HasValue { get { return _flagHasValue; } }
        /// <summary>Gets the held value.</summary>
        public T Value { get {
            if (_flagHasValue) {
                return _value;
            } else {
                throw new InvalidOperationException("No value.");
            }
        } }
        /// <summary>Gets the held value, or <paramref name="defaultValue"/> when none is present.</summary>
        /// <param name="defaultValue">The value to return when nothing is held.</param>
        public T GetValueOrDefault(T defaultValue = default) {
            if (_flagHasValue) {
                return _value;
            } else {
                return defaultValue;
            }
        }
    }
}