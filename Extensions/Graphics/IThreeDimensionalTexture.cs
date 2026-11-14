using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// A 3-dimensional (volume) texture resource. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IThreeDimensionalTexture : ITexture {
        /// <summary>Gets the depth of the volume texture. </summary>
        int Depth { get; }
    }
}