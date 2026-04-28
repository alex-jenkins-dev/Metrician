// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Renderable.Contracts;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    public sealed class LineSetRenderable : IRenderable
    {
        public record Segment(Vector3 A, Vector3 B);

        private readonly List<Segment> _segments;

        public Color LineColour { get; set; } = Color.White;
        public float LineWidth { get; set; } = 1f;
        public bool IsVisible { get; set; } = true;

        public LineSetRenderable(IEnumerable<Segment>? segments = null)
        {
            _segments = segments is null ? new() : new(segments);
            RecalcBounds();
        }

        public void Add(Vector3 a, Vector3 b)
        {
            _segments.Add(new Segment(a, b));
            RecalcBounds();
        }

        public void Clear()
        {
            _segments.Clear();
            RecalcBounds();
        }

        public BoundingBox3D? Bounds { get; private set; }

        private void RecalcBounds()
        {
            if (_segments.Count == 0) { Bounds = null; return; }
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            foreach (var s in _segments)
            {
                min = Vector3.Min(min, Vector3.Min(s.A, s.B));
                max = Vector3.Max(max, Vector3.Max(s.A, s.B));
            }
            Bounds = new BoundingBox3D(min, max);
        }

        public void Render(RenderContext ctx)
        {
            using var pen = new Pen(LineColour, LineWidth);
            foreach (var seg in _segments)
            {
                var pa = ctx.Project(seg.A);
                var pb = ctx.Project(seg.B);
                if (!ctx.IsVisible(pa) && !ctx.IsVisible(pb)) continue;
                ctx.Graphics.DrawLine(pen, pa, pb);
            }
        }
    }
}
