// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Renderable.Contracts;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    public sealed class SphereRenderable : IRenderable
    {
        public Vector3 Center { get; set; } = Vector3.Zero;
        public float Radius { get; set; } = 1f;
        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();

        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                var r = new Vector3(Radius);
                return new BoundingBox3D(Center - r, Center + r);
            }
        }

        private readonly CircleRenderable _silhouette = new();

        public void Render(RenderContext ctx)
        {
            if (Style is null) return;

            Vector3 vd = ctx.Camera.Target - ctx.Camera.Eye;
            if (vd.LengthSquared() < 1e-10f) return;
            Vector3 viewDir = Vector3.Normalize(vd);

            _silhouette.Center = Center;
            _silhouette.Normal = -viewDir;
            _silhouette.Radius = Radius;
            _silhouette.Style = Style;
            _silhouette.Render(ctx);
        }
    }
}
