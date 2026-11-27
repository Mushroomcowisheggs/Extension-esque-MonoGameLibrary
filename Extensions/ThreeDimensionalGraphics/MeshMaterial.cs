using System;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Platform-neutral surface material for three-dimensional geometry: an albedo color
    /// whose alpha drives transparency, an additive emission color, an unlit switch and a
    /// face culling mode. Adapters map this description to their native material model.
    /// </summary>
    /// <remarks>
    /// The default material (white albedo, black emission, lit, back-face culling) is what
    /// submissions without a material render with, and what an untextured mesh looks like
    /// under the adapter's built-in lighting.
    /// </remarks>
    public readonly struct MeshMaterial {
        /// <summary>
        /// Initializes a material.
        /// </summary>
        /// <param name="colorAlbedo">The base color; its alpha component controls transparency (255 = opaque).</param>
        /// <param name="colorEmission">The color added on top of lit shading (black = none).</param>
        /// <param name="flagUnshaded">True to draw the geometry unlit, using the albedo color directly.</param>
        /// <param name="modeCull">The face culling mode.</param>
        public MeshMaterial(Color colorAlbedo, Color colorEmission, bool flagUnshaded, MeshCullMode modeCull) {
            AlbedoColor = colorAlbedo;
            EmissionColor = colorEmission;
            UnshadedFlag = flagUnshaded;
            CullMode = modeCull;
        }
        
        /// <summary>Gets the base color. Its alpha component controls transparency (255 = opaque).</summary>
        public Color AlbedoColor { get; }
        
        /// <summary>Gets the emission color added on top of lit shading (black = none).</summary>
        public Color EmissionColor { get; }
        
        /// <summary>Gets a value indicating whether the geometry is drawn unlit.</summary>
        public bool UnshadedFlag { get; }
        
        /// <summary>Gets the face culling mode.</summary>
        public MeshCullMode CullMode { get; }
        
        /// <summary>
        /// Gets the default material: white albedo, black emission, lit shading and
        /// back-face culling.
        /// </summary>
        public static MeshMaterial Default {
            get {
                return new MeshMaterial(Color.White, Color.Black, false, MeshCullMode.Back);
            }
        }
    }
}
