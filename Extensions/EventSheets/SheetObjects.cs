using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Thread-safe object lookup and iteration for an event-sheet context. The name and group
    /// indexes are rebuilt when the set changes, so a frame reads the arrays without allocating.
    /// </summary>
    public sealed class SheetObjects {
        private static readonly SheetObject[] _objectsEmpty = new SheetObject[0];
        private readonly object _lock = new object();
        private readonly List<SheetObject> _objects = new List<SheetObject>();
        private readonly Dictionary<string, SheetObject[]> _objectsByName =
            new Dictionary<string, SheetObject[]>(StringComparer.Ordinal);
        private readonly Dictionary<string, SheetObject[]> _objectsByGroup =
            new Dictionary<string, SheetObject[]>(StringComparer.Ordinal);
        private SheetObject[] _objectsSnapshot = _objectsEmpty;
        
        /// <summary>Gets the number of objects in the set.</summary>
        public int Count {
            get {
                lock (_lock) {
                    return _objects.Count;
                }
            }
        }
        
        /// <summary>Adds one object instance. The same instance cannot be added twice.</summary>
        /// <param name="objectSheet">The object instance to add.</param>
        public void Add(SheetObject objectSheet) {
            if (objectSheet == null) {
                throw new ArgumentNullException(nameof(objectSheet));
            }
            lock (_lock) {
                if (_objects.Contains(objectSheet)) {
                    throw new InvalidOperationException("The object is already in this set.");
                }
                _objects.Add(objectSheet);
                Rebuild();
            }
        }
        
        /// <summary>Removes one object instance.</summary>
        /// <param name="objectSheet">The object instance to remove.</param>
        /// <returns>True when the instance was present and removed; otherwise false.</returns>
        public bool Remove(SheetObject objectSheet) {
            if (objectSheet == null) {
                throw new ArgumentNullException(nameof(objectSheet));
            }
            lock (_lock) {
                if (!_objects.Remove(objectSheet)) {
                    return false;
                }
                Rebuild();
                return true;
            }
        }
        
        /// <summary>Gets every object that declares the supplied name.</summary>
        /// <param name="nameObject">The object name.</param>
        /// <returns>The matching objects; empty when the name is unknown.</returns>
        public IReadOnlyList<SheetObject> GetByName(string nameObject) {
            ValidateName(nameObject);
            lock (_lock) {
                SheetObject[] objects;
                if (_objectsByName.TryGetValue(nameObject, out objects)) {
                    return objects;
                }
                return _objectsEmpty;
            }
        }
        
        /// <summary>Gets every object that belongs to the supplied group.</summary>
        /// <param name="nameGroup">The group name.</param>
        /// <returns>The matching objects; empty when the group is unknown.</returns>
        public IReadOnlyList<SheetObject> GetByGroup(string nameGroup) {
            ValidateName(nameGroup);
            lock (_lock) {
                SheetObject[] objects;
                if (_objectsByGroup.TryGetValue(nameGroup, out objects)) {
                    return objects;
                }
                return _objectsEmpty;
            }
        }
        
        /// <summary>Gets the first object that declares the supplied name.</summary>
        /// <param name="nameObject">The object name.</param>
        /// <param name="objectSheet">The first matching object, or null when the name is unknown.</param>
        /// <returns>True when an object was found; otherwise false.</returns>
        public bool TryGetFirst(string nameObject, out SheetObject objectSheet) {
            IReadOnlyList<SheetObject> objects = GetByName(nameObject);
            if (objects.Count > 0) {
                objectSheet = objects[0];
                return true;
            }
            objectSheet = null;
            return false;
        }
        
        /// <summary>Gets a stable snapshot of every object in insertion order.</summary>
        /// <returns>The snapshot array. It is replaced, never mutated, when the set changes.</returns>
        public IReadOnlyList<SheetObject> Snapshot() {
            lock (_lock) {
                return _objectsSnapshot;
            }
        }
        
        /// <summary>Invokes one action for every object that declares the supplied name.</summary>
        /// <param name="nameObject">The object name.</param>
        /// <param name="actionObject">The action to invoke for each match.</param>
        public void ForEach(string nameObject, Action<SheetObject> actionObject) {
            if (actionObject == null) {
                throw new ArgumentNullException(nameof(actionObject));
            }
            IReadOnlyList<SheetObject> objects = GetByName(nameObject);
            for (int i = 0; i < objects.Count; i += 1) {
                actionObject(objects[i]);
            }
        }
        
        /// <summary>Removes every object from the set.</summary>
        public void Clear() {
            lock (_lock) {
                _objects.Clear();
                Rebuild();
            }
        }
        
        private void Rebuild() {
            _objectsByName.Clear();
            _objectsByGroup.Clear();
            if (_objects.Count == 0) {
                _objectsSnapshot = _objectsEmpty;
                return;
            }
            
            _objectsSnapshot = _objects.ToArray();
            for (int i = 0; i < _objectsSnapshot.Length; i += 1) {
                SheetObject objectSheet = _objectsSnapshot[i];
                AddToIndex(_objectsByName, objectSheet.Name, objectSheet);
                for (int group = 0; group < objectSheet.Groups.Count; group += 1) {
                    AddToIndex(_objectsByGroup, objectSheet.Groups[group], objectSheet);
                }
            }
        }
        
        private static void AddToIndex(
            Dictionary<string, SheetObject[]> index,
            string key,
            SheetObject objectSheet
        ) {
            SheetObject[] objectsExisting;
            if (!index.TryGetValue(key, out objectsExisting)) {
                index.Add(key, new SheetObject[] { objectSheet });
                return;
            }
            SheetObject[] objectsUpdated = new SheetObject[objectsExisting.Length + 1];
            Array.Copy(objectsExisting, objectsUpdated, objectsExisting.Length);
            objectsUpdated[objectsExisting.Length] = objectSheet;
            index[key] = objectsUpdated;
        }
        
        private static void ValidateName(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("A non-empty object or group name is required.", nameof(name));
            }
        }
    }
}
