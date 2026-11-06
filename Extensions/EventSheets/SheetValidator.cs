using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Validates instruction identifiers and parameter shapes before a sheet is compiled.
    /// </summary>
    internal static class SheetValidator {
        internal static void Validate(SheetDocument document, ISheetVocabulary vocabulary) {
            if (document == null) {
                throw new ArgumentNullException(nameof(document));
            }
            if (vocabulary == null) {
                throw new ArgumentNullException(nameof(vocabulary));
            }
            
            SheetReferenceIndex index = new SheetReferenceIndex(document);
            List<string> errors = new List<string>();
            ValidateEvents(document.Events, vocabulary, index, "events", errors);
            if (errors.Count > 0) {
                throw new SheetFormatException(errors);
            }
        }
        
        private static void ValidateEvents(
            IReadOnlyList<SheetEvent> events,
            ISheetVocabulary vocabulary,
            SheetReferenceIndex index,
            string path,
            List<string> errors
        ) {
            for (int i = 0; i < events.Count; i += 1) {
                SheetEvent eventSheet = events[i];
                string eventPath = path + "[" + i + "]";
                ValidateConditions(eventSheet.Conditions, vocabulary, index, eventPath, errors);
                ValidateActions(eventSheet.Actions, vocabulary, index, eventPath, errors);
                ValidateEvents(eventSheet.SubEvents, vocabulary, index, eventPath + ".subEvents", errors);
            }
        }
        
        private static void ValidateConditions(
            IReadOnlyList<SheetCondition> conditions,
            ISheetVocabulary vocabulary,
            SheetReferenceIndex index,
            string eventPath,
            List<string> errors
        ) {
            for (int i = 0; i < conditions.Count; i += 1) {
                SheetCondition condition = conditions[i];
                string path = eventPath + ".conditions[" + i + "]";
                SheetInstructionInfo info;
                SheetConditionHandler handler;
                if (!vocabulary.TryGetCondition(condition.InstructionId, out info, out handler)) {
                    errors.Add(path + ".id: unknown condition '" + condition.InstructionId + "'.");
                    continue;
                }
                ValidateParameters(condition.Parameters, info, index, path, errors);
            }
        }
        
        private static void ValidateActions(
            IReadOnlyList<SheetAction> actions,
            ISheetVocabulary vocabulary,
            SheetReferenceIndex index,
            string eventPath,
            List<string> errors
        ) {
            for (int i = 0; i < actions.Count; i += 1) {
                SheetAction action = actions[i];
                string path = eventPath + ".actions[" + i + "]";
                SheetInstructionInfo info;
                SheetActionHandler handler;
                if (!vocabulary.TryGetAction(action.InstructionId, out info, out handler)) {
                    errors.Add(path + ".id: unknown action '" + action.InstructionId + "'.");
                    continue;
                }
                ValidateParameters(action.Parameters, info, index, path, errors);
            }
        }
        
        private static void ValidateParameters(
            IReadOnlyList<SheetValue> values,
            SheetInstructionInfo info,
            SheetReferenceIndex index,
            string path,
            List<string> errors
        ) {
            int countRequired = 0;
            for (int i = 0; i < info.Parameters.Count; i += 1) {
                if (!info.Parameters[i].Optional) {
                    countRequired += 1;
                }
            }
            if (values.Count < countRequired || values.Count > info.Parameters.Count) {
                errors.Add(
                    path + ".parameters: expected " + countRequired + " to " +
                    info.Parameters.Count + " values, received " + values.Count + "."
                );
                return;
            }
            
            int countToCheck = values.Count;
            if (countToCheck > info.Parameters.Count) {
                countToCheck = info.Parameters.Count;
            }
            for (int i = 0; i < countToCheck; i += 1) {
                SheetParameterKind kindExpected = info.Parameters[i].Kind;
                if (!IsCompatible(values[i].Kind, kindExpected)) {
                    errors.Add(
                        path + ".parameters[" + i + "]: expected " + kindExpected +
                        ", received " + values[i].Kind + "."
                    );
                    continue;
                }
                ValidateReference(values[i], index, path + ".parameters[" + i + "]", errors);
            }
        }
        
        private static void ValidateReference(
            SheetValue value,
            SheetReferenceIndex index,
            string path,
            List<string> errors
        ) {
            switch (value.Kind) {
                case SheetValueKind.Object:
                    if (!index.HasObjectOrGroup(value.NameValue)) {
                        errors.Add(path + ": unknown object or group '" + value.NameValue + "'.");
                    }
                    break;
                case SheetValueKind.Variable:
                    if (!index.HasVariable(value.NameValue)) {
                        errors.Add(path + ": unknown variable '" + value.NameValue + "'.");
                    }
                    break;
                case SheetValueKind.Property:
                    string nameObject;
                    string nameProperty;
                    if (!SheetReferenceIndex.TryParseProperty(value.NameValue, out nameObject, out nameProperty)) {
                        errors.Add(path + ": expected a property path such as 'Player.X'.");
                        break;
                    }
                    if (!index.HasObject(nameObject)) {
                        errors.Add(path + ": unknown object '" + nameObject + "'.");
                        break;
                    }
                    if (!SheetReferenceIndex.IsPropertyName(nameProperty)) {
                        errors.Add(path + ": unknown property '" + nameProperty + "'.");
                    }
                    break;
            }
        }
        
        private static bool IsCompatible(SheetValueKind actual, SheetParameterKind expected) {
            switch (expected) {
                case SheetParameterKind.Any:
                    return true;
                case SheetParameterKind.Number:
                    return actual == SheetValueKind.Number ||
                           actual == SheetValueKind.Variable ||
                           actual == SheetValueKind.Property;
                case SheetParameterKind.Text:
                    return actual == SheetValueKind.Text ||
                           actual == SheetValueKind.Variable ||
                           actual == SheetValueKind.Property;
                case SheetParameterKind.Boolean:
                    return actual == SheetValueKind.Boolean ||
                           actual == SheetValueKind.Variable ||
                           actual == SheetValueKind.Property;
                case SheetParameterKind.Object:
                    return actual == SheetValueKind.Object;
                case SheetParameterKind.Variable:
                    return actual == SheetValueKind.Variable;
                case SheetParameterKind.Property:
                    return actual == SheetValueKind.Property;
                default:
                    return false;
            }
        }
        
        private sealed class SheetReferenceIndex {
            private readonly HashSet<string> _objects = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _groups = new HashSet<string>(StringComparer.Ordinal);
            private readonly HashSet<string> _variables = new HashSet<string>(StringComparer.Ordinal);
            
            internal SheetReferenceIndex(SheetDocument document) {
                if (document == null) {
                    throw new ArgumentNullException(nameof(document));
                }
                for (int i = 0; i < document.Objects.Count; i += 1) {
                    SheetObject objectSheet = document.Objects[i];
                    _objects.Add(objectSheet.Name);
                    for (int group = 0; group < objectSheet.Groups.Count; group += 1) {
                        _groups.Add(objectSheet.Groups[group]);
                    }
                }
                IReadOnlyList<KeyValuePair<string, SheetVariableValue>> variables = document.Variables.Snapshot();
                for (int i = 0; i < variables.Count; i += 1) {
                    _variables.Add(variables[i].Key);
                }
            }
            
            internal bool HasObject(string nameObject) {
                return _objects.Contains(nameObject);
            }
            
            internal bool HasObjectOrGroup(string name) {
                return _objects.Contains(name) || _groups.Contains(name);
            }
            
            internal bool HasVariable(string nameVariable) {
                return _variables.Contains(nameVariable);
            }
            
            internal static bool TryParseProperty(
                string pathProperty,
                out string nameObject,
                out string nameProperty
            ) {
                int separator = pathProperty.IndexOf('.');
                if (separator <= 0 || separator == pathProperty.Length - 1) {
                    nameObject = string.Empty;
                    nameProperty = string.Empty;
                    return false;
                }
                nameObject = pathProperty.Substring(0, separator);
                nameProperty = pathProperty.Substring(separator + 1);
                return true;
            }
            
            internal static bool IsPropertyName(string nameProperty) {
                return nameProperty == "X"
                    || nameProperty == "Y"
                    || nameProperty == "Width"
                    || nameProperty == "Height"
                    || nameProperty == "Angle"
                    || nameProperty == "Name"
                    || nameProperty == "Visible";
            }
        }
    }
}
