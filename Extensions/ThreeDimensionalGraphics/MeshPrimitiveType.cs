namespace MonoGameLibrary.Extensions.ThreeDimensionalGraphics {
    /// <summary>
    /// Platform-neutral primitive topology for a mesh.
    /// </summary>
    public enum MeshPrimitiveType {
        /// <summary>Each three indices form one triangle.</summary>
        TriangleList,
        
        /// <summary>Each new index after the first two forms a triangle.</summary>
        TriangleStrip,
        
        /// <summary>Each two indices form one line.</summary>
        LineList,
        
        /// <summary>Each new index after the first forms a line.</summary>
        LineStrip
    }
}
