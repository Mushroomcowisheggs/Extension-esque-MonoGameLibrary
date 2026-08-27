using System;

namespace MonoGameLibrary.Utilities {
    /// <summary>
    /// Assertion utilities for cleanup verification.
    /// Designed for development and testing assistance.
    /// Not intended for framework modules.
    /// </summary>
    public static class CleanupAssert {
        /// <summary>
        /// Asserts that the provided action does not throw when executed.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="message">The message to include if the assertion fails.</param>
        /// <exception cref="InvalidOperationException">Thrown if the action throws.</exception>
        public static void DoesNotThrow(Action action, string message = null) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }
            
            try {
                action.Invoke();
            } catch (Exception exception) {
                string messageToUse = message;
                if (messageToUse == null) {
                    messageToUse = "Cleanup assertion failed: action threw an exception.";
                }
                throw new InvalidOperationException(messageToUse, exception);
            }
        }
        
        /// <summary>
        /// Asserts that the provided condition is true.
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="message">The message to include if the assertion fails.</param>
        /// <exception cref="InvalidOperationException">Thrown if the condition is false.</exception>
        public static void IsTrue(Func<bool> condition, string message) {
            if (condition == null) {
                throw new ArgumentNullException(nameof(condition));
            }
            if (string.IsNullOrWhiteSpace(message)) {
                throw new ArgumentException("Message cannot be empty.", nameof(message));
            }
            
            if (!condition.Invoke()) {
                throw new InvalidOperationException($"Cleanup assertion failed: {message}");
            }
        }
    }
}