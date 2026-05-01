// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;
using Metrician.Core.Scripting;

namespace Metrician.Core.ScriptBinding
{
    public static class ScriptIntrospector
    {
        public static Script Introspect(
            IGraphWorld world,
            ITemplateNameSystem? templateNames = null)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));

            var script = new Script();
            var idByNode = new Dictionary<NodeId, string>();
            var counters = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var node in world.Nodes.All)
            {
                string typeName = templateNames?.Get(node.Id) ?? node.Title;
                string baseName = MakeBaseName(typeName);
                counters.TryGetValue(baseName, out int n);
                counters[baseName] = n + 1;
                string id = $"{baseName}{n + 1}";
                idByNode[node.Id] = id;
                script.Nodes.Add(new NodeDeclaration(id, typeName));
            }

            foreach (var node in world.Nodes.All)
            {
                string scriptId = idByNode[node.Id];

                var position = world.Layout.Get(node.Id);
                if (position is { } p)
                    script.Properties.Add(new PropertyAssignment(
                        scriptId,
                        ScriptApplier.PositionPropertyName,
                        PropertyValueText.Format(p)));

                foreach (var property in world.Properties.PropertiesOf(node.Id))
                {
                    if (!PropertyValueText.IsSupported(property.Type)) continue;
                    script.Properties.Add(new PropertyAssignment(
                        scriptId,
                        property.Name,
                        PropertyValueText.Format(property.Value)));
                }
            }

            foreach (var node in world.Nodes.All)
            {
                if (!idByNode.TryGetValue(node.Id, out var targetId)) continue;
                int targetIndex = 0;
                foreach (var pin in world.Pins.Inputs(node.Id))
                {
                    if (world.Wires.SourceOf(pin.Id) is { } source &&
                        idByNode.TryGetValue(source.Owner, out var sourceId))
                    {
                        int sourceIndex = IndexOfOutput(world, source);
                        if (sourceIndex >= 0)
                            script.Connections.Add(new Connection(
                                sourceId, sourceIndex,
                                targetId, targetIndex,
                                $"{source.Name} -> {pin.Id.Name}"));
                    }
                    targetIndex++;
                }
            }

            return script;
        }

        private static int IndexOfOutput(IGraphWorld world, PinId pin)
        {
            int i = 0;
            foreach (var p in world.Pins.Outputs(pin.Owner))
            {
                if (p.Id == pin) return i;
                i++;
            }
            return -1;
        }

        private static string MakeBaseName(string typeName)
        {
            const string nodeTemplate = "NodeTemplate";
            const string template = "Template";
            const string node = "Node";
            string s = typeName ?? string.Empty;

            if (s.EndsWith(nodeTemplate, StringComparison.Ordinal) && s.Length > nodeTemplate.Length)
                s = s[..^nodeTemplate.Length];
            else if (s.EndsWith(template, StringComparison.Ordinal) && s.Length > template.Length)
                s = s[..^template.Length];
            else if (s.EndsWith(node, StringComparison.Ordinal) && s.Length > node.Length)
                s = s[..^node.Length];

            s = SanitiseIdentifier(s);
            if (s.Length == 0) return "node";
            return char.ToLowerInvariant(s[0]) + s[1..];
        }

        private static string SanitiseIdentifier(string s)
        {
            Span<char> buffer = stackalloc char[s.Length];
            int n = 0;
            foreach (char c in s)
                if (char.IsLetterOrDigit(c) || c == '_')
                    buffer[n++] = c;
            return new string(buffer[..n]);
        }
    }
}
