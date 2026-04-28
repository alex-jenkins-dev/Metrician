// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.ComponentModel;
using System.Reflection;
using Metrician.Graph.Contracts;

namespace Metrician.Script
{
    /// <summary>
    /// Snapshots live <see cref="INode"/>s into a <see cref="GraphScript"/> for
    /// save/load through <see cref="GraphScriptText.Format"/> and
    /// <see cref="GraphScriptApplier.Apply"/>. Properties at their default
    /// values are skipped to keep saved scripts readable.
    /// </summary>
    public static class GraphScriptIntrospector
    {
        public static GraphScript Introspect(IEnumerable<INode> nodes)
        {
            var script = new GraphScript();

            var idByNode = new Dictionary<INode, string>(ReferenceEqualityComparer.Instance);
            var counters = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var node in nodes)
            {
                string baseName = MakeBaseName(node.GetType().Name);
                counters.TryGetValue(baseName, out int n);
                counters[baseName] = n + 1;
                string id = $"{baseName}{n + 1}";
                idByNode[node] = id;
                script.Nodes.Add(new NodeDecl(id, node.GetType().Name));
            }

            foreach (var node in nodes)
            {
                string id = idByNode[node];
                var defaults = TryCreateDefaultProbe(node.GetType());
                foreach (var prop in EmittableProperties(node.GetType()))
                {
                    object? current = prop.GetValue(node);
                    if (current is null) continue;

                    object? def = defaults != null
                        ? SafeGet(prop, defaults)
                        : DefaultOf(prop.PropertyType);
                    if (Equals(current, def)) continue;

                    script.Properties.Add(new PropertyAssignment(
                        id, prop.Name, ScriptValues.Format(current)));
                }
            }

            // Emit connections in target-pin-index order so reload's auto-compact
            // produces matching pin indices on variadic-input nodes.
            foreach (var target in nodes)
            {
                string targetId = idByNode[target];
                for (int i = 0; i < target.Inputs.Count; i++)
                {
                    var pin = target.Inputs[i];
                    var srcOut = pin.Source;
                    if (srcOut is null) continue;
                    if (!idByNode.TryGetValue(srcOut.Owner, out var sourceId))
                        continue;

                    int sourceIdx = IndexOf(srcOut.Owner.Outputs, srcOut);
                    if (sourceIdx < 0) continue;

                    script.Connections.Add(new Connection(
                        sourceId, sourceIdx.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        targetId, i.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
            }

            return script;
        }

        // "ClockFaceNode" -> "clockFace": strip trailing "Node" and lower-case the first letter.
        private static string MakeBaseName(string typeName)
        {
            const string suffix = "Node";
            if (typeName.EndsWith(suffix, StringComparison.Ordinal) &&
                typeName.Length > suffix.Length)
                typeName = typeName[..^suffix.Length];
            if (typeName.Length == 0) return "node";
            return char.ToLowerInvariant(typeName[0]) + typeName[1..];
        }

        private static IEnumerable<PropertyInfo> EmittableProperties(Type type)
        {
            foreach (var prop in type.GetProperties(
                BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (!ScriptValues.IsSupported(prop.PropertyType)) continue;

                // Position is Browsable(false) but needed for layout round-trip.
                if (prop.Name != nameof(INodeLayout.Position))
                {
                    var browsable = prop.GetCustomAttribute<BrowsableAttribute>();
                    if (browsable is { Browsable: false }) continue;
                }

                if (prop.GetIndexParameters().Length > 0) continue;
                yield return prop;
            }
        }

        // Best-effort default probe; null when the type lacks a parameterless ctor.
        private static object? TryCreateDefaultProbe(Type type)
        {
            try { return Activator.CreateInstance(type); }
            catch { return null; }
        }

        private static object? SafeGet(PropertyInfo prop, object instance)
        {
            try { return prop.GetValue(instance); } catch { return null; }
        }

        // Fallback default when no probe is available; gives default(T) for value types.
        private static object? DefaultOf(Type t)
        {
            if (!t.IsValueType) return null;
            var u = Nullable.GetUnderlyingType(t);
            if (u != null) return null;
            try { return Activator.CreateInstance(t); }
            catch { return null; }
        }

        private static int IndexOf<T>(IReadOnlyList<T> list, T item)
            where T : class
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], item)) return i;
            return -1;
        }
    }
}
