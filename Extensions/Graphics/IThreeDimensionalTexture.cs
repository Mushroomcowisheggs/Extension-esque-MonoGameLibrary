namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// A 3-dimensional (volume) texture resource. 
    /// </summary>
    public interface IThreeDimensionalTexture : ITexture {
        /// <summary>Gets the depth of the volume texture. </summary>
        int Depth { get; }
    }
}