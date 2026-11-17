using System;
using Godot;
using MonoGameLibrary.Extensions.Graphics;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Adapters.Godot.Graphics {
    /// <summary>
    /// Godot adapter for <see cref="IEffect"/>. 
    /// Wraps a Godot <see cref="ShaderMaterial"/> object. 
    /// </summary>
    internal sealed class Effect : IEffect {
        private readonly ShaderMaterial _materialNative;
        
        public Effect(ShaderMaterial materialNative) {
            if (materialNative == null) {
                throw new ArgumentNullException(nameof(materialNative));
            }
            _materialNative = materialNative;
        }
        
        /// <inheritdoc />
        public void SetParameter<T>(string name, T value) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Parameter name cannot be empty.", nameof(name));
            }
            
            // Godot shader materials do not enumerate their shader parameters, so an
            // unknown name cannot be rejected at set time; the value is stored and
            // ignored by shaders that lack it.
            
            if (typeof(T) == typeof(float)) {
                _materialNative.SetShaderParameter(name, (float)(object)value);
            } else if (value is TwoDimensionalVector vector) {
                _materialNative.SetShaderParameter(name, new Vector2(vector.X, vector.Y));
            } else if (value is ITwoDimensionalTexture texture) {
                TwoDimensionalTexture textureConcrete = texture as TwoDimensionalTexture;
                if (textureConcrete == null) {
                    throw new NotSupportedException("Unsupported texture adapter.");
                }
                Texture2D textureNative = textureConcrete.GetNativeTexture();
                if (textureNative == null) {
                    throw new ObjectDisposedException(
                        nameof(TwoDimensionalTexture), 
                        "The texture adapter has been released and no longer holds a texture. "
                    );
                }
                _materialNative.SetShaderParameter(name, textureNative);
            } else if (typeof(T) == typeof(Vector2)) {
                _materialNative.SetShaderParameter(name, (Vector2)(object)value);
            } else if (typeof(T) == typeof(Vector3)) {
                _materialNative.SetShaderParameter(name, (Vector3)(object)value);
            } else if (typeof(T) == typeof(Vector4)) {
                _materialNative.SetShaderParameter(name, (Vector4)(object)value);
            } else if (typeof(T) == typeof(Transform2D)) {
                _materialNative.SetShaderParameter(name, (Transform2D)(object)value);
            } else if (typeof(T) == typeof(Texture2D)) {
                _materialNative.SetShaderParameter(name, (Texture2D)(object)value);
            } else {
                throw new NotSupportedException($"Parameter type {typeof(T).FullName} is not supported by Effect.SetParameter.");
            }
        }
        
        /// <inheritdoc />
        public void Apply() {
            // Godot binds a shader material per canvas item instead of applying technique
            // passes: the material becomes active when the render context assigns it in
            // Begin and stays active for every draw of the block. There is no pass to
            // advance, so applying the effect is already complete at this point.
        }
        
        /// <summary>
        /// Gets the underlying native shader material. Used by RenderContext for Begin().
        /// </summary>
        internal ShaderMaterial NativeMaterial {
            get { return _materialNative; }
        }
    }
}
