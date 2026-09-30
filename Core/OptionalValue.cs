using System;

namespace MonoGameLibrary.Core {
    public struct OptionalValue<T> where T : struct {
        private readonly T _value;
        private readonly bool _flagHasValue;
        
        public OptionalValue(T value) { _value = value; _flagHasValue = true; }
        public OptionalValue() { _value = default; _flagHasValue = false; }
        
        public bool HasValue { get { return _flagHasValue; } }
        public T Value { get {
            if (_flagHasValue) {
                return _value;
            } else {
                throw new InvalidOperationException("No value.");
            }
        } }
        public T GetValueOrDefault(T defaultValue = default) {
            if (_flagHasValue) {
                return _value;
            } else {
                return defaultValue;
            }
        }
    }
}