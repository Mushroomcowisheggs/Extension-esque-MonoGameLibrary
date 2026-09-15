using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace MonoGameLibrary.Adapters.MonoGame.Audio {
    /// <summary>
    /// Recognizes the backend failures that mean "audio is not usable right now" so that
    /// adapters can translate them into <see cref="MonoGameLibrary.Extensions.Audio.AudioDeviceException"/>.
    /// The original exception is always preserved as the inner exception.
    /// </summary>
    internal static class AudioFailure {
        /// <summary>
        /// Determines whether the supplied exception represents an audio device failure.
        /// </summary>
        /// <param name="exception">The exception thrown by the audio backend.</param>
        /// <returns>True when the failure should be reported as an audio device failure.</returns>
        internal static bool IsDeviceFailure(Exception exception) {
            if (exception is NoAudioHardwareException) {
                return true;
            }
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