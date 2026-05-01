// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Library.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class CylinderRenderable : Library.Renderables.IRenderable
    {
        private readonly Library.Renderables.CylinderRenderable _cylinder = new();
        private readonly Library.Renderables.AxisRenderable _axis = new();

        public Vector3 Center { get; set; } = Vector3.Zero;
        public Vector3 Axis { get; set; } = Vector3.UnitZ;
        public float Radius { get; set; } = 1f;
        public float Height { get; set; } = 2f;
        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();
        public bool ShowAxis { get; set; }
        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                _cylinder.Center = Center;
                _cylinder.Axis = Axis;
                _cylinder.Radius = Radius;
                _cylinder.Height = Height;
                return _cylinder.Bounds;
            }
        }

        public void Render(RenderContext ctx)
        {
            _cylinder.Center = Center;
            _cylinder.Axis = Axis;
            _cylinder.Radius = Radius;
            _cylinder.Height = Height;
            _cylinder.Style = Style;
            _cylinder.Render(ctx);

            if (!ShowAxis) return;

            _axis.Origin = Center;
            _axis.Direction = Axis;
            _axis.Length = Height * 1.2f;
            _axis.PositiveEnd = Library.Renderables.AxisEnd.Arrow;
            _axis.NegativeEnd = Library.Renderables.AxisEnd.None;
            _axis.ShaftStyle = new StrokeStyle
            {
                Colour = Style.Colour,
                Width = 1f,
                Pattern = StrokePattern.Dashed,
            };
            _axis.EndColour = Style.Colour;
            _axis.Render(ctx);
        }
    }
}
