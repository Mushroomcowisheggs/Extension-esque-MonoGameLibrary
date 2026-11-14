using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Supplies raw JSON text for a named event sheet.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface ISheetStore {
        string Read(string nameSheet);
    }
}
