// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Globalization;
using System.Reflection;
using Metrician.Graph.Contracts;

namespace Metrician.Script
{
    /// <summary>
    /// Materialises a <see cref="GraphScript"/> into live nodes and wires them.
    /// Returns nodes in declaration order; the caller adds them to a
    /// <c>NodeGraph</c> and triggers layout or change events.
    /// </summary>
    public static class GraphScriptApplier
    {
        public static IReadOnlyList<INode> Apply(
            GraphScript script, IReadOnlyList<ScriptNodeFactory> available)
        {
            var (byShort, byFull) = BuildLookup(available);
            var nodesById = new Dictionary<string, INode>(StringComparer.Ordinal);
            var ordered = new List<INode>();

            foreach (var decl in script.Nodes)
            {
                if (nodesById.ContainsKey(decl.Id))
                    throw new ScriptException(
                        $"Node id '{decl.Id}' already defined.");
                var factory = ResolveFactory(decl.TypeName, byShort, byFull);
                INode node;
                try { node = factory.Create(); }
                catch (Exception ex)
                {
                    throw new ScriptException(
                        $"Failed to instantiate '{decl.TypeName}': {ex.Message}");
                }
                nodesById[decl.Id] = node;
                ordered.Add(node);
            }

            foreach (var p in script.Properties)
            {
                if (!nodesById.TryGetValue(p.NodeId, out var node))
                    throw new ScriptException($"Unknown node id '{p.NodeId}'.");
                SetProperty(node, p.PropertyName, p.Value);
            }

            // Rebuild IDynamicPins layouts once final property state is in place,
            // so the connection phase sees the up-to-date pin set.
            foreach (var node in ordered)
                if (node is IDynamicPins d) d.RebuildPins();

            foreach (var c in script.Connections)
            {
                if (!nodesById.TryGetValue(c.SourceId, out var src))
                    throw new ScriptException($"Unknown node id '{c.SourceId}'.");
                if (!nodesById.TryGetValue(c.TargetId, out var dst))
                    throw new ScriptException($"Unknown node id '{c.TargetId}'.");

                var outPin = ResolveOutputPin(src, c.SourcePin);
                var inPin  = ResolveInputPin(dst,  c.TargetPin);

                if (!inPin.TryConnect(outPin))
                    throw new ScriptException(
                        $"Type mismatch: cannot connect '{c.SourceId}.{c.SourcePin}' " +
                        $"({outPin.ValueType.Name}) to '{c.TargetId}.{c.TargetPin}' " +
                        $"({inPin.ValueType.Name}).");

                if (dst is IVariadicInputs v) v.CompactInputs();
            }

            return ordered;
        }

        private static (Dictionary<string, List<ScriptNodeFactory>> byShort,
                        Dictionary<string, ScriptNodeFactory> byFull)
            BuildLookup(IReadOnlyList<ScriptNodeFactory> available)
        {
            var byShort = new Dictionary<string, List<ScriptNodeFactory>>(StringComparer.Ordinal);
            var byFull = new Dictionary<string, ScriptNodeFactory>(StringComparer.Ordinal);
            foreach (var f in available)
            {
                if (!byShort.TryGetValue(f.ShortName, out var list))
                    byShort[f.ShortName] = list = new List<ScriptNodeFactory>();
                list.Add(f);
                byFull[f.FullName] = f;
            }
            return (byShort, byFull);
        }

        private static ScriptNodeFactory ResolveFactory(
            string typeName,
            Dictionary<string, List<ScriptNodeFactory>> byShort,
            Dictionary<string, ScriptNodeFactory> byFull)
        {
            if (byFull.TryGetValue(typeName, out var exact)) return exact;

            if (byShort.TryGetValue(typeName, out var matches))
            {
                if (matches.Count == 1) return matches[0];
                var qualifiers = string.Join(", ", matches.Select(m => m.FullName));
                throw new ScriptException(
                    $"Node type '{typeName}' is ambiguous. " +
                    $"Qualify with one of: {qualifiers}.");
            }

            throw new ScriptException(
                $"Unknown node type '{typeName}'. " +
                $"Plugin not loaded, or name misspelled?");
        }

        private static INodeOutput ResolveOutputPin(INode node, string pinRef)
        {
            if (int.TryParse(pinRef, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
            {
                if (idx < 0 || idx >= node.Outputs.Count)
                    throw new ScriptException(
                        $"Output index {idx} out of range on node '{node.Title}' " +
                        $"(has {node.Outputs.Count}).");
                return node.Outputs[idx];
            }
            foreach (var p in node.Outputs)
                if (string.Equals(p.Name, pinRef, StringComparison.Ordinal))
                    return p;
            throw new ScriptException(
                $"No output pin named '{pinRef}' on node '{node.Title}'.");
        }

        private static INodeInput ResolveInputPin(INode node, string pinRef)
        {
            if (int.TryParse(pinRef, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
            {
                if (idx < 0 || idx >= node.Inputs.Count)
                    throw new ScriptException(
                        $"Input index {idx} out of range on node '{node.Title}' " +
                        $"(has {node.Inputs.Count}).");
                return node.Inputs[idx];
            }
            // Prefer the first unconnected pin so variadic inputs target the
            // current spare rather than re-binding an already-wired pin.
            INodeInput? firstNamed = null;
            foreach (var p in node.Inputs)
            {
                if (!string.Equals(p.Name, pinRef, StringComparison.Ordinal)) continue;
                firstNamed ??= p;
                if (p.Source is null) return p;
            }
            if (firstNamed != null) return firstNamed;
            throw new ScriptException(
                $"No input pin named '{pinRef}' on node '{node.Title}'.");
        }

        private static void SetProperty(INode node, string propName, string valueStr)
        {
            var prop = node.GetType().GetProperty(
                propName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null || !prop.CanWrite)
                throw new ScriptException(
                    $"Property '{propName}' not writable on '{node.GetType().Name}'.");

            object value;
            try { value = ScriptValues.Parse(valueStr, prop.PropertyType); }
            catch (Exception ex)
            {
                throw new ScriptException(
                    $"Cannot assign '{valueStr}' to {prop.PropertyType.Name} " +
                    $"property '{propName}': {ex.Message}");
            }
            prop.SetValue(node, value);
        }
    }
}
