// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Library.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class CircleRenderable : Library.Renderables.IRenderable
    {
        private readonly Library.Renderables.CircleRenderable _circle = new();
        private Library.Renderables.SurfaceNormalRenderable? _normal;

        public Vector3 Center { get; set; } = Vector3.Zero;
        public Vector3 Normal { get; set; } = Vector3.UnitZ;
        public float Radius { get; set; } = 1f;
        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();
        public bool ShowNormal { get; set; }
        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                _circle.Center = Center;
                _circle.Normal = Normal;
                _circle.Radius = Radius;
                return _circle.Bounds;
            }
        }

        public void Render(RenderContext ctx)
        {
            _circle.Center = Center;
            _circle.Normal = Normal;
            _circle.Radius = Radius;
            _circle.Style = Style;
            _circle.Render(ctx);

            if (!ShowNormal) return;

            float length = Radius;
            _normal ??= new Library.Renderables.SurfaceNormalRenderable(
                Center, Normal, length, Style.Colour);
            _normal.Origin = Center;
            _normal.Direction = Normal;
            _normal.Length = length;
            _normal.Colour = Style.Colour;
            _normal.Render(ctx);
        }
    }
}
