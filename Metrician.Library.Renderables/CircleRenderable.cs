// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Library.Renderables
{
    public sealed class CircleRenderable : IRenderable
    {
        private readonly ArcRenderable _arc = new() { IncludedAngleDegrees = 360f };

        public Vector3 Center
        {
            get => _arc.Center;
            set => _arc.Center = value;
        }

        public Vector3 Normal
        {
            get => _arc.Normal;
            set => _arc.Normal = value;
        }

        public float Radius
        {
            get => _arc.Radius;
            set => _arc.Radius = value;
        }

        public StrokeStyle Style
        {
            get => _arc.Style;
            set => _arc.Style = value;
        }

        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds => _arc.Bounds;

        public void Render(RenderContext ctx) => _arc.Render(ctx);
    }
}
