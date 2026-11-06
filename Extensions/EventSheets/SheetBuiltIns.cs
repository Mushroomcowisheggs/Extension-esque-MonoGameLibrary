using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Diagnostics;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Registers the small, platform-independent instruction set shipped with the extension.
    /// </summary>
    public static class SheetBuiltIns {
        /// <summary>Creates a vocabulary that already contains the built-in instructions.</summary>
        /// <returns>A new vocabulary with every built-in condition and action registered.</returns>
        public static ISheetVocabulary CreateVocabulary() {
            SheetVocabulary vocabulary = new SheetVocabulary();
            Register(vocabulary);
            return vocabulary;
        }
        
        /// <summary>Registers every built-in condition and action in the supplied vocabulary.</summary>
        /// <param name="vocabulary">The vocabulary that receives the built-in instructions.</param>
        public static void Register(ISheetVocabulary vocabulary) {
            if (vocabulary == null) {
                throw new ArgumentNullException(nameof(vocabulary));
            }
            
            vocabulary.RegisterCondition(
                new SheetInstructionInfo(
                    "always",
                    "Always",
                    SheetInstructionCategory.Condition
                ),
                Always
            );
            vocabulary.RegisterCondition(
                new SheetInstructionInfo(
                    "number-variable-equals",
                    "{0} equals {1}",
                    SheetInstructionCategory.Condition,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Variable", SheetParameterKind.Variable),
                        new SheetParameterInfo("Value", SheetParameterKind.Number)
                    }
                ),
                NumberVariableEquals
            );
            vocabulary.RegisterCondition(
                new SheetInstructionInfo(
                    "number-variable-greater",
                    "{0} is greater than {1}",
                    SheetInstructionCategory.Condition,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Variable", SheetParameterKind.Variable),
                        new SheetParameterInfo("Value", SheetParameterKind.Number)
                    }
                ),
                NumberVariableGreater
            );
            vocabulary.RegisterCondition(
                new SheetInstructionInfo(
                    "number-variable-less",
                    "{0} is less than {1}",
                    SheetInstructionCategory.Condition,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Variable", SheetParameterKind.Variable),
                        new SheetParameterInfo("Value", SheetParameterKind.Number)
                    }
                ),
                NumberVariableLess
            );
            vocabulary.RegisterCondition(
                new SheetInstructionInfo(
                    "object-visible",
                    "{0} visible is {1}",
                    SheetInstructionCategory.Condition,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Object", SheetParameterKind.Object),
                        new SheetParameterInfo("Visible", SheetParameterKind.Boolean)
                    }
                ),
                ObjectVisible
            );
            vocabulary.RegisterCondition(
                new SheetInstructionInfo(
                    "object-exists",
                    "{0} exists",
                    SheetInstructionCategory.Condition,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Object", SheetParameterKind.Object)
                    }
                ),
                ObjectExists
            );
            vocabulary.RegisterCondition(
                new SheetInstructionInfo(
                    "time-after",
                    "Time is at least {0} seconds",
                    SheetInstructionCategory.Condition,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Seconds", SheetParameterKind.Number)
                    }
                ),
                TimeAfter
            );
            vocabulary.RegisterCondition(
                new SheetInstructionInfo(
                    "random-chance",
                    "Random chance is below {0} percent",
                    SheetInstructionCategory.Condition,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Percent", SheetParameterKind.Number)
                    }
                ),
                RandomChance
            );
            
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "set-number-variable",
                    "Set {0} to {1}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Variable", SheetParameterKind.Variable),
                        new SheetParameterInfo("Value", SheetParameterKind.Number)
                    }
                ),
                SetNumberVariable
            );
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "add-number-variable",
                    "Add {1} to {0}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Variable", SheetParameterKind.Variable),
                        new SheetParameterInfo("Value", SheetParameterKind.Number)
                    }
                ),
                AddNumberVariable
            );
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "set-text-variable",
                    "Set {0} to {1}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Variable", SheetParameterKind.Variable),
                        new SheetParameterInfo("Value", SheetParameterKind.Text)
                    }
                ),
                SetTextVariable
            );
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "set-boolean-variable",
                    "Set {0} to {1}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Variable", SheetParameterKind.Variable),
                        new SheetParameterInfo("Value", SheetParameterKind.Boolean)
                    }
                ),
                SetBooleanVariable
            );
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "set-position",
                    "Set {0} position to {1}, {2}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Object", SheetParameterKind.Object),
                        new SheetParameterInfo("X", SheetParameterKind.Number),
                        new SheetParameterInfo("Y", SheetParameterKind.Number)
                    }
                ),
                SetPosition
            );
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "move-by",
                    "Move {0} by {1}, {2}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Object", SheetParameterKind.Object),
                        new SheetParameterInfo("X", SheetParameterKind.Number),
                        new SheetParameterInfo("Y", SheetParameterKind.Number)
                    }
                ),
                MoveBy
            );
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "set-visible",
                    "Set {0} visible to {1}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Object", SheetParameterKind.Object),
                        new SheetParameterInfo("Visible", SheetParameterKind.Boolean)
                    }
                ),
                SetVisible
            );
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "destroy-object",
                    "Destroy {0}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Object", SheetParameterKind.Object)
                    }
                ),
                DestroyObject
            );
            vocabulary.RegisterAction(
                new SheetInstructionInfo(
                    "log",
                    "Log {0}",
                    SheetInstructionCategory.Action,
                    new SheetParameterInfo[] {
                        new SheetParameterInfo("Message", SheetParameterKind.Text)
                    }
                ),
                LogMessage
            );
        }
        
        private static bool Always(in SheetCall call) {
            return true;
        }
        
        private static bool NumberVariableEquals(in SheetCall call) {
            return ReadNumberVariable(call, 0) == call.GetNumber(1);
        }
        
        private static bool NumberVariableGreater(in SheetCall call) {
            return ReadNumberVariable(call, 0) > call.GetNumber(1);
        }
        
        private static bool NumberVariableLess(in SheetCall call) {
            return ReadNumberVariable(call, 0) < call.GetNumber(1);
        }
        
        private static bool ObjectVisible(in SheetCall call) {
            string nameObject = call.GetObjectName(0);
            bool flagVisibleExpected = call.GetBoolean(1);
            IReadOnlyList<SheetObject> objects = call.Context.PickObjects(
                nameObject,
                delegate(SheetObject objectSheet) {
                    return objectSheet.Visible == flagVisibleExpected;
                }
            );
            return objects.Count > 0;
        }
        
        private static bool ObjectExists(in SheetCall call) {
            string nameObject = call.GetObjectName(0);
            IReadOnlyList<SheetObject> objects = call.Context.PickObjects(
                nameObject,
                delegate(SheetObject objectSheet) { return true; }
            );
            return objects.Count > 0;
        }
        
        private static bool TimeAfter(in SheetCall call) {
            return call.Context.FrameTime.TotalTimeSpan.TotalSeconds >= call.GetNumber(0);
        }
        
        private static bool RandomChance(in SheetCall call) {
            double percent = call.GetNumber(0);
            if (percent <= 0.0) {
                return false;
            }
            if (percent >= 100.0) {
                return true;
            }
            return call.Context.Random.NextDouble() * 100.0 < percent;
        }
        
        private static void SetNumberVariable(in SheetCall call) {
            call.Context.Variables.SetNumber(call.GetVariableName(0), call.GetNumber(1));
        }
        
        private static void AddNumberVariable(in SheetCall call) {
            string nameVariable = call.GetVariableName(0);
            double numberCurrent;
            if (!call.Context.Variables.TryGetNumber(nameVariable, out numberCurrent)) {
                numberCurrent = 0.0;
            }
            call.Context.Variables.SetNumber(nameVariable, numberCurrent + call.GetNumber(1));
        }
        
        private static void SetTextVariable(in SheetCall call) {
            call.Context.Variables.SetText(call.GetVariableName(0), call.GetText(1));
        }
        
        private static void SetBooleanVariable(in SheetCall call) {
            call.Context.Variables.SetBoolean(call.GetVariableName(0), call.GetBoolean(1));
        }
        
        private static void SetPosition(in SheetCall call) {
            IReadOnlyList<SheetObject> objects = call.GetObjects(0);
            double positionX = call.GetNumber(1);
            double positionY = call.GetNumber(2);
            for (int i = 0; i < objects.Count; i += 1) {
                objects[i].X = positionX;
                objects[i].Y = positionY;
            }
        }
        
        private static void MoveBy(in SheetCall call) {
            IReadOnlyList<SheetObject> objects = call.GetObjects(0);
            double offsetX = call.GetNumber(1);
            double offsetY = call.GetNumber(2);
            for (int i = 0; i < objects.Count; i += 1) {
                objects[i].X += offsetX;
                objects[i].Y += offsetY;
            }
        }
        
        private static void SetVisible(in SheetCall call) {
            IReadOnlyList<SheetObject> objects = call.GetObjects(0);
            bool flagVisible = call.GetBoolean(1);
            for (int i = 0; i < objects.Count; i += 1) {
                objects[i].Visible = flagVisible;
            }
        }
        
        private static void DestroyObject(in SheetCall call) {
            IReadOnlyList<SheetObject> objects = call.GetObjects(0);
            for (int i = 0; i < objects.Count; i += 1) {
                call.Context.Objects.Remove(objects[i]);
            }
        }
        
        private static void LogMessage(in SheetCall call) {
            call.Context.Logger.Info(call.GetText(0));
        }
        
        private static double ReadNumberVariable(in SheetCall call, int index) {
            string nameVariable = call.GetVariableName(index);
            double number;
            if (!call.Context.Variables.TryGetNumber(nameVariable, out number)) {
                return 0.0;
            }
            return number;
        }
    }
}
