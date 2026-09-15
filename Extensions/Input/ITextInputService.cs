using System;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.Input {
    /// <summary>
    /// Service that reports text committed by the platform input method.
    /// Characters are buffered when the platform raises them and forwarded from
    /// <see cref="Update"/> so that subscribers always run on the update thread.
    /// This contract intentionally covers only what the backends can deliver:
    /// it does not expose composition state or an editable selection, because
    /// the underlying window API has no such surface.
    /// </summary>
    public interface ITextInputService {
        /// <summary>
        /// Raised once per entered character during <see cref="Update"/>.
        /// </summary>
        event EventHandler<TextEnteredEventArgs> TextEntered;
        
        /// <summary>
        /// Gets or sets a value indicating whether entered characters are buffered
        /// and forwarded. Set this to false to discard input while a text field is
        /// not focused.
        /// </summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// Forwards every character buffered since the previous call and then clears the buffer.
        /// Must be called once per frame.
        /// </summary>
        /// <param name="timeFrame">Timing information for the current frame.</param>
        void Update(FrameTime timeFrame);
    }
}