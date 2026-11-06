using System;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// A parsed object-property path, cached so a frame does not parse the same path again.
    /// </summary>
    internal readonly struct SheetPropertyRef {
        internal SheetPropertyRef(string nameObject, string nameProperty) {
            if (nameObject == null) {
                throw new ArgumentNullException(nameof(nameObject));
            }
            if (nameProperty == null) {
                throw new ArgumentNullException(nameof(nameProperty));
            }
            NameObject = nameObject;
            NameProperty = nameProperty;
        }
        
        internal string NameObject { get; }
        
        internal string NameProperty { get; }
    }
}
