namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Supplies raw JSON text for a named event sheet.
    /// </summary>
    public interface ISheetStore {
        string Read(string nameSheet);
    }
}
