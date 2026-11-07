using System;
using System.Globalization;
using System.Collections.Generic;
using MonoGameLibrary.Core.Diagnostics;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Mutable frame-scoped state visible to event instruction handlers.
    /// </summary>
    public sealed class SheetContext {
        private readonly ILogger _logger;
        private readonly Random _random;
        private readonly Dictionary<string, SheetObject[]> _pickedObjects =
            new Dictionary<string, SheetObject[]>(StringComparer.Ordinal);
        private readonly Stack<Dictionary<string, SheetObject[]>> _selectionScopes =
            new Stack<Dictionary<string, SheetObject[]>>();
        private readonly Dictionary<string, SheetPropertyRef> _properties =
            new Dictionary<string, SheetPropertyRef>(StringComparer.Ordinal);
        private FrameTime _timeFrame;
        private long _numberFrame;
        
        /// <summary>Initializes the frame-scoped context.</summary>
        /// <param name="objects">The object instances the sheet addresses.</param>
        /// <param name="variables">The sheet's variables.</param>
        /// <param name="logger">The logger used by instruction handlers.</param>
        /// <param name="random">The random source shared by the frame.</param>
        public SheetContext(
            SheetObjects objects,
            SheetVariables variables,
            ILogger logger,
            Random random
        ) {
            if (objects == null) {
                throw new ArgumentNullException(nameof(objects));
            }
            if (variables == null) {
                throw new ArgumentNullException(nameof(variables));
            }
            if (logger == null) {
                throw new ArgumentNullException(nameof(logger));
            }
            if (random == null) {
                throw new ArgumentNullException(nameof(random));
            }
            Objects = objects;
            Variables = variables;
            _logger = logger;
            _random = random;
            _timeFrame = FrameTime.Zero;
        }
        
        /// <summary>Gets the object set the instructions read and change.</summary>
        public SheetObjects Objects { get; }
        
        /// <summary>Gets the global variables of the running sheet.</summary>
        public SheetVariables Variables { get; }
        
        /// <summary>Gets the timing information of the frame being executed.</summary>
        public FrameTime FrameTime {
            get { return _timeFrame; }
        }
        
        /// <summary>Gets the number of the frame being executed, starting at one.</summary>
        public long FrameNumber {
            get { return _numberFrame; }
        }
        
        /// <summary>Gets the random source instructions may use.</summary>
        public Random Random {
            get { return _random; }
        }
        
        /// <summary>Gets the logger available to instructions and the runtime.</summary>
        public ILogger Logger {
            get { return _logger; }
        }
        
        internal void BeginFrame(FrameTime timeFrame, long numberFrame) {
            _timeFrame = timeFrame;
            _numberFrame = numberFrame;
            _pickedObjects.Clear();
            _selectionScopes.Clear();
        }
        
        /// <summary>
        /// Gets the currently objectsPicked objects with a given name or group. Before a condition picks a
        /// subset, this returns every matching object.
        /// </summary>
        /// <param name="nameObject">The object or group name.</param>
        /// <returns>The objectsPicked objects when a condition narrowed them; otherwise every match.</returns>
        public IReadOnlyList<SheetObject> GetObjects(string nameObject) {
            if (string.IsNullOrWhiteSpace(nameObject)) {
                throw new ArgumentException("A non-empty object or group name is required.", nameof(nameObject));
            }
            SheetObject[] objectsPicked;
            if (_pickedObjects.TryGetValue(nameObject, out objectsPicked)) {
                return objectsPicked;
            }
            IReadOnlyList<SheetObject> objects = Objects.GetByName(nameObject);
            if (objects.Count == 0) {
                objects = Objects.GetByGroup(nameObject);
            }
            return objects;
        }
        
        /// <summary>
        /// Filters the current selection for a name or group. Sub-events inherit the resulting selection.
        /// </summary>
        /// <param name="nameObject">The object or group name.</param>
        /// <param name="predicateObject">The test an object must satisfy to stay objectsSelected.</param>
        /// <returns>The newly objectsPicked objects.</returns>
        public IReadOnlyList<SheetObject> PickObjects(
            string nameObject,
            Func<SheetObject, bool> predicateObject
        ) {
            if (predicateObject == null) {
                throw new ArgumentNullException(nameof(predicateObject));
            }
            IReadOnlyList<SheetObject> objectsCandidates = GetObjects(nameObject);
            List<SheetObject> objectsSelected = new List<SheetObject>();
            for (int i = 0; i < objectsCandidates.Count; i += 1) {
                if (predicateObject(objectsCandidates[i])) {
                    objectsSelected.Add(objectsCandidates[i]);
                }
            }
            SheetObject[] objectsResult = objectsSelected.ToArray();
            _pickedObjects[nameObject] = objectsResult;
            return objectsResult;
        }
        
        internal void BeginRootEvent() {
            _pickedObjects.Clear();
            _selectionScopes.Clear();
        }
        
        internal void BeginSubEvent() {
            _selectionScopes.Push(CopySelections());
        }
        
        internal void EndEvent(bool flagRoot) {
            if (flagRoot) {
                _pickedObjects.Clear();
                _selectionScopes.Clear();
                return;
            }
            Dictionary<string, SheetObject[]> selection = _selectionScopes.Pop();
            _pickedObjects.Clear();
            foreach (KeyValuePair<string, SheetObject[]> pair in selection) {
                _pickedObjects.Add(pair.Key, pair.Value);
            }
        }
        
        private Dictionary<string, SheetObject[]> CopySelections() {
            Dictionary<string, SheetObject[]> copy =
                new Dictionary<string, SheetObject[]>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, SheetObject[]> pair in _pickedObjects) {
                copy.Add(pair.Key, pair.Value);
            }
            return copy;
        }
        
        internal bool TryResolveNumber(SheetValue value, out double number) {
            switch (value.Kind) {
                case SheetValueKind.Number:
                    number = value.NumberValue;
                    return true;
                case SheetValueKind.Boolean:
                    number = value.BooleanValue ? 1.0 : 0.0;
                    return true;
                case SheetValueKind.Variable:
                    return Variables.TryGetNumber(value.NameValue, out number);
                case SheetValueKind.Property:
                    return TryResolvePropertyNumber(value.NameValue, out number);
                case SheetValueKind.Text:
                    return double.TryParse(
                        value.TextValue,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out number
                    );
                default:
                    number = 0.0;
                    return false;
            }
        }
        
        internal bool TryResolveText(SheetValue value, out string text) {
            switch (value.Kind) {
                case SheetValueKind.Text:
                    text = value.TextValue;
                    return true;
                case SheetValueKind.Variable:
                    if (Variables.TryGetText(value.NameValue, out text)) {
                        return true;
                    }
                    SheetVariableValue valueVariable;
                    if (Variables.TryGet(value.NameValue, out valueVariable)) {
                        text = VariableToText(valueVariable);
                        return true;
                    }
                    break;
                case SheetValueKind.Property:
                    return TryResolvePropertyText(value.NameValue, out text);
                case SheetValueKind.Number:
                    text = value.NumberValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SheetValueKind.Boolean:
                    text = value.BooleanValue ? "true" : "false";
                    return true;
            }
            text = string.Empty;
            return false;
        }
        
        internal bool TryResolveBoolean(SheetValue value, out bool flagBoolean) {
            switch (value.Kind) {
                case SheetValueKind.Boolean:
                    flagBoolean = value.BooleanValue;
                    return true;
                case SheetValueKind.Number:
                    flagBoolean = value.NumberValue != 0.0;
                    return true;
                case SheetValueKind.Variable:
                    return Variables.TryGetBoolean(value.NameValue, out flagBoolean);
                case SheetValueKind.Property:
                    return TryResolvePropertyBoolean(value.NameValue, out flagBoolean);
                case SheetValueKind.Text:
                    return bool.TryParse(value.TextValue, out flagBoolean);
                default:
                    flagBoolean = false;
                    return false;
            }
        }
        
        private bool TryResolvePropertyNumber(string pathProperty, out double number) {
            SheetObject objectSheet;
            string nameProperty;
            if (!TryFindProperty(pathProperty, out objectSheet, out nameProperty)) {
                number = 0.0;
                return false;
            }
            switch (nameProperty) {
                case "X":
                    number = objectSheet.X;
                    return true;
                case "Y":
                    number = objectSheet.Y;
                    return true;
                case "Width":
                    number = objectSheet.Width;
                    return true;
                case "Height":
                    number = objectSheet.Height;
                    return true;
                case "Angle":
                    number = objectSheet.Angle;
                    return true;
                default:
                    number = 0.0;
                    return false;
            }
        }
        
        private bool TryResolvePropertyText(string pathProperty, out string text) {
            SheetObject objectSheet;
            string nameProperty;
            if (!TryFindProperty(pathProperty, out objectSheet, out nameProperty)) {
                text = string.Empty;
                return false;
            }
            if (nameProperty == "Name") {
                text = objectSheet.Name;
                return true;
            }
            text = string.Empty;
            return false;
        }
        
        private bool TryResolvePropertyBoolean(string pathProperty, out bool flagBoolean) {
            SheetObject objectSheet;
            string nameProperty;
            if (!TryFindProperty(pathProperty, out objectSheet, out nameProperty)) {
                flagBoolean = false;
                return false;
            }
            if (nameProperty == "Visible") {
                flagBoolean = objectSheet.Visible;
                return true;
            }
            flagBoolean = false;
            return false;
        }
        
        private bool TryFindProperty(
            string pathProperty,
            out SheetObject objectSheet,
            out string nameProperty
        ) {
            SheetPropertyRef refProperty;
            if (!_properties.TryGetValue(pathProperty, out refProperty)) {
                int separator = pathProperty.IndexOf('.');
                if (separator <= 0 || separator == pathProperty.Length - 1) {
                    objectSheet = null;
                    nameProperty = string.Empty;
                    return false;
                }
                refProperty = new SheetPropertyRef(
                    pathProperty.Substring(0, separator),
                    pathProperty.Substring(separator + 1)
                );
                _properties.Add(pathProperty, refProperty);
            }
            nameProperty = refProperty.NameProperty;
            return Objects.TryGetFirst(refProperty.NameObject, out objectSheet);
        }
        
        private static string VariableToText(SheetVariableValue valueVariable) {
            switch (valueVariable.Kind) {
                case SheetVariableKind.Number:
                    return valueVariable.NumberValue.ToString(CultureInfo.InvariantCulture);
                case SheetVariableKind.Text:
                    return valueVariable.TextValue;
                case SheetVariableKind.Boolean:
                    return valueVariable.BooleanValue ? "true" : "false";
                default:
                    return string.Empty;
            }
        }
    }
}
