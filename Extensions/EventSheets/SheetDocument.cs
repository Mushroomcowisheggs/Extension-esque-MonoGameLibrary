using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// The portable root of an event sheet.
    /// </summary>
    public sealed class SheetDocument {
        /// <summary>Creates a document from its parts. The parts are copied and remain immutable.</summary>
        /// <param name="version">The format version; one or greater.</param>
        /// <param name="nameSheet">The non-empty sheet name.</param>
        /// <param name="objects">The object instances the sheet declares.</param>
        /// <param name="variables">The global variables of the sheet.</param>
        /// <param name="events">The top-level events in execution order.</param>
        public SheetDocument(
            int version,
            string nameSheet,
            IEnumerable<SheetObject> objects,
            SheetVariables variables,
            IEnumerable<SheetEvent> events
        ) {
            if (version < 1) {
                throw new ArgumentOutOfRangeException(nameof(version));
            }
            if (string.IsNullOrWhiteSpace(nameSheet)) {
                throw new ArgumentException("A non-empty sheet name is required.", nameof(nameSheet));
            }
            if (objects == null) {
                throw new ArgumentNullException(nameof(objects));
            }
            if (variables == null) {
                throw new ArgumentNullException(nameof(variables));
            }
            if (events == null) {
                throw new ArgumentNullException(nameof(events));
            }
            Version = version;
            Name = nameSheet;
            Objects = CopyObjects(objects);
            Variables = variables.Clone();
            Events = CopyEvents(events);
        }
        
        /// <summary>Gets the format version of the document.</summary>
        public int Version { get; }
        
        /// <summary>Gets the sheet name.</summary>
        public string Name { get; }
        
        /// <summary>Gets the object instances the sheet declares.</summary>
        public IReadOnlyList<SheetObject> Objects { get; }
        
        /// <summary>Gets the initial global variables of the sheet.</summary>
        public SheetVariables Variables { get; }
        
        /// <summary>Gets the top-level events in execution order.</summary>
        public IReadOnlyList<SheetEvent> Events { get; }
        
        private static SheetObject[] CopyObjects(IEnumerable<SheetObject> objects) {
            if (objects == null) {
                return new SheetObject[0];
            }
            List<SheetObject> objectsCopy = new List<SheetObject>();
            foreach (SheetObject objectSheet in objects) {
                if (objectSheet == null) {
                    throw new ArgumentException("A sheet cannot contain a null object.", nameof(objects));
                }
                objectsCopy.Add(objectSheet.Clone());
            }
            return objectsCopy.ToArray();
        }
        
        private static SheetEvent[] CopyEvents(IEnumerable<SheetEvent> events) {
            if (events == null) {
                return new SheetEvent[0];
            }
            List<SheetEvent> eventsCopy = new List<SheetEvent>(events);
            for (int i = 0; i < eventsCopy.Count; i += 1) {
                if (eventsCopy[i] == null) {
                    throw new ArgumentException("A sheet cannot contain a null event.", nameof(events));
                }
            }
            return eventsCopy.ToArray();
        }
    }
}
