using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Adapters.MonoGame.Graphics {
    /// <summary>
    /// MonoGame adapter for <see cref="IEffect"/>. 
    /// Wraps an XNA/MonoGame <see cref="Effect"/> object. 
    /// </summary>
    internal sealed class Effect : IEffect {
        private readonly Microsoft.Xna.Framework.Graphics.Effect _effectNative;
        
        public Effect(Microsoft.Xna.Framework.Graphics.Effect effectNative) {
            if (effectNative == null) {
                throw new ArgumentNullException(nameof(effectNative));
            }
            _effectNative = effectNative;
        }
        
        /// <inheritdoc />
        public void SetParameter<T>(string name, T value) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Parameter name cannot be empty.", nameof(name));
            }
            
            var parameter = _effectNative.Parameters[name];
            if (parameter == null) {
                throw new InvalidOperationException($"Effect parameter '{name}' not found.");
            }
            
            if (typeof(T) == typeof(float)) {
                parameter.SetValue((float)(object)value);
            } else if (value is TwoDimensionalVector vector) {
                parameter.SetValue(new Vector2(vector.X, vector.Y));
            } else if (value is ITwoDimensionalTexture texture) {
                TwoDimensionalTexture textureConcrete = texture as TwoDimensionalTexture;
                if (textureConcrete == null) {
                    throw new NotSupportedException("Unsupported texture adapter.");
                }
                parameter.SetValue(textureConcrete.GetNativeTexture());
            } else if (typeof(T) == typeof(Vector2)) {
                parameter.SetValue((Vector2)(object)value);
            } else if (typeof(T) == typeof(Vector3)) {
                parameter.SetValue((Vector3)(object)value);
            } else if (typeof(T) == typeof(Vector4)) {
                parameter.SetValue((Vector4)(object)value);
            } else if (typeof(T) == typeof(Matrix)) {
                parameter.SetValue((Matrix)(object)value);
            } else if (typeof(T) == typeof(Texture2D)) {
                parameter.SetValue((Texture2D)(object)value);
            } else {
                throw new NotSupportedException($"Parameter type {typeof(T).FullName} is not supported by Effect.SetParameter.");
            }
        }
        
        /// <inheritdoc />
        public void Apply() {
            _effectNative.CurrentTechnique.Passes[0].Apply();
        }
        
        /// <summary>
        /// Gets the underlying native effect. Used by RenderContext for Begin().
        /// </summary>
        internal Microsoft.Xna.Framework.Graphics.Effect NativeEffect {
            get { return _effectNative; }
        }
    }
}
