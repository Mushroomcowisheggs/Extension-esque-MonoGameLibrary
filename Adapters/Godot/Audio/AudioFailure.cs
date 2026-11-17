using System;
using System.IO;

namespace MonoGameLibrary.Adapters.Godot.Audio {
    /// <summary>
    /// Recognizes the backend failures that mean "audio is not usable right now" so that
    /// adapters can translate them into <see cref="MonoGameLibrary.Extensions.Audio.AudioDeviceException"/>.
    /// The original exception is always preserved as the inner exception.
    /// </summary>
    /// <remarks>
    /// Godot has no <c>NoAudioHardwareException</c>: a missing or headless audio device runs the
    /// engine's dummy driver and fails silently instead of throwing, so that failure kind cannot
    /// appear here. The remaining kinds cover the exception classes the audio path can raise.
    /// </remarks>
    internal static class AudioFailure {
        /// <summary>
        /// Determines whether the supplied exception represents an audio device failure.
        /// </summary>
        /// <param name="exception">The exception thrown by the audio backend.</param>
        /// <returns>True when the failure should be reported as an audio device failure.</returns>
        internal static bool IsDeviceFailure(Exception exception) {
            if (exception is DllNotFoundException) {
                return true;
            }
            if (exception is InvalidOperationException) {
                return true;
            }
            if (exception is IOException) {
                return true;
            }
            return false;
        }
    }
}
