// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Renderable.Contracts;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    /// <summary>
    /// Torus drawn as its view-dependent silhouette: two closed curves traced
    /// where the surface normal is perpendicular to the view direction.
    /// Surface point p(θ, φ) = C + (R + r cos φ)(cos θ u + sin θ v) + r sin φ a;
    /// silhouette satisfies N . viewDir = 0, giving φ = atan2(c, R(θ)) ± π/2 with
    /// R(θ) = a.viewDir cos θ + b.viewDir sin θ and c = a.viewDir.
    ///
    /// Parametric torus: https://en.wikipedia.org/wiki/Torus#Geometry
    /// Silhouette concept: https://en.wikipedia.org/wiki/Silhouette_edge
    /// </summary>
    public sealed class TorusRenderable : IRenderable
    {
        private readonly PolylineRenderable _silhouetteA = new() { Closed = true, Smooth = true };
        private readonly PolylineRenderable _silhouetteB = new() { Closed = true, Smooth = true };

        public Vector3 Center { get; set; } = Vector3.Zero;

        public Vector3 Axis { get; set; } = Vector3.UnitZ;

        /// <summary>Distance from <see cref="Center"/> to the tube centreline.</summary>
        public float MajorRadius { get; set; } = 1f;

        /// <summary>Tube radius.</summary>
        public float MinorRadius { get; set; } = 0.25f;

        public int Segments { get; set; } = 120;

        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();

        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                float ext = MajorRadius + MinorRadius;
                return new BoundingBox3D(Center - new Vector3(ext), Center + new Vector3(ext));
            }
        }

        public void Render(RenderContext ctx)
        {
            if (Style is null) return;

            Vector3 axis = Axis;
            if (axis.LengthSquared() < 1e-12f) axis = Vector3.UnitZ;
            axis = Vector3.Normalize(axis);

            Vector3 helper = MathF.Abs(Vector3.Dot(axis, Vector3.UnitX)) < 0.9f
                ? Vector3.UnitX : Vector3.UnitY;
            Vector3 u = Vector3.Normalize(Vector3.Cross(axis, helper));
            Vector3 v = Vector3.Cross(axis, u);

            Vector3 viewDir = Vector3.Normalize(ctx.Camera.Target - ctx.Camera.Eye);
            float a = Vector3.Dot(viewDir, u);
            float b = Vector3.Dot(viewDir, v);
            float c = Vector3.Dot(viewDir, axis);

            // Side-on views (c = 0) make atan2(0, x) jump by π at x = 0, collapsing
            // the rounded caps. Perturb to keep the formula continuous.
            const float cFloor = 1e-2f;
            if (MathF.Abs(c) < cFloor)
                c = c < 0f ? -cFloor : cFloor;

            // Bump segment count so each silhouette cap reads as a smooth arc.
            const int kCap = 24;
            const int hardCap = 4000;
            float planar = MathF.Sqrt(a * a + b * b);
            int segNeeded = planar > 0f
                ? (int)MathF.Ceiling(MathF.PI * kCap * planar / MathF.Abs(c))
                : Segments;
            int seg = Math.Clamp(Math.Max(Segments, segNeeded), 12, hardCap);

            var aPts = new Vector3[seg];
            var bPts = new Vector3[seg];

            float R = MajorRadius;
            float r = MinorRadius;

            for (int i = 0; i < seg; i++)
            {
                float theta = i * 2f * MathF.PI / seg;
                float cosT = MathF.Cos(theta);
                float sinT = MathF.Sin(theta);
                float Rtheta = a * cosT + b * sinT;

                float delta = MathF.Atan2(c, Rtheta);
                float phiA = delta + MathF.PI / 2f;
                float phiB = delta - MathF.PI / 2f;

                Vector3 inPlane = cosT * u + sinT * v;
                aPts[i] = Center + (R + r * MathF.Cos(phiA)) * inPlane + (r * MathF.Sin(phiA)) * axis;
                bPts[i] = Center + (R + r * MathF.Cos(phiB)) * inPlane + (r * MathF.Sin(phiB)) * axis;
            }

            _silhouetteA.Points = aPts;
            _silhouetteA.Style = Style;
            _silhouetteA.Render(ctx);

            _silhouetteB.Points = bPts;
            _silhouetteB.Style = Style;
            _silhouetteB.Render(ctx);
        }
    }
}
