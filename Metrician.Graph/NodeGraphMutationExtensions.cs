// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core;
using Metrician.Contracts.Graph;

namespace Metrician.Graph
{
    /// <summary>
    /// Mutation helpers for <see cref="NodeGraph"/>. These methods do not raise
    /// events; the caller notifies observers and triggers repaint.
    /// </summary>
    public static class NodeGraphMutationExtensions
    {
        public static void AddNodeAt(
            this NodeGraph graph, INode node, PointF canvasAt)
        {
            if (node is INodeLayout layout)
                layout.Position = canvasAt;
            graph.Nodes.Add(node);
        }

        public static void DeleteNode(
            this NodeGraph graph, INode node, WireConversions? conversions = null)
        {
            var compactTargets = new HashSet<IVariadicInputs>();
            foreach (var other in graph.Nodes)
            {
                if (ReferenceEquals(other, node)) continue;
                foreach (var inPin in other.Inputs)
                {
                    if (inPin.Source != null && ReferenceEquals(inPin.Source.Owner, node))
                    {
                        ValueConverterRegistryExtensions.Disconnect(inPin, conversions);
                        if (other is IVariadicInputs v) compactTargets.Add(v);
                    }
                }
            }
            foreach (var v in compactTargets) v.CompactInputs();

            graph.Nodes.Remove(node);
        }

        public static void AddNodesWithLayout(
            this NodeGraph graph, IReadOnlyList<INode> nodes, NodeGraphTheme theme)
        {
            if (nodes is null || nodes.Count == 0) return;

            foreach (var node in nodes)
                graph.Nodes.Add(node);

            graph.SugiyamaLayout(theme);
        }
    }
}
