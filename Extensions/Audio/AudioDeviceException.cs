using System;

namespace MonoGameLibrary.Extensions.Audio {
    /// <summary>
    /// Raised when the audio device is missing, cannot be opened, or fails while
    /// playing. Adapters translate backend specific failures into this exception so
    /// that game code can degrade gracefully without referencing a platform library.
    /// The original backend exception is preserved as <see cref="Exception.InnerException"/>.
    /// </summary>
    public sealed class AudioDeviceException : Exception {
        /// <summary>Initializes a new instance with a default message.</summary>
        public AudioDeviceException() : base("The audio device is not available.") {
        }
        
        /// <summary>Initializes a new instance with the supplied message.</summary>
        /// <param name="message">A description of the failure.</param>
        public AudioDeviceException(string message) : base(message) {
        }
        
        /// <summary>Initializes a new instance with the supplied message and inner cause.</summary>
        /// <param name="message">A description of the failure.</param>
        /// <param name="exceptionInner">The backend exception that caused this failure.</param>
        public AudioDeviceException(string message, Exception exceptionInner) : base(message, exceptionInner) {
        }
    }
}