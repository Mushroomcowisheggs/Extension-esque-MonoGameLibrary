using System;
using System.IO;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Reads event-sheet JSON files below one application-controlled directory.
    /// </summary>
    public sealed class SheetFileStore : ISheetStore {
        private readonly string _directoryRoot;
        
        /// <summary>Initializes a store rooted at the supplied directory.</summary>
        /// <param name="directoryRoot">The directory the sheet names are resolved below.</param>
        public SheetFileStore(string directoryRoot) {
            if (string.IsNullOrWhiteSpace(directoryRoot)) {
                throw new ArgumentException("A root directory is required.", nameof(directoryRoot));
            }
            _directoryRoot = Path.GetFullPath(directoryRoot);
        }
        
        /// <summary>Reads the JSON text of the named sheet.</summary>
        /// <param name="nameSheet">The sheet name, without the `.json` extension.</param>
        /// <returns>The sheet JSON text.</returns>
        public string Read(string nameSheet) {
            if (string.IsNullOrWhiteSpace(nameSheet)) {
                throw new ArgumentException("A sheet name is required.", nameof(nameSheet));
            }
            string pathFile = Path.GetFullPath(Path.Combine(_directoryRoot, nameSheet + ".json"));
            string prefix = _directoryRoot.EndsWith(
                Path.DirectorySeparatorChar.ToString(),
                StringComparison.Ordinal
            ) ? _directoryRoot : _directoryRoot + Path.DirectorySeparatorChar;
            if (!pathFile.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                throw new ArgumentException("The sheet name escapes the root directory.", nameof(nameSheet));
            }
            return File.ReadAllText(pathFile);
        }
    }
}
