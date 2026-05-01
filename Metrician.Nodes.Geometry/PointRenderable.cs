// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Globalization;
using System.Numerics;
using Metrician.Library.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class PointRenderable : Library.Renderables.IRenderable
    {
        private readonly Library.Renderables.LabelledPointRenderable _point =
            new(Vector3.Zero, string.Empty);

        public Vector3 Position { get; set; } = Vector3.Zero;
        public Color Colour { get; set; } = Color.Yellow;
        public float DotRadius { get; set; } = 4f;
        public bool ShowLabel { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds => _point.Bounds;

        public void Render(RenderContext ctx)
        {
            _point.Position = Position;
            _point.Colour = Colour;
            _point.DotRadius = DotRadius;
            _point.Label = ShowLabel ? FormatLabel(Position) : string.Empty;
            _point.Render(ctx);
        }

        private static string FormatLabel(Vector3 p)
        {
            var ci = CultureInfo.InvariantCulture;
            return $"({p.X.ToString("0.###", ci)}, {p.Y.ToString("0.###", ci)}, {p.Z.ToString("0.###", ci)})";
        }
    }
}
