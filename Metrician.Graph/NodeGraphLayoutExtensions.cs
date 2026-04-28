// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core;
using Metrician.Graph.Contracts;

namespace Metrician.Graph
{
    public static class NodeGraphLayoutExtensions
    {
        /// <summary>
        /// https://en.wikipedia.org/wiki/Layered_graph_drawing
        /// https://blog.disy.net/sugiyama-method/
        /// </summary>
        public static void SugiyamaLayout(this NodeGraph graph, NodeGraphTheme theme)
        {
            if (graph.Nodes.Count == 0) return;

            var layer = new Dictionary<INode, int>();
            var visiting = new HashSet<INode>();

            int LayerOf(INode node)
            {
                if (layer.TryGetValue(node, out var existing)) return existing;
                if (!visiting.Add(node)) return 0;
                int max = 0;
                foreach (var pin in node.Inputs)
                {
                    if (pin.Source?.Owner is INode upstream && IndexOf(graph, upstream) >= 0)
                    {
                        int up = LayerOf(upstream);
                        if (up + 1 > max) max = up + 1;
                    }
                }
                visiting.Remove(node);
                layer[node] = max;
                return max;
            }

            foreach (var node in graph.Nodes) LayerOf(node);

            var layers = new SortedDictionary<int, List<INode>>();
            foreach (var node in graph.Nodes)
            {
                var L = layer[node];
                if (!layers.TryGetValue(L, out var bucket))
                    layers[L] = bucket = new List<INode>();
                bucket.Add(node);
            }

            var positionedY = new Dictionary<INode, float>();

            foreach (var (L, nodesInLayer) in layers)
            {
                if (L > 0)
                {
                    nodesInLayer.Sort((a, b) =>
                    {
                        float aY = MeanInputY(a, positionedY);
                        float bY = MeanInputY(b, positionedY);
                        return aY.CompareTo(bY);
                    });
                }

                float x = theme.LayoutOriginX + L * theme.LayerSpacingX;
                float y = theme.LayoutOriginY;
                foreach (var node in nodesInLayer)
                {
                    if (node is INodeLayout layout)
                    {
                        layout.Position = new PointF(x, y);
                        positionedY[node] = y;
                        var rect = NodeGeometry.GetNodeRect(node, theme);
                        y += rect.Height + theme.LayerSpacingY;
                    }
                }
            }
        }

        private static float MeanInputY(INode node, Dictionary<INode, float> positionedY)
        {
            int count = 0;
            float sum = 0f;
            foreach (var pin in node.Inputs)
            {
                if (pin.Source?.Owner is INode upstream && positionedY.TryGetValue(upstream, out var y))
                {
                    sum += y;
                    count++;
                }
            }
            return count == 0 ? 0f : sum / count;
        }

        private static int IndexOf(NodeGraph graph, INode node)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
                if (ReferenceEquals(graph.Nodes[i], node)) return i;
            return -1;
        }
    }
}
