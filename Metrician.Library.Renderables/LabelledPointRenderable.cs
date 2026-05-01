// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Library.Renderables
{
    public sealed class LabelledPointRenderable : IRenderable
    {
        public Vector3 Position { get; set; }
        public string Label { get; set; }
        public Color Colour { get; set; } = Color.Yellow;
        public float DotRadius { get; set; } = 4f;
        public bool IsVisible { get; set; } = true;

        private static readonly Font _font = new("Segoe UI", 8f);

        public BoundingBox3D? Bounds => null;

        public LabelledPointRenderable(Vector3 position, string label)
        {
            Position = position;
            Label = label;
        }

        public void Render(RenderContext ctx)
        {
            var screen = ctx.Project(Position);
            if (!ctx.IsVisible(screen)) return;

            float r = DotRadius;
            using var brush = new SolidBrush(Colour);
            ctx.Graphics.FillEllipse(brush, screen.X - r, screen.Y - r, r * 2, r * 2);
            ctx.Graphics.DrawString(Label, _font, brush, screen.X + r + 2, screen.Y - 6);
        }
    }
}
