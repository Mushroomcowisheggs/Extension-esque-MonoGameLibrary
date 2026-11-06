using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Maps the small event-sheet JSON format without reflection or serializer-generated types.
    /// </summary>
    internal sealed class SheetDocumentLoader {
        internal SheetDocument Load(string json) {
            if (string.IsNullOrWhiteSpace(json)) {
                throw new SheetFormatException("root", "JSON text is empty.");
            }
            
            try {
                using (JsonDocument documentJson = JsonDocument.Parse(json)) {
                    return LoadRoot(documentJson.RootElement);
                }
            } catch (JsonException exceptionJson) {
                throw new SheetFormatException("root", exceptionJson.Message);
            }
        }
        
        private static SheetDocument LoadRoot(JsonElement root) {
            if (root.ValueKind != JsonValueKind.Object) {
                throw new SheetFormatException("root", "The document must be a JSON object.");
            }
            
            List<string> errors = new List<string>();
            int version = ReadOptionalInt(root, "version", 1, "version", errors);
            if (version < 1) {
                errors.Add("version: must be at least 1.");
            }
            string nameSheet = ReadRequiredString(root, "name", "name", errors);
            if (nameSheet.Length == 0) {
                nameSheet = "Unnamed";
            }
            
            SheetVariables variables = ReadVariables(root, "variables", "variables", errors);
            List<SheetObject> objects = ReadObjects(root, "objects", "objects", errors);
            List<SheetEvent> events = ReadEvents(root, "events", "events", errors);
            
            if (errors.Count > 0) {
                throw new SheetFormatException(errors);
            }
            return new SheetDocument(version, nameSheet, objects, variables, events);
        }
        
        private static SheetVariables ReadVariables(
            JsonElement parent,
            string propertyName,
            string path,
            List<string> errors
        ) {
            SheetVariables variables = new SheetVariables();
            JsonElement element;
            if (!parent.TryGetProperty(propertyName, out element)) {
                return variables;
            }
            if (element.ValueKind != JsonValueKind.Object) {
                errors.Add(path + ": expected an object.");
                return variables;
            }
            
            foreach (JsonProperty property in element.EnumerateObject()) {
                string variablePath = path + "." + property.Name;
                try {
                    switch (property.Value.ValueKind) {
                        case JsonValueKind.Number:
                            double number;
                            if (property.Value.TryGetDouble(out number)) {
                                variables.SetNumber(property.Name, number);
                            } else {
                                errors.Add(variablePath + ": number is outside the supported range.");
                            }
                            break;
                        case JsonValueKind.String:
                            variables.SetText(property.Name, property.Value.GetString());
                            break;
                        case JsonValueKind.True:
                            variables.SetBoolean(property.Name, true);
                            break;
                        case JsonValueKind.False:
                            variables.SetBoolean(property.Name, false);
                            break;
                        default:
                            errors.Add(variablePath + ": expected a number, text, or Boolean.");
                            break;
                    }
                } catch (ArgumentException exceptionArgument) {
                    errors.Add(variablePath + ": " + exceptionArgument.Message);
                }
            }
            return variables;
        }
        
        private static List<SheetObject> ReadObjects(
            JsonElement parent,
            string propertyName,
            string path,
            List<string> errors
        ) {
            List<SheetObject> objects = new List<SheetObject>();
            JsonElement element;
            if (!TryReadArray(parent, propertyName, path, errors, out element)) {
                return objects;
            }
            
            int index = 0;
            foreach (JsonElement objectElement in element.EnumerateArray()) {
                string objectPath = path + "[" + index + "]";
                if (objectElement.ValueKind != JsonValueKind.Object) {
                    errors.Add(objectPath + ": expected an object.");
                    index += 1;
                    continue;
                }
                
                string nameObject = ReadRequiredString(objectElement, "name", objectPath + ".name", errors);
                double positionX = ReadOptionalDouble(objectElement, "x", 0.0, objectPath + ".x", errors);
                double positionY = ReadOptionalDouble(objectElement, "y", 0.0, objectPath + ".y", errors);
                double width = ReadOptionalDouble(objectElement, "width", 0.0, objectPath + ".width", errors);
                double height = ReadOptionalDouble(objectElement, "height", 0.0, objectPath + ".height", errors);
                double angle = ReadOptionalDouble(objectElement, "angle", 0.0, objectPath + ".angle", errors);
                bool flagVisible = ReadOptionalBoolean(
                    objectElement,
                    "visible",
                    true,
                    objectPath + ".visible",
                    errors
                );
                List<string> groups = ReadStringArray(
                    objectElement,
                    "groups",
                    objectPath + ".groups",
                    errors
                );
                SheetVariables variables = ReadVariables(
                    objectElement,
                    "variables",
                    objectPath + ".variables",
                    errors
                );
                
                if (nameObject.Length > 0) {
                    try {
                        SheetObject objectSheet = new SheetObject(
                            nameObject,
                            positionX,
                            positionY,
                            width,
                            height,
                            angle,
                            flagVisible,
                            groups
                        );
                        foreach (KeyValuePair<string, SheetVariableValue> pair in variables.Snapshot()) {
                            objectSheet.Variables.Set(pair.Key, pair.Value);
                        }
                        objects.Add(objectSheet);
                    } catch (ArgumentException exceptionArgument) {
                        errors.Add(objectPath + ": " + exceptionArgument.Message);
                    }
                }
                index += 1;
            }
            return objects;
        }
        
        private static List<SheetEvent> ReadEvents(
            JsonElement parent,
            string propertyName,
            string path,
            List<string> errors
        ) {
            List<SheetEvent> events = new List<SheetEvent>();
            JsonElement element;
            if (!TryReadArray(parent, propertyName, path, errors, out element)) {
                return events;
            }
            
            int index = 0;
            foreach (JsonElement eventElement in element.EnumerateArray()) {
                events.Add(ReadEvent(eventElement, path + "[" + index + "]", errors));
                index += 1;
            }
            return events;
        }
        
        private static SheetEvent ReadEvent(JsonElement element, string path, List<string> errors) {
            if (element.ValueKind != JsonValueKind.Object) {
                errors.Add(path + ": expected an object.");
                return new SheetEvent();
            }
            
            string comment = ReadOptionalString(element, "comment", string.Empty, path + ".comment", errors);
            bool flagOnce = ReadOptionalBoolean(element, "once", false, path + ".once", errors);
            List<SheetCondition> conditions = ReadConditions(element, path + ".conditions", errors);
            List<SheetAction> actions = ReadActions(element, path + ".actions", errors);
            List<SheetEvent> eventsSub = ReadEvents(element, "subEvents", path + ".subEvents", errors);
            return new SheetEvent(conditions, actions, eventsSub, comment, flagOnce);
        }
        
        private static List<SheetCondition> ReadConditions(
            JsonElement parent,
            string path,
            List<string> errors
        ) {
            List<SheetCondition> conditions = new List<SheetCondition>();
            JsonElement element;
            if (!TryReadArray(parent, "conditions", path, errors, out element)) {
                return conditions;
            }
            int index = 0;
            foreach (JsonElement conditionElement in element.EnumerateArray()) {
                SheetInstructionData data = ReadInstruction(conditionElement, path + "[" + index + "]", errors);
                conditions.Add(new SheetCondition(data.Id, data.Parameters));
                index += 1;
            }
            return conditions;
        }
        
        private static List<SheetAction> ReadActions(
            JsonElement parent,
            string path,
            List<string> errors
        ) {
            List<SheetAction> actions = new List<SheetAction>();
            JsonElement element;
            if (!TryReadArray(parent, "actions", path, errors, out element)) {
                return actions;
            }
            int index = 0;
            foreach (JsonElement actionElement in element.EnumerateArray()) {
                SheetInstructionData data = ReadInstruction(actionElement, path + "[" + index + "]", errors);
                actions.Add(new SheetAction(data.Id, data.Parameters));
                index += 1;
            }
            return actions;
        }
        
        private static SheetInstructionData ReadInstruction(
            JsonElement element,
            string path,
            List<string> errors
        ) {
            if (element.ValueKind != JsonValueKind.Object) {
                errors.Add(path + ": expected an object.");
                return new SheetInstructionData(
                    "invalid",
                    new SheetValue[0]
                );
            }
            
            string instructionId = ReadRequiredString(element, "id", path + ".id", errors);
            List<SheetValue> listParameters = ReadValues(element, path + ".parameters", errors);
            if (instructionId.Length == 0) {
                instructionId = "invalid";
            }
            return new SheetInstructionData(instructionId, listParameters);
        }
        
        private static List<SheetValue> ReadValues(
            JsonElement parent,
            string path,
            List<string> errors
        ) {
            List<SheetValue> values = new List<SheetValue>();
            JsonElement element;
            if (!TryReadArray(parent, "parameters", path, errors, out element)) {
                return values;
            }
            int index = 0;
            foreach (JsonElement valueElement in element.EnumerateArray()) {
                values.Add(ReadValue(valueElement, path + "[" + index + "]", errors));
                index += 1;
            }
            return values;
        }
        
        private static SheetValue ReadValue(JsonElement element, string path, List<string> errors) {
            try {
                switch (element.ValueKind) {
                    case JsonValueKind.Number:
                        double number;
                        if (element.TryGetDouble(out number)) {
                            return SheetValue.Number(number);
                        }
                        break;
                    case JsonValueKind.String:
                        return SheetValue.Text(element.GetString());
                    case JsonValueKind.True:
                        return SheetValue.Boolean(true);
                    case JsonValueKind.False:
                        return SheetValue.Boolean(false);
                    case JsonValueKind.Object:
                        string name;
                        if (TryReadTaggedString(element, "object", out name)) {
                            return SheetValue.Object(name);
                        }
                        if (TryReadTaggedString(element, "variable", out name)) {
                            return SheetValue.Variable(name);
                        }
                        if (TryReadTaggedString(element, "property", out name)) {
                            return SheetValue.Property(name);
                        }
                        break;
                }
            } catch (ArgumentException exceptionArgument) {
                errors.Add(path + ": " + exceptionArgument.Message);
                return SheetValue.Text(string.Empty);
            }
            
            errors.Add(path + ": expected a primitive value or {object|variable|property} tag.");
            return SheetValue.Text(string.Empty);
        }
        
        private static bool TryReadTaggedString(
            JsonElement element,
            string tag,
            out string value
        ) {
            JsonElement tagged;
            if (element.TryGetProperty(tag, out tagged) && tagged.ValueKind == JsonValueKind.String) {
                value = tagged.GetString();
                return !string.IsNullOrWhiteSpace(value);
            }
            value = string.Empty;
            return false;
        }
        
        private static List<string> ReadStringArray(
            JsonElement parent,
            string propertyName,
            string path,
            List<string> errors
        ) {
            List<string> values = new List<string>();
            JsonElement element;
            if (!TryReadArray(parent, propertyName, path, errors, out element)) {
                return values;
            }
            int index = 0;
            foreach (JsonElement valueElement in element.EnumerateArray()) {
                if (valueElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(valueElement.GetString())) {
                    errors.Add(path + "[" + index + "]: expected a non-empty string.");
                } else {
                    values.Add(valueElement.GetString());
                }
                index += 1;
            }
            return values;
        }
        
        private static bool TryReadArray(
            JsonElement parent,
            string propertyName,
            string path,
            List<string> errors,
            out JsonElement element
        ) {
            if (!parent.TryGetProperty(propertyName, out element)) {
                element = default(JsonElement);
                return false;
            }
            if (element.ValueKind != JsonValueKind.Array) {
                errors.Add(path + ": expected an array.");
                element = default(JsonElement);
                return false;
            }
            return true;
        }
        
        private static string ReadRequiredString(
            JsonElement parent,
            string propertyName,
            string path,
            List<string> errors
        ) {
            JsonElement element;
            if (!parent.TryGetProperty(propertyName, out element)) {
                errors.Add(path + ": property is required.");
                return string.Empty;
            }
            if (element.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(element.GetString())) {
                errors.Add(path + ": expected a non-empty string.");
                return string.Empty;
            }
            return element.GetString();
        }
        
        private static string ReadOptionalString(
            JsonElement parent,
            string propertyName,
            string valueDefault,
            string path,
            List<string> errors
        ) {
            JsonElement element;
            if (!parent.TryGetProperty(propertyName, out element)) {
                return valueDefault;
            }
            if (element.ValueKind != JsonValueKind.String) {
                errors.Add(path + ": expected a string.");
                return valueDefault;
            }
            return element.GetString();
        }
        
        private static int ReadOptionalInt(
            JsonElement parent,
            string propertyName,
            int valueDefault,
            string path,
            List<string> errors
        ) {
            JsonElement element;
            if (!parent.TryGetProperty(propertyName, out element)) {
                return valueDefault;
            }
            int value;
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out value)) {
                errors.Add(path + ": expected a 32-bit integer.");
                return valueDefault;
            }
            return value;
        }
        
        private static double ReadOptionalDouble(
            JsonElement parent,
            string propertyName,
            double valueDefault,
            string path,
            List<string> errors
        ) {
            JsonElement element;
            if (!parent.TryGetProperty(propertyName, out element)) {
                return valueDefault;
            }
            double value;
            if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out value)) {
                errors.Add(path + ": expected a number.");
                return valueDefault;
            }
            return value;
        }
        
        private static bool ReadOptionalBoolean(
            JsonElement parent,
            string propertyName,
            bool valueDefault,
            string path,
            List<string> errors
        ) {
            JsonElement element;
            if (!parent.TryGetProperty(propertyName, out element)) {
                return valueDefault;
            }
            if (element.ValueKind == JsonValueKind.True) {
                return true;
            }
            if (element.ValueKind == JsonValueKind.False) {
                return false;
            }
            errors.Add(path + ": expected a Boolean.");
            return valueDefault;
        }
        
        private readonly struct SheetInstructionData {
            internal SheetInstructionData(string id, IReadOnlyList<SheetValue> listParameters) {
                if (id == null) {
                    throw new ArgumentNullException(nameof(id));
                }
                Id = id;
                Parameters = listParameters;
            }
            
            internal string Id { get; }
            internal IReadOnlyList<SheetValue> Parameters { get; }
        }
    }
}
