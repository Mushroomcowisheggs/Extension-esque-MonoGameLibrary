namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Control and inspection surface for a loaded event sheet.
    /// </summary>
    public interface ISheetService {
        bool IsLoaded { get; }
        string LoadedSheetName { get; }
        SheetDocument CurrentDocument { get; }
        SheetContext CurrentContext { get; }
        
        void LoadConfiguredSheet();
        void Load(SheetDocument document);
        void Reload();
        string RenderMarkdown();
        string RenderHtml();
    }
}
