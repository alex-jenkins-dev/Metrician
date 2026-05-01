// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Library.Renderables
{
    /// <summary>
    /// A circular helix drawn as a polyline. Centred on <see cref="Center"/>, advances
    /// along <see cref="Axis"/> at <see cref="Pitch"/> world units per turn, starting
    /// from <see cref="StartDirection"/> and wrapping by the right-hand rule.
    /// </summary>
    public sealed class HelixRenderable : IRenderable
    {
        private readonly PolylineRenderable _polyline = new() { Smooth = true };

        public Vector3 Center { get; set; } = Vector3.Zero;

        public Vector3 Axis { get; set; } = Vector3.UnitZ;

        /// <summary>Radial direction at t = 0, projected onto the plane perpendicular to <see cref="Axis"/>.</summary>
        public Vector3 StartDirection { get; set; } = Vector3.UnitX;

        public float Radius { get; set; } = 1f;

        /// <summary>Axial advance per full turn.</summary>
        public float Pitch { get; set; } = 1f;

        /// <summary>Number of full turns; may be fractional.</summary>
        public float Turns { get; set; } = 3f;

        public int PointsPerTurn { get; set; } = 30;

        public StrokeStyle Style { get; set; } = StrokeStyle.SolidWhite();

        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                float h = MathF.Abs(Pitch * Turns);
                float ext = MathF.Sqrt(Radius * Radius + h * h * 0.25f);
                return new BoundingBox3D(Center - new Vector3(ext), Center + new Vector3(ext));
            }
        }

        public void Render(RenderContext ctx)
        {
            if (Style is null) return;

            float turns = Turns;
            if (MathF.Abs(turns) < 1e-6f) return;

            // (u, v) is an orthonormal basis perpendicular to a; v = a x u so positive
            // angle wraps right-hand-rule about a.
            Vector3 a = Axis;
            if (a.LengthSquared() < 1e-12f) a = Vector3.UnitZ;
            a = Vector3.Normalize(a);

            Vector3 u = StartDirection - Vector3.Dot(StartDirection, a) * a;
            if (u.LengthSquared() < 1e-12f)
            {
                Vector3 helper = MathF.Abs(Vector3.Dot(a, Vector3.UnitX)) < 0.9f
                    ? Vector3.UnitX : Vector3.UnitY;
                u = Vector3.Cross(a, helper);
            }
            u = Vector3.Normalize(u);
            Vector3 v = Vector3.Cross(a, u);

            int perTurn = Math.Max(2, PointsPerTurn);
            int n = Math.Max(2, (int)MathF.Round(MathF.Abs(turns) * perTurn) + 1);

            float totalAxial = Pitch * turns;
            var samples = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                float angle = t * turns * 2f * MathF.PI;
                float h = (t - 0.5f) * totalAxial;
                samples[i] = Center
                    + Radius * (MathF.Cos(angle) * u + MathF.Sin(angle) * v)
                    + h * a;
            }

            _polyline.Points = samples;
            _polyline.Style = Style;
            _polyline.Closed = false;
            _polyline.Render(ctx);
        }
    }
}
