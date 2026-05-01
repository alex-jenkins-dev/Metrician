// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Contracts.Renderables;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    public sealed class PolylineRenderable : IRenderable
    {
        private Vector3[] _points;

        public Vector3[] Points
        {
            get => _points;
            set
            {
                _points = value ?? Array.Empty<Vector3>();
                RecalcBounds();
            }
        }

        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();

        /// <summary>If true, the final point joins back to the first.</summary>
        public bool Closed { get; set; } = false;

        /// <summary>If true, samples are interpolated with a cardinal spline.</summary>
        public bool Smooth { get; set; } = false;

        public bool IsVisible { get; set; } = true;

        public PolylineRenderable() : this(Array.Empty<Vector3>()) { }

        public PolylineRenderable(Vector3[] points)
        {
            _points = points ?? [];
            RecalcBounds();
        }

        public BoundingBox3D? Bounds { get; private set; }

        private void RecalcBounds()
        {
            if (_points.Length == 0) { Bounds = null; return; }
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            foreach (var p in _points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Bounds = new BoundingBox3D(min, max);
        }

        public void Render(RenderContext ctx)
        {
            if (Style is null || _points.Length < 2) return;

            var pts = new PointF[_points.Length];
            for (int i = 0; i < _points.Length; i++)
                pts[i] = ctx.Project(_points[i]);

            using var pen = Style.CreatePen();
            if (Smooth && _points.Length >= 3)
            {
                if (Closed)
                    ctx.Graphics.DrawClosedCurve(pen, pts);
                else
                    ctx.Graphics.DrawCurve(pen, pts);
            }
            else if (Closed)
            {
                ctx.Graphics.DrawPolygon(pen, pts);
            }
            else
            {
                ctx.Graphics.DrawLines(pen, pts);
            }
        }
    }
}
