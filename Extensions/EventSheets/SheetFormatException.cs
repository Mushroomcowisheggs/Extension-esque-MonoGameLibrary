using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Reports one or more path-based errors in an event-sheet document.
    /// </summary>
    public sealed class SheetFormatException : FormatException {
        /// <summary>Initializes the exception from one path-based error.</summary>
        /// <param name="path">The document path of the failing element.</param>
        /// <param name="message">The validation message.</param>
        public SheetFormatException(string path, string message)
            : this(new string[] { path + ": " + message }) {
        }
        
        /// <summary>Initializes the exception from a set of path-based errors.</summary>
        /// <param name="errors">The validation errors, each already prefixed with its document path.</param>
        public SheetFormatException(IEnumerable<string> errors)
            : base(BuildMessage(errors)) {
            if (errors == null) {
                throw new ArgumentNullException(nameof(errors));
            }
            List<string> copy = new List<string>();
            foreach (string error in errors) {
                if (!string.IsNullOrWhiteSpace(error)) {
                    copy.Add(error);
                }
            }
            if (copy.Count == 0) {
                copy.Add("The event sheet is invalid.");
            }
            Errors = copy.ToArray();
        }
        
        /// <summary>Gets the individual validation errors, each prefixed with its document path.</summary>
        public IReadOnlyList<string> Errors { get; }
        
        private static string BuildMessage(IEnumerable<string> errors) {
            if (errors == null) {
                return "The event sheet is invalid.";
            }
            List<string> copy = new List<string>();
            foreach (string error in errors) {
                if (!string.IsNullOrWhiteSpace(error)) {
                    copy.Add(error);
                }
            }
            if (copy.Count == 0) {
                return "The event sheet is invalid.";
            }
            return string.Join(Environment.NewLine, copy);
        }
    }
}
