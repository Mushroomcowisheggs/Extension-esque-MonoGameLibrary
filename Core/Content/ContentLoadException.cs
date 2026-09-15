using System;

namespace MonoGameLibrary.Core.Content {
    /// <summary>
    /// Raised when an asset cannot be loaded through the content pipeline.
    /// Adapters translate backend specific failures into this exception so that
    /// game and extension code can react without referencing a platform library.
    /// </summary>
    public sealed class ContentLoadException : Exception {
        /// <summary>Initializes a new instance with a default message.</summary>
        public ContentLoadException() : base("Content could not be loaded.") {
        }
        
        /// <summary>Initializes a new instance with the supplied message.</summary>
        /// <param name="message">A description of the failure.</param>
        public ContentLoadException(string message) : base(message) {
        }
        
        /// <summary>Initializes a new instance with the supplied message and inner cause.</summary>
        /// <param name="message">A description of the failure.</param>
        /// <param name="exceptionInner">The backend exception that caused this failure.</param>
        public ContentLoadException(string message, Exception exceptionInner) : base(message, exceptionInner) {
        }
    }
}