// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Globalization;

namespace Metrician.Core.Scripting
{
    public static class ScriptParser
    {
        public static Script Parse(string source)
        {
            var script = new Script();
            string[] lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNumber = i + 1;
                string raw = lines[i].TrimEnd('\r');

                int commentStart = raw.IndexOf("//", StringComparison.Ordinal);
                string body;
                string? commentText;
                if (commentStart >= 0)
                {
                    body = raw[..commentStart];
                    string trimmedComment = raw[(commentStart + 2)..].Trim();
                    commentText = trimmedComment.Length > 0 ? trimmedComment : null;
                }
                else
                {
                    body = raw;
                    commentText = null;
                }

                string line = body.Trim();
                if (line.Length == 0) continue;

                if (line.Contains("->", StringComparison.Ordinal))
                    script.Connections.Add(ParseConnection(line, lineNumber, commentText));
                else if (line.Contains('='))
                    ApplyAssignment(line, lineNumber, script);
                else
                    throw new ScriptException(
                        $"Unrecognised statement: '{line}'", lineNumber);
            }
            return script;
        }

        private static void ApplyAssignment(string line, int lineNumber, Script script)
        {
            int equals = line.IndexOf('=');
            string left = line[..equals].Trim();
            string right = line[(equals + 1)..].Trim();
            if (left.Length == 0)
                throw new ScriptException("Missing identifier before '='.", lineNumber);
            if (right.Length == 0)
                throw new ScriptException("Missing value after '='.", lineNumber);

            int dot = left.IndexOf('.');
            if (dot < 0)
                script.Nodes.Add(new NodeDeclaration(left, right));
            else
                script.Properties.Add(new PropertyAssignment(
                    left[..dot].Trim(), left[(dot + 1)..].Trim(), right));
        }

        private static Connection ParseConnection(string line, int lineNumber, string? comment)
        {
            int arrow = line.IndexOf("->", StringComparison.Ordinal);
            string left = line[..arrow].Trim();
            string right = line[(arrow + 2)..].Trim();
            var (sourceId, sourceIndex) = SplitPinReference(left, lineNumber);
            var (targetId, targetIndex) = SplitPinReference(right, lineNumber);
            return new Connection(sourceId, sourceIndex, targetId, targetIndex, comment);
        }

        private static (string nodeId, int pinIndex) SplitPinReference(string text, int lineNumber)
        {
            int dot = text.IndexOf('.');
            if (dot < 0)
                throw new ScriptException(
                    $"Pin reference '{text}' must be of the form id.ordinal.", lineNumber);
            string nodeId = text[..dot].Trim();
            string indexText = text[(dot + 1)..].Trim();
            if (!int.TryParse(
                    indexText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int pinIndex))
                throw new ScriptException(
                    $"Pin reference '{text}' must use an ordinal index (got '{indexText}').",
                    lineNumber);
            if (pinIndex < 0)
                throw new ScriptException(
                    $"Pin ordinal must be non-negative, got '{pinIndex}'.", lineNumber);
            return (nodeId, pinIndex);
        }
    }
}
