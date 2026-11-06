using System;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Controls how an event-sheet module loads and runs one named document.
    /// </summary>
    public sealed class SheetOptions {
        /// <summary>Gets or sets the sheet name the module loads; the file store appends <c>.json</c>.</summary>
        public string NameSheet { get; set; } = "main";
        
        /// <summary>Gets or sets the directory the default file store reads below.</summary>
        public string RootDirectory { get; set; } = "EventSheets";
        
        /// <summary>Gets or sets the update order the module occupies in the host schedule.</summary>
        public int OrderUpdate { get; set; } = 100;
        
        /// <summary>Gets or sets whether the module loads the configured sheet during initialization.</summary>
        public bool FlagAutoLoad { get; set; } = true;
        
        /// <summary>Validates that the required values are present.</summary>
        /// <exception cref="ArgumentException">Thrown when a name or directory is empty.</exception>
        public void Validate() {
            if (string.IsNullOrWhiteSpace(NameSheet)) {
                throw new ArgumentException("A sheet name is required.", nameof(NameSheet));
            }
            if (string.IsNullOrWhiteSpace(RootDirectory)) {
                throw new ArgumentException("A root directory is required.", nameof(RootDirectory));
            }
        }
    }
}
