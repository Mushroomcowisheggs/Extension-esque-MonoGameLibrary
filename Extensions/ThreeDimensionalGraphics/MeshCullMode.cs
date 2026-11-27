namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Platform-neutral face culling for a material.
    /// </summary>
    public enum MeshCullMode {
        /// <summary>Both faces are drawn.</summary>
        None,
        
        /// <summary>Back faces are culled; front faces are drawn. This is the default.</summary>
        Back,
        
        /// <summary>Front faces are culled; back faces are drawn.</summary>
        Front
    }
}
