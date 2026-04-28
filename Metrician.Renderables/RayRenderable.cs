// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    public class RayRenderable : LineSegmentRenderable
    {
        public Vector3 Origin { get; set; } = Vector3.Zero;

        public Vector3 Direction { get; set; } = Vector3.UnitX;

        public float Length { get; set; } = 2f;

        protected override void GetEndpoints(out Vector3 negative, out Vector3 positive)
        {
            Vector3 dir = Vector3.Normalize(Direction);
            negative = Origin;
            positive = Origin + dir * Length;
        }

        public static RayRenderable Arrow(
            Vector3 origin, Vector3 direction, float length,
            Color color, string? label = null) => new()
            {
                Origin = origin,
                Direction = direction,
                Length = length,
                PositiveEnd = AxisEnd.Arrow,
                NegativeEnd = AxisEnd.None,
                ShaftStyle = new StrokeStyle { Colour = color, Width = 1.5f },
                EndColour = color,
                Label = label,
            };

        public static RayRenderable WithDotOrigin(
            Vector3 origin, Vector3 direction, float length,
            Color color, string? label = null) => new()
            {
                Origin = origin,
                Direction = direction,
                Length = length,
                PositiveEnd = AxisEnd.Arrow,
                NegativeEnd = AxisEnd.Dot,
                ShaftStyle = new StrokeStyle { Colour = color, Width = 1.5f },
                EndColour = color,
                Label = label,
            };
    }
}
