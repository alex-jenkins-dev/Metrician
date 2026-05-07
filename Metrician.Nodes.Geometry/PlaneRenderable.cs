// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Library.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class PlaneRenderable : Library.Renderables.IRenderable
    {
        private readonly Library.Renderables.PlaneRenderable _plane = new();
        private Library.Renderables.SurfaceNormalRenderable? _normal;

        public Vector3 Center { get; set; } = Vector3.Zero;
        public Vector3 Normal { get; set; } = Vector3.UnitZ;
        public float Width { get; set; } = 2f;
        public float Height { get; set; } = 2f;
        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();
        public bool ShowNormal { get; set; }
        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                _plane.Center = Center;
                _plane.Normal = Normal;
                _plane.Width = Width;
                _plane.Height = Height;
                return _plane.Bounds;
            }
        }

        public void Render(RenderContext ctx)
        {
            _plane.Center = Center;
            _plane.Normal = Normal;
            _plane.Width = Width;
            _plane.Height = Height;
            _plane.Style = Style;
            _plane.Render(ctx);

            if (!ShowNormal) return;

            float length = MathF.Min(Width, Height) * 0.5f;
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
