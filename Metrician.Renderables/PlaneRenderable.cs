// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Contracts.Renderables;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    /// <summary>Wireframe rectangular patch of a plane.</summary>
    public sealed class PlaneRenderable : IRenderable
    {
        public Vector3 Center { get; set; } = Vector3.Zero;

        public Vector3 Normal { get; set; } = Vector3.UnitZ;

        public float Width { get; set; } = 2f;
        public float Height { get; set; } = 2f;

        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();

        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                float ext = MathF.Sqrt(Width * Width + Height * Height) * 0.5f;
                return new BoundingBox3D(Center - new Vector3(ext), Center + new Vector3(ext));
            }
        }

        public void Render(RenderContext ctx)
        {
            if (Style is null) return;

            // Pick a helper axis not nearly parallel to Normal so the cross is well-conditioned.
            Vector3 n = Vector3.Normalize(Normal);
            Vector3 helper = MathF.Abs(Vector3.Dot(n, Vector3.UnitX)) < 0.9f
                ? Vector3.UnitX : Vector3.UnitY;
            Vector3 u = Vector3.Normalize(Vector3.Cross(n, helper));
            Vector3 v = Vector3.Cross(n, u);

            float halfW = Width * 0.5f;
            float halfH = Height * 0.5f;

            Vector3 c00 = Center - halfW * u - halfH * v;
            Vector3 c10 = Center + halfW * u - halfH * v;
            Vector3 c11 = Center + halfW * u + halfH * v;
            Vector3 c01 = Center - halfW * u + halfH * v;

            PointF p00 = ctx.Project(c00);
            PointF p10 = ctx.Project(c10);
            PointF p11 = ctx.Project(c11);
            PointF p01 = ctx.Project(c01);

            using var pen = Style.CreatePen();
            ctx.Graphics.DrawLine(pen, p00, p10);
            ctx.Graphics.DrawLine(pen, p10, p11);
            ctx.Graphics.DrawLine(pen, p11, p01);
            ctx.Graphics.DrawLine(pen, p01, p00);
        }
    }
}
