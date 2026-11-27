using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// Drawing seam for the neutral user-interface widgets: filled rectangles, text and
    /// text measurement. Widgets name only this contract; the concrete renderer is
    /// composed at the composition root.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IUiRenderer {
        /// <summary>Begins a drawing block for one interface pass.</summary>
        void Begin();
        
        /// <summary>Ends the drawing block and flushes it.</summary>
        void End();
        
        /// <summary>Measures the rendered size of a text string.</summary>
        /// <param name="text">The text to measure.</param>
        /// <returns>The rendered size in render coordinates.</returns>
        TwoDimensionalVector MeasureText(string text);
        
        /// <summary>Draws a filled rectangle.</summary>
        /// <param name="bounds">The rectangle in render coordinates.</param>
        /// <param name="color">The fill color.</param>
        void DrawRectangle(Rectangle bounds, Color color);
        
        /// <summary>Draws a line of text at a position.</summary>
        /// <param name="text">The text to draw.</param>
        /// <param name="position">Screen position (top-left origin).</param>
        /// <param name="color">The text color.</param>
        void DrawText(string text, TwoDimensionalVector position, Color color);
    }
}
