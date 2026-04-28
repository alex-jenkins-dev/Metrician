// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    /// <summary>
    /// A line segment of total <see cref="Length"/> centred on <see cref="Origin"/>
    /// along <see cref="Direction"/>, with independently styled ends.
    /// </summary>
    public sealed class AxisRenderable : LineSegmentRenderable
    {
        public Vector3 Origin { get; set; } = Vector3.Zero;

        public Vector3 Direction { get; set; } = Vector3.UnitX;

        /// <summary>Total length; ends sit at Origin +- Length/2 along Direction.</summary>
        public float Length { get; set; } = 2f;

        protected override void GetEndpoints(out Vector3 negative, out Vector3 positive)
        {
            Vector3 dir = Vector3.Normalize(Direction);
            positive = Origin + dir * (Length * 0.5f);
            negative = Origin - dir * (Length * 0.5f);
        }

        /// <summary>A bounded axis with arrowheads at both ends.</summary>
        public static AxisRenderable BoundedAxis(
            Vector3 origin, Vector3 direction, float length,
            Color color, string? label = null) => new()
            {
                Origin = origin,
                Direction = direction,
                Length = length,
                PositiveEnd = AxisEnd.Arrow,
                NegativeEnd = AxisEnd.Arrow,
                ShaftStyle = new StrokeStyle { Colour = color, Width = 1.5f },
                EndColour = color,
                Label = label,
            };

        /// <summary>An "infinite" line: dashed shaft with small dots at each end.</summary>
        public static AxisRenderable InfiniteLine(
            Vector3 origin, Vector3 direction, float displayLength,
            Color color, string? label = null) => new()
            {
                Origin = origin,
                Direction = direction,
                Length = displayLength,
                PositiveEnd = AxisEnd.Dot,
                NegativeEnd = AxisEnd.Dot,
                ShaftStyle = new StrokeStyle
                {
                    Colour = color,
                    Width = 1f,
                    Pattern = StrokePattern.Dashed,
                },
                EndColour = Color.FromArgb(160, color),
                DotRadius = 2.5f,
                Label = label,
            };

        /// <summary>Standard XYZ triad at the origin (X = red, Y = green, Z = blue).</summary>
        public static (AxisRenderable X, AxisRenderable Y, AxisRenderable Z)
            WorldAxes(float length = 2f) => (
                BoundedAxis(Vector3.Zero, Vector3.UnitX, length, Color.FromArgb(220, 60, 60), "X"),
                BoundedAxis(Vector3.Zero, Vector3.UnitY, length, Color.FromArgb(60, 200, 60), "Y"),
                BoundedAxis(Vector3.Zero, Vector3.UnitZ, length, Color.FromArgb(60, 100, 220), "Z")
            );
    }
}
