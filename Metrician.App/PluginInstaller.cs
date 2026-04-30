// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core;
using Metrician.Graph;
using Metrician.Plugins;
using Metrician.Script;
using System.Diagnostics;

namespace Metrician.App
{
    /// <summary>One-shot plugin discovery.</summary>
    internal static class PluginInstaller // TODO: This is doing way too much.
    {
        /// <summary>
        /// Discovers plugins, registers factories, and populates the graph editor's Add menu.
        /// </summary>
        public static PluginLoadResult Install(SessionState session)
        {
            var pluginDir = Path.Combine(AppContext.BaseDirectory, "Plugins");
            var exclusions = PluginExclusions.FromFile(
                Path.Combine(pluginDir, "exclusions.txt"));
            var loader = new PluginLoader(exclusions);
            var plugins = loader.LoadFromDirectory(pluginDir);

            foreach (var factory in plugins.Factories)
                factory.RegisterWith(session.Registry);

            foreach (var converter in plugins.Converters)
                converter.RegisterWith(session.Converters);

            foreach (var node in plugins.Nodes)
                session.GraphControl.AvailableNodes.Add(
                    new NodeMenuEntry(node.Label, node.Factory)
                    {
                        Vendor = node.Vendor,
                        Source = node.NodeType.Assembly.GetName().Name ?? "",
                    });

            // RenderNode needs the registry, so it has no parameterless ctor;
            // pin it as a built-in rather than relying on auto-discovery.
            session.GraphControl.AvailableNodes.Add(
                new NodeMenuEntry("Render", () => new RenderNode(session.Registry))
                {
                    Pinned = true,
                });

            foreach (var error in plugins.Errors)
                Debug.WriteLine($"[plugin-load] {error}");

            return plugins;
        }

        public static IReadOnlyList<ScriptNodeFactory> ScriptFactories(
            SessionState session, PluginLoadResult plugins)
        {
            var list = new List<ScriptNodeFactory>(plugins.Nodes.Count + 1);
            foreach (var n in plugins.Nodes)
            {
                list.Add(new ScriptNodeFactory(
                    n.NodeType.Name,
                    n.NodeType.FullName ?? n.NodeType.Name,
                    n.Factory));
            }
            list.Add(new ScriptNodeFactory(
                nameof(RenderNode),
                typeof(RenderNode).FullName!,
                () => new RenderNode(session.Registry)));
            return list;
        }
    }
}
