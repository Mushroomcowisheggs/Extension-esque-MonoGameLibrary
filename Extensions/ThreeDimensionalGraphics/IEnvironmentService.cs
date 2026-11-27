using System.Numerics;
using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Service describing the three-dimensional scene environment: the directional sun,
    /// the ambient term, shadow casting and the background. Adapters apply this
    /// description to their native lighting and environment model.
    /// </summary>
    /// <remarks>
    /// The environment is off by default: the renderers keep their built-in default
    /// lighting rig and leave the background untouched until <see cref="FlagEnabled"/> is
    /// set. Materials render against whichever environment is active; a material's own
    /// emission is added on top of it.
    /// </remarks>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface IEnvironmentService {
        /// <summary>
        /// Gets or sets a value indicating whether this environment overrides the
        /// built-in default lighting and background.
        /// </summary>
        bool FlagEnabled { get; set; }
        
        /// <summary>
        /// Gets or sets the direction the sun light travels along. The direction is
        /// normalized by the adapter before use. The zero vector is rejected at the
        /// entry point: implementations throw <see cref="System.ArgumentException"/> for
        /// it, and adapters reject it the same way when they read it back.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when the value is the zero vector.</exception>
        Vector3 SunDirection { get; set; }
        
        /// <summary>Gets or sets the sun color.</summary>
        Color SunColor { get; set; }
        
        /// <summary>Gets or sets the sun intensity multiplier (1.0 = the color as supplied).</summary>
        float SunIntensity { get; set; }
        
        /// <summary>Gets or sets the ambient light color.</summary>
        Color AmbientColor { get; set; }
        
        /// <summary>Gets or sets a value indicating whether the sun casts shadows.</summary>
        bool FlagShadows { get; set; }
        
        /// <summary>Gets or sets the background color behind the geometry.</summary>
        Color BackgroundColor { get; set; }
    }
}
