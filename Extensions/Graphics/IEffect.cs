using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Content;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Represents a shader effect that can be applied to rendering.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IEffect : IAsset {
        /// <summary>
        /// Sets a parameter value on the effect.
        /// </summary>
        /// <typeparam name="T">The type of the parameter (float, Vector2, Matrix, etc.).</typeparam>
        /// <param name="name">The parameter name as defined in the shader.</param>
        /// <param name="value">The value to set.</param>
        void SetParameter<T>(string name, T value);
        
        /// <summary>
        /// Applies the effect to the current rendering pipeline.
        /// Called internally by the render context.
        /// </summary>
        void Apply();
    }
}