using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// A portable object instance exposed to event-sheet instructions.
    /// </summary>
    public sealed class SheetObject {
        private readonly object _lock = new object();
        private readonly string[] _groups;
        private double _positionX;
        private double _positionY;
        private double _width;
        private double _height;
        private double _angle;
        private bool _flagVisible;
        
        /// <summary>Creates an object instance with its initial state.</summary>
        /// <param name="nameObject">The non-empty object name the document uses.</param>
        /// <param name="positionX">The initial horizontal position.</param>
        /// <param name="positionY">The initial vertical position.</param>
        /// <param name="width">The initial drawn width.</param>
        /// <param name="height">The initial drawn height.</param>
        /// <param name="angle">The initial rotation in radians.</param>
        /// <param name="flagVisible">The initial visibility.</param>
        /// <param name="groups">The groups the instance belongs to.</param>
        public SheetObject(
            string nameObject,
            double positionX = 0.0,
            double positionY = 0.0,
            double width = 0.0,
            double height = 0.0,
            double angle = 0.0,
            bool flagVisible = true,
            IEnumerable<string> groups = null
        ) {
            if (string.IsNullOrWhiteSpace(nameObject)) {
                throw new ArgumentException("A non-empty object name is required.", nameof(nameObject));
            }
            Name = nameObject;
            _positionX = positionX;
            _positionY = positionY;
            _width = width;
            _height = height;
            _angle = angle;
            _flagVisible = flagVisible;
            _groups = CopyGroups(groups);
            Variables = new SheetVariables();
        }
        
        /// <summary>Gets the object name the document uses.</summary>
        public string Name { get; }
        
        /// <summary>Gets or sets the horizontal position.</summary>
        public double X {
            get { lock (_lock) { return _positionX; } }
            set { lock (_lock) { _positionX = value; } }
        }
        
        /// <summary>Gets or sets the vertical position.</summary>
        public double Y {
            get { lock (_lock) { return _positionY; } }
            set { lock (_lock) { _positionY = value; } }
        }
        
        /// <summary>Gets or sets the drawn width.</summary>
        public double Width {
            get { lock (_lock) { return _width; } }
            set { lock (_lock) { _width = value; } }
        }
        
        /// <summary>Gets or sets the drawn height.</summary>
        public double Height {
            get { lock (_lock) { return _height; } }
            set { lock (_lock) { _height = value; } }
        }
        
        /// <summary>Gets or sets the rotation in radians.</summary>
        public double Angle {
            get { lock (_lock) { return _angle; } }
            set { lock (_lock) { _angle = value; } }
        }
        
        /// <summary>Gets or sets whether the object is visible.</summary>
        public bool Visible {
            get { lock (_lock) { return _flagVisible; } }
            set { lock (_lock) { _flagVisible = value; } }
        }
        
        /// <summary>Gets the groups the instance belongs to.</summary>
        public IReadOnlyList<string> Groups {
            get { return _groups; }
        }
        
        /// <summary>Gets the variables local to this object instance.</summary>
        public SheetVariables Variables { get; }
        
        /// <summary>Creates an independent copy of the object, including its variables.</summary>
        /// <returns>The copied instance.</returns>
        public SheetObject Clone() {
            SheetObject objectCopy = new SheetObject(
                Name,
                X,
                Y,
                Width,
                Height,
                Angle,
                Visible,
                _groups
            );
            foreach (KeyValuePair<string, SheetVariableValue> pair in Variables.Snapshot()) {
                objectCopy.Variables.Set(pair.Key, pair.Value);
            }
            return objectCopy;
        }
        
        private static string[] CopyGroups(IEnumerable<string> groups) {
            if (groups == null) {
                return new string[0];
            }
            List<string> copy = new List<string>();
            foreach (string group in groups) {
                if (string.IsNullOrWhiteSpace(group)) {
                    throw new ArgumentException("Object groups cannot contain empty names.", nameof(groups));
                }
                if (!copy.Contains(group)) {
                    copy.Add(group);
                }
            }
            return copy.ToArray();
        }
    }
}
