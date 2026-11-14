using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// A bitmap or vector font. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IFont : IAsset {
        /// <summary>
        /// Measures the on-screen size of the given text string when rendered with this font. 
        /// </summary>
        /// <param name="text">The text to measure. </param>
        /// <returns>The width and height of the rendered text as a <see cref="TwoDimensionalVector"/>. </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="text"/> is null. </exception>
        TwoDimensionalVector MeasureString(string text);
    }
}