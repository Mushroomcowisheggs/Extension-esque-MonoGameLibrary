using System;
using System.IO;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Reads event-sheet JSON files below one application-controlled directory.
    /// </summary>
    public sealed class SheetFileStore : ISheetStore {
        private readonly string _directoryRoot;
        
        public SheetFileStore(string directoryRoot) {
            if (string.IsNullOrWhiteSpace(directoryRoot)) {
                throw new ArgumentException("A root directory is required.", nameof(directoryRoot));
            }
            _directoryRoot = Path.GetFullPath(directoryRoot);
        }
        
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
