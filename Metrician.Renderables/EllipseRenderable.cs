// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing.Drawing2D;
using System.Numerics;
using Metrician.Renderable.Contracts;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    public sealed class EllipseRenderable : IRenderable
    {
        public Vector3 Center { get; set; } = Vector3.Zero;

        public Vector3 Normal { get; set; } = Vector3.UnitZ;

        public Vector3 MajorAxisDirection { get; set; } = Vector3.UnitX;

        public float SemiMajorAxis { get; set; } = 1f;

        public float SemiMinorAxis { get; set; } = 0.5f;

        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();

        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                float ext = MathF.Max(MathF.Abs(SemiMajorAxis), MathF.Abs(SemiMinorAxis));
                var r = new Vector3(ext);
                return new BoundingBox3D(Center - r, Center + r);
            }
        }

        public void Render(RenderContext ctx)
        {
            if (Style is null) return;
            if (SemiMajorAxis <= 0f || SemiMinorAxis <= 0f) return;

            // (u, v) is an orthonormal basis in the ellipse plane: u along
            // MajorAxisDirection, v = n x u so the minor axis lies 90 degrees
            // counter-clockwise about Normal.
            Vector3 n = Vector3.Normalize(Normal);
            Vector3 u = MajorAxisDirection - Vector3.Dot(MajorAxisDirection, n) * n;
            if (u.LengthSquared() < 1e-12f)
            {
                Vector3 helper = MathF.Abs(Vector3.Dot(n, Vector3.UnitX)) < 0.9f
                    ? Vector3.UnitX : Vector3.UnitY;
                u = Vector3.Cross(n, helper);
            }
            u = Vector3.Normalize(u);
            Vector3 v = Vector3.Cross(n, u);

            PointF sc = ctx.Project(Center);
            PointF su = ctx.Project(Center + SemiMajorAxis * u);
            PointF sv = ctx.Project(Center + SemiMinorAxis * v);
            float euX = su.X - sc.X, euY = su.Y - sc.Y;
            float evX = sv.X - sc.X, evY = sv.Y - sc.Y;

            // Maps the unit circle to the screen-space ellipse:
            // local (1, 0) -> sc + eu (major-axis tip),
            // local (0, 1) -> sc + ev (minor-axis tip).
            using var matrix = new Matrix(euX, euY, evX, evY, sc.X, sc.Y);
            using var path = new GraphicsPath();
            path.AddEllipse(-1f, -1f, 2f, 2f);
            path.Transform(matrix);

            using var pen = Style.CreatePen();
            ctx.Graphics.DrawPath(pen, path);
        }
    }
}
