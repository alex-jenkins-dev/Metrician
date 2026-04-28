// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Text;

namespace Metrician.Script
{
    /// <summary>
    /// Converts between script text and the <see cref="GraphScript"/> model.
    /// Surface-syntax only; type resolution and graph mutation live elsewhere.
    /// </summary>
    public static class GraphScriptText
    {
        /// <summary>
        /// Reads and parses a script file.
        /// IO failures surface as <see cref="ScriptException"/>.
        /// </summary>
        public static GraphScript ReadFile(string path)
        {
            string text;
            try { text = File.ReadAllText(path); }
            catch (Exception ex)
            {
                throw new ScriptException($"Could not read script '{path}': {ex.Message}");
            }
            return Parse(text);
        }

        /// <summary>Serialises the model and writes it to disk.</summary>
        public static void WriteFile(string path, GraphScript script)
        {
            try { File.WriteAllText(path, Format(script)); }
            catch (Exception ex)
            {
                throw new ScriptException($"Could not write script '{path}': {ex.Message}");
            }
        }

        /// <summary>
        /// Parses script text.
        /// Throws <see cref="ScriptException"/> with a 1-based line number on malformed input.
        /// </summary>
        public static GraphScript Parse(string source)
        {
            var script = new GraphScript();
            var lines = source.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                int lineNo = i + 1;
                string raw = lines[i].TrimEnd('\r');
                int slash = raw.IndexOf("//", StringComparison.Ordinal);
                if (slash >= 0) raw = raw[..slash];
                string line = raw.Trim();
                if (line.Length == 0) continue;

                if (line.Contains("->", StringComparison.Ordinal))
                    script.Connections.Add(ParseConnection(line, lineNo));
                else if (line.Contains('='))
                    HandleAssign(line, lineNo, script);
                else
                    throw new ScriptException(
                        $"Unrecognised statement: '{line}'", lineNo);
            }
            return script;
        }

        /// <summary>
        /// Serialises the model.
        /// Layout: nodes, blank line, properties, blank line, connections; empty sections are omitted.
        /// </summary>
        public static string Format(GraphScript script)
        {
            var sb = new StringBuilder();

            foreach (var n in script.Nodes)
                sb.Append(n.Id).Append(" = ").AppendLine(n.TypeName);

            if (script.Properties.Count > 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                foreach (var p in script.Properties)
                    sb.Append(p.NodeId).Append('.').Append(p.PropertyName)
                      .Append(" = ").AppendLine(p.Value);
            }

            if (script.Connections.Count > 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                foreach (var c in script.Connections)
                    sb.Append(c.SourceId).Append('.').Append(c.SourcePin)
                      .Append(" -> ")
                      .Append(c.TargetId).Append('.').AppendLine(c.TargetPin);
            }

            return sb.ToString();
        }

        // Node declaration vs property assignment, distinguished by a '.' on the LHS.
        private static void HandleAssign(string line, int lineNo, GraphScript script)
        {
            int eq = line.IndexOf('=');
            string lhs = line[..eq].Trim();
            string rhs = line[(eq + 1)..].Trim();
            if (lhs.Length == 0)
                throw new ScriptException("Missing identifier before '='.", lineNo);
            if (rhs.Length == 0)
                throw new ScriptException("Missing value after '='.", lineNo);

            int dot = lhs.IndexOf('.');
            if (dot < 0)
            {
                script.Nodes.Add(new NodeDecl(lhs, rhs));
            }
            else
            {
                string id = lhs[..dot].Trim();
                string prop = lhs[(dot + 1)..].Trim();
                script.Properties.Add(new PropertyAssignment(id, prop, rhs));
            }
        }

        private static Connection ParseConnection(string line, int lineNo)
        {
            int arrow = line.IndexOf("->", StringComparison.Ordinal);
            string lhs = line[..arrow].Trim();
            string rhs = line[(arrow + 2)..].Trim();
            var (srcId, srcPin) = SplitPinRef(lhs, lineNo);
            var (dstId, dstPin) = SplitPinRef(rhs, lineNo);
            return new Connection(srcId, srcPin, dstId, dstPin);
        }

        private static (string id, string pinRef) SplitPinRef(string text, int lineNo)
        {
            int dot = text.IndexOf('.');
            if (dot < 0)
                throw new ScriptException(
                    $"Pin reference '{text}' must be of the form id.pin.", lineNo);
            return (text[..dot].Trim(), text[(dot + 1)..].Trim());
        }
    }
}
