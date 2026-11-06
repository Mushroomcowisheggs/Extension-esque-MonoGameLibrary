using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace MonoGameLibrary.Extensions.EventSheets {
    /// <summary>
    /// Produces human-readable event tables for authoring review and diagnostics.
    /// </summary>
    public static class SheetDocumentRenderer {
        /// <summary>Renders the document as a Markdown event table.</summary>
        /// <param name="document">The document to render.</param>
        /// <param name="vocabulary">The vocabulary used for author-facing labels; raw identifiers are shown when null.</param>
        /// <returns>The Markdown table.</returns>
        public static string RenderMarkdown(SheetDocument document, ISheetVocabulary vocabulary = null) {
            if (document == null) {
                throw new ArgumentNullException(nameof(document));
            }
            
            StringBuilder output = new StringBuilder();
            output.Append("# ").AppendLine(document.Name);
            output.AppendLine();
            output.AppendLine("| Event | Conditions | Actions | Once |");
            output.AppendLine("| --- | --- | --- | --- |");
            for (int i = 0; i < document.Events.Count; i += 1) {
                AppendMarkdownEvent(output, document.Events[i], (i + 1).ToString(CultureInfo.InvariantCulture), vocabulary);
            }
            return output.ToString();
        }
        
        /// <summary>Renders the document as an HTML event table.</summary>
        /// <param name="document">The document to render.</param>
        /// <param name="vocabulary">The vocabulary used for author-facing labels; raw identifiers are shown when null.</param>
        /// <returns>The HTML table fragment.</returns>
        public static string RenderHtml(SheetDocument document, ISheetVocabulary vocabulary = null) {
            if (document == null) {
                throw new ArgumentNullException(nameof(document));
            }
            
            StringBuilder output = new StringBuilder();
            output.Append("<h1>").Append(WebUtility.HtmlEncode(document.Name)).AppendLine("</h1>");
            output.AppendLine("<table>");
            output.AppendLine("<thead><tr><th>Event</th><th>Conditions</th><th>Actions</th><th>Once</th></tr></thead>");
            output.AppendLine("<tbody>");
            for (int i = 0; i < document.Events.Count; i += 1) {
                AppendHtmlEvent(output, document.Events[i], (i + 1).ToString(CultureInfo.InvariantCulture), 0, vocabulary);
            }
            output.AppendLine("</tbody>");
            output.AppendLine("</table>");
            return output.ToString();
        }
        
        private static void AppendMarkdownEvent(
            StringBuilder output,
            SheetEvent sheetEvent,
            string path,
            ISheetVocabulary vocabulary
        ) {
            output.Append("| ")
                .Append(path)
                .Append(" | ")
                .Append(EscapeMarkdown(FormatConditions(sheetEvent.Conditions, vocabulary)))
                .Append(" | ")
                .Append(EscapeMarkdown(FormatActions(sheetEvent.Actions, vocabulary)))
                .Append(" | ");
            AppendOnce(output, sheetEvent.Once);
            output.AppendLine(" |");
            if (!string.IsNullOrWhiteSpace(sheetEvent.Comment)) {
                output.Append("| ")
                    .Append(path)
                    .Append(" | _")
                    .Append(EscapeMarkdown(sheetEvent.Comment))
                    .AppendLine("_ | | |");
            }
            for (int i = 0; i < sheetEvent.SubEvents.Count; i += 1) {
                AppendMarkdownEvent(
                    output,
                    sheetEvent.SubEvents[i],
                    path + "." + (i + 1).ToString(CultureInfo.InvariantCulture),
                    vocabulary
                );
            }
        }
        
        private static void AppendHtmlEvent(
            StringBuilder output,
            SheetEvent sheetEvent,
            string path,
            int depth,
            ISheetVocabulary vocabulary
        ) {
            string indent = new string(' ', depth * 2);
            output.Append("<tr><td>")
                .Append(indent)
                .Append(WebUtility.HtmlEncode(path))
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(FormatConditions(sheetEvent.Conditions, vocabulary)))
                .Append("</td><td>")
                .Append(WebUtility.HtmlEncode(FormatActions(sheetEvent.Actions, vocabulary)))
                .Append("</td><td>");
            AppendOnce(output, sheetEvent.Once);
            output.AppendLine("</td></tr>");
            if (!string.IsNullOrWhiteSpace(sheetEvent.Comment)) {
                output.Append("<tr><td>")
                    .Append(indent)
                    .Append(WebUtility.HtmlEncode(path))
                    .Append("</td><td colspan=\"3\"><em>")
                    .Append(WebUtility.HtmlEncode(sheetEvent.Comment))
                    .AppendLine("</em></td></tr>");
            }
            for (int i = 0; i < sheetEvent.SubEvents.Count; i += 1) {
                AppendHtmlEvent(
                    output,
                    sheetEvent.SubEvents[i],
                    path + "." + (i + 1).ToString(CultureInfo.InvariantCulture),
                    depth + 1,
                    vocabulary
                );
            }
        }
        
        private static void AppendOnce(StringBuilder output, bool flagOnce) {
            if (flagOnce) {
                output.Append("yes");
            } else {
                output.Append("no");
            }
        }
        
        private static string FormatConditions(
            IReadOnlyList<SheetCondition> conditions,
            ISheetVocabulary vocabulary
        ) {
            if (conditions.Count == 0) {
                return "always";
            }
            List<string> values = new List<string>();
            for (int i = 0; i < conditions.Count; i += 1) {
                values.Add(FormatInstruction(
                    conditions[i].InstructionId,
                    conditions[i].Parameters,
                    vocabulary,
                    true
                ));
            }
            return string.Join(" and ", values);
        }
        
        private static string FormatActions(
            IReadOnlyList<SheetAction> actions,
            ISheetVocabulary vocabulary
        ) {
            if (actions.Count == 0) {
                return "—";
            }
            List<string> values = new List<string>();
            for (int i = 0; i < actions.Count; i += 1) {
                values.Add(FormatInstruction(
                    actions[i].InstructionId,
                    actions[i].Parameters,
                    vocabulary,
                    false
                ));
            }
            return string.Join(" then ", values);
        }
        
        private static string FormatInstruction(
            string instructionId,
            IReadOnlyList<SheetValue> listParameters,
            ISheetVocabulary vocabulary,
            bool flagCondition
        ) {
            SheetInstructionInfo info = FindInstruction(instructionId, vocabulary, flagCondition);
            if (info != null) {
                return FormatLabel(info, listParameters);
            }
            return FormatRaw(instructionId, listParameters);
        }
        
        private static SheetInstructionInfo FindInstruction(
            string instructionId,
            ISheetVocabulary vocabulary,
            bool flagCondition
        ) {
            if (vocabulary == null) {
                return null;
            }
            SheetInstructionInfo info;
            if (flagCondition) {
                SheetConditionHandler handler;
                if (vocabulary.TryGetCondition(instructionId, out info, out handler)) {
                    return info;
                }
                return null;
            }
            SheetActionHandler handlerAction;
            if (vocabulary.TryGetAction(instructionId, out info, out handlerAction)) {
                return info;
            }
            return null;
        }
        
        private static string FormatLabel(SheetInstructionInfo info, IReadOnlyList<SheetValue> listParameters) {
            string label = info.Label;
            for (int i = 0; i < listParameters.Count; i += 1) {
                string textValue;
                if (i < info.Parameters.Count) {
                    textValue = FormatValue(listParameters[i], info.Parameters[i].Kind);
                } else {
                    textValue = FormatValue(listParameters[i], SheetParameterKind.Any);
                }
                label = label.Replace("{" + i.ToString(CultureInfo.InvariantCulture) + "}", textValue);
            }
            return label;
        }
        
        private static string FormatValue(SheetValue value, SheetParameterKind kind) {
            switch (value.Kind) {
                case SheetValueKind.Number:
                    return value.NumberValue.ToString(CultureInfo.InvariantCulture);
                case SheetValueKind.Text:
                    return "'" + value.TextValue + "'";
                case SheetValueKind.Boolean:
                    if (value.BooleanValue) {
                        return "true";
                    }
                    return "false";
                case SheetValueKind.Object:
                case SheetValueKind.Variable:
                    return value.NameValue;
                case SheetValueKind.Property:
                    return value.NameValue;
                default:
                    return value.ToString();
            }
        }
        
        private static string FormatRaw(string instructionId, IReadOnlyList<SheetValue> listParameters) {
            if (listParameters.Count == 0) {
                return instructionId;
            }
            List<string> values = new List<string>();
            for (int i = 0; i < listParameters.Count; i += 1) {
                values.Add(listParameters[i].ToString());
            }
            return instructionId + "(" + string.Join(", ", values) + ")";
        }
        
        private static string EscapeMarkdown(string value) {
            return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
