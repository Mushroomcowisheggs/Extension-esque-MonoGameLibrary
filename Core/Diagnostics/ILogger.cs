using System;
using System.Diagnostics.CodeAnalysis;
using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Core.Diagnostics {
    /// <summary>
    /// Represents the severity level of a log message.
    /// </summary>
    public enum LogLevel {
        /// <summary>Detailed information, typically for development use.</summary>
        Debug, 
        /// <summary>General informational message.</summary>
        Info, 
        /// <summary>Indicates a potential problem.</summary>
        Warning, 
        /// <summary>Indicates an error that has occurred.</summary>
        Error, 
        /// <summary>Indicates a critical error that may cause the application to fail.</summary>
        Fatal
    }
    
    /// <summary>
    /// A basic logging interface.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface ILogger {
        /// <inheritdoc cref="Log(LogLevel, string, Exception)"/>
        /// <param name="level">The severity level.</param>
        /// <param name="message">The log message.</param>
        /// <remarks>
        /// This overload logs a message without an associated exception.
        /// </remarks>
        void Log(LogLevel level, string message);
        
        /// <summary>
        /// Writes a log entry.
        /// </summary>
        /// <param name="level">The severity level. </param>
        /// <param name="message">The log message. </param>
        /// <param name="exception">An optional exception to attach. </param>
        void Log(LogLevel level, string message, Exception exception);
        
        /// <summary>
        /// Checks if a given log level is enabled. 
        /// </summary>
        /// <param name="level">The log level to check. </param>
        /// <returns><c>true</c> if the level is enabled; otherwise <c>false</c>. </returns>
        bool IsEnabled(LogLevel level);
    }
    
    /// <summary>
    /// Extension methods for <see cref="ILogger"/> for convenient logging.
    /// </summary>
    public static class LoggerExtensions {
        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The debug message.</param>
        public static void Debug(this ILogger logger, string message) {
            logger.Log(LogLevel.Debug, message);
        }
        
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The informational message.</param>
        public static void Info(this ILogger logger, string message) {
            logger.Log(LogLevel.Info, message);
        }
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The warning message.</param>
        public static void Warning(this ILogger logger, string message) {
            logger.Log(LogLevel.Warning, message);
        }
        
        /// <inheritdoc cref="Error(ILogger, string, Exception)"/>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The error message.</param>
        public static void Error(this ILogger logger, string message) {
            logger.Log(LogLevel.Error, message);
        }
        
        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The error message.</param>
        /// <param name="exception">The exception to log.</param>
        public static void Error(this ILogger logger, string message, Exception exception) {
            logger.Log(LogLevel.Error, message, exception);
        }
        
        /// <summary>
        /// Logs a fatal error message.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The fatal error message.</param>
        /// <remarks>This overload does not include an exception parameter.</remarks>
        public static void Fatal(this ILogger logger, string message) {
            logger.Log(LogLevel.Fatal, message);
        }
    }
}