// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing.Drawing2D;
using System.Numerics;
using Metrician.Renderable.Contracts;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    /// <summary>
    /// A 3-D circular arc, drawn as a true ellipse: an affine image of the unit
    /// circle is always an ellipse, so under orthographic projection a circle
    /// projects exactly to an ellipse on screen.
    /// https://en.wikipedia.org/wiki/Ellipse#Definition_as_an_affine_image_of_the_unit_circle
    /// </summary>
    public sealed class ArcRenderable : IRenderable
    {
        public Vector3 Center { get; set; } = Vector3.Zero;

        public Vector3 Normal { get; set; } = Vector3.UnitZ;

        /// <summary>Direction of the arc's start point from <see cref="Center"/>, projected onto the arc plane.</summary>
        public Vector3 StartDirection { get; set; } = Vector3.UnitX;

        public float Radius { get; set; } = 1f;

        /// <summary>Sweep in degrees, clamped to (0, 360].</summary>
        public float IncludedAngleDegrees { get; set; } = 360f;

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

        public void Render(RenderContext ctx)
        {
            if (Style is null) return;

            float angleDeg = MathF.Min(360f, IncludedAngleDegrees);
            if (angleDeg <= 0f) return;

            // (u, v) is an orthonormal basis in the arc plane: u along StartDirection,
            // v = n x u so positive sweep follows the right-hand rule about Normal.
            Vector3 n = Vector3.Normalize(Normal);
            Vector3 u = StartDirection - Vector3.Dot(StartDirection, n) * n;
            if (u.LengthSquared() < 1e-12f)
            {
                Vector3 helper = MathF.Abs(Vector3.Dot(n, Vector3.UnitX)) < 0.9f
                    ? Vector3.UnitX : Vector3.UnitY;
                u = Vector3.Cross(n, helper);
            }
            u = Vector3.Normalize(u);
            Vector3 v = Vector3.Cross(n, u);

            PointF sc = ctx.Project(Center);
            PointF su = ctx.Project(Center + Radius * u);
            PointF sv = ctx.Project(Center + Radius * v);
            float euX = su.X - sc.X, euY = su.Y - sc.Y;
            float evX = sv.X - sc.X, evY = sv.Y - sc.Y;

            // Maps the unit circle to the screen-space ellipse:
            // local (1, 0) -> sc + eu, local (0, 1) -> sc + ev.
            using var matrix = new Matrix(euX, euY, evX, evY, sc.X, sc.Y);
            using var path = new GraphicsPath();

            bool closed = angleDeg >= 360f - 1e-3f;
            if (closed)
                path.AddEllipse(-1f, -1f, 2f, 2f);
            else
                path.AddArc(-1f, -1f, 2f, 2f, 0f, angleDeg);

            path.Transform(matrix);

            using var pen = Style.CreatePen();
            ctx.Graphics.DrawPath(pen, path);
        }
    }
}
