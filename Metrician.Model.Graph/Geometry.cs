// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Model.Graph
{
    public readonly record struct Rect(
        float X, float Y, float Width, float Height)
    {
        public float Left => X;
        public float Top => Y;
        public float Right => X + Width;
        public float Bottom => Y + Height;

        public bool Contains(Vector2 p) =>
            p.X >= X && p.X < X + Width &&
            p.Y >= Y && p.Y < Y + Height;
    }

    public static class Geometry
    {
        public static Rect NodeRect(
            IGraphWorld world, NodeId id, LayoutMetrics m)
        {
            var inputs = world.Pins.Inputs(id).Count();
            var outputs = world.Pins.Outputs(id).Count();
            int rows = Math.Max(inputs, outputs);
            float height = m.HeaderHeight
                         + Math.Max(rows, 1) * m.RowHeight
                         + m.FooterHeight;
            var pos = world.Layout.Get(id) ?? Vector2.Zero;
            return new Rect(pos.X, pos.Y, m.NodeWidth, height);
        }

        public static Vector2 PinPosition(
            IGraphWorld world, PinId pin, LayoutMetrics m)
        {
            var rect = NodeRect(world, pin.Owner, m);
            var siblings = pin.Direction == PinDirection.Input
                ? world.Pins.Inputs(pin.Owner)
                : world.Pins.Outputs(pin.Owner);

            int idx = 0;
            foreach (var p in siblings)
            {
                if (p.Id == pin) break;
                idx++;
            }

            float y = rect.Top
                    + m.HeaderHeight
                    + idx * m.RowHeight
                    + m.RowHeight / 2f;
            float x = pin.Direction == PinDirection.Input ? rect.Left : rect.Right;
            return new Vector2(x, y);
        }

        public static NodeId? NodeAt(
            IGraphWorld world, Vector2 canvasPt, LayoutMetrics m)
        {
            NodeId? hit = null;
            foreach (var node in world.Nodes.All)
            {
                if (NodeRect(world, node.Id, m).Contains(canvasPt))
                    hit = node.Id;
            }
            return hit;
        }

        public static PinId? OutputPinAt(
            IGraphWorld world, Vector2 canvasPt, LayoutMetrics m)
        {
            float r = m.HitRadius;
            foreach (var node in world.Nodes.All)
                foreach (var pin in world.Pins.Outputs(node.Id))
                    if (Vector2.Distance(PinPosition(world, pin.Id, m), canvasPt) <= r)
                        return pin.Id;
            return null;
        }

        public static PinId? InputPinAt(
            IGraphWorld world, Vector2 canvasPt, LayoutMetrics m)
        {
            float r = m.HitRadius;
            foreach (var node in world.Nodes.All)
                foreach (var pin in world.Pins.Inputs(node.Id))
                    if (Vector2.Distance(PinPosition(world, pin.Id, m), canvasPt) <= r)
                        return pin.Id;
            return null;
        }
    }
}
