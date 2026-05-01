// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core;
using Metrician.Contracts.Graph;

namespace Metrician.Graph
{
    public static class NodeGraphHitTest
    {
        public static INode? FindNodeAt(
            NodeGraph graph, PointF canvasPt, NodeGraphTheme theme)
        {
            for (int i = graph.Nodes.Count - 1; i >= 0; i--)
            {
                var node = graph.Nodes[i];
                if (Contains(NodeGeometry.GetNodeRect(node, theme), canvasPt))
                    return node;
            }
            return null;
        }

        public static INodeOutput? FindOutputPinAt(
            NodeGraph graph, PointF canvasPt, NodeGraphTheme theme)
        {
            float r = theme.HitRadius;
            foreach (var node in graph.Nodes)
                foreach (var pin in node.Outputs)
                    if (Distance(NodeGeometry.GetPinCanvasPos(pin, theme), canvasPt) <= r)
                        return pin;
            return null;
        }

        public static INodeInput? FindInputPinAt(
            NodeGraph graph, PointF canvasPt, NodeGraphTheme theme)
        {
            float r = theme.HitRadius;
            foreach (var node in graph.Nodes)
                foreach (var pin in node.Inputs)
                    if (Distance(NodeGeometry.GetPinCanvasPos(pin, theme), canvasPt) <= r)
                        return pin;
            return null;
        }

        private static double Distance(PointF a, PointF b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool Contains(Rectangle r, PointF p) =>
            p.X >= r.X && p.X < r.X + r.Width &&
            p.Y >= r.Y && p.Y < r.Y + r.Height;
    }
}
