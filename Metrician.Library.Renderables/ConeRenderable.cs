// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Library.Renderables
{
    /// <summary>
    /// Wireframe cone or truncated cone. Cap A sits at Center + Axis*Height/2 with
    /// radius RadiusA; cap B at Center - Axis*Height/2 with RadiusB. Set either
    /// radius to ~0 for a cone apex at that end.
    /// Silhouette: https://en.wikipedia.org/wiki/Silhouette_edge
    /// </summary>
    public sealed class ConeRenderable : IRenderable
    {
        public Vector3 Center { get; set; } = Vector3.Zero;

        public Vector3 Axis { get; set; } = Vector3.UnitZ;

        public float RadiusA { get; set; } = 1f;
        public float RadiusB { get; set; } = 0.4f;
        public float Height { get; set; } = 2f;

        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();

        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                float maxR = MathF.Max(RadiusA, RadiusB);
                float ext = MathF.Sqrt(maxR * maxR + Height * Height * 0.25f);
                return new BoundingBox3D(Center - new Vector3(ext), Center + new Vector3(ext));
            }
        }

        private readonly CircleRenderable _capA = new();
        private readonly CircleRenderable _capB = new();

        public void Render(RenderContext ctx)
        {
            if (Style is null) return;

            Vector3 axis = Vector3.Normalize(Axis);
            Vector3 capACenter = Center + axis * (Height * 0.5f);
            Vector3 capBCenter = Center - axis * (Height * 0.5f);

            if (RadiusA > 1e-5f)
            {
                _capA.Center = capACenter;
                _capA.Normal = axis;
                _capA.Radius = RadiusA;
                _capA.Style = Style;
                _capA.Render(ctx);
            }
            if (RadiusB > 1e-5f)
            {
                _capB.Center = capBCenter;
                _capB.Normal = axis;
                _capB.Radius = RadiusB;
                _capB.Style = Style;
                _capB.Render(ctx);
            }

            // The silhouette under parallel projection runs perpendicular to the
            // in-plane component of the view direction. Bail when the camera
            // looks straight down the axis (inPlane is zero).
            Vector3 viewDir = Vector3.Normalize(ctx.Camera.Target - ctx.Camera.Eye);
            Vector3 antiView = -viewDir;
            Vector3 inPlane = antiView - Vector3.Dot(antiView, axis) * axis;

            float inPlaneLen = inPlane.Length();
            if (inPlaneLen <= 1e-5f) return;

            Vector3 tangential = Vector3.Cross(axis, inPlane / inPlaneLen);

            Vector3 wA1 = capACenter + RadiusA * tangential;
            Vector3 wA2 = capACenter - RadiusA * tangential;
            Vector3 wB1 = capBCenter + RadiusB * tangential;
            Vector3 wB2 = capBCenter - RadiusB * tangential;

            PointF pA1 = ctx.Project(wA1);
            PointF pA2 = ctx.Project(wA2);
            PointF pB1 = ctx.Project(wB1);
            PointF pB2 = ctx.Project(wB2);

            using var pen = Style.CreatePen();
            ctx.Graphics.DrawLine(pen, pA1, pB1);
            ctx.Graphics.DrawLine(pen, pA2, pB2);
        }
    }
}
