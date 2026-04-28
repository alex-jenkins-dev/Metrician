// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Rendering;

namespace Metrician.Renderables
{
    public sealed class SurfaceNormalRenderable : RayRenderable
    {
        public SurfaceNormalRenderable(
            Vector3 position, Vector3 normal, float length, Color color)
        {
            Origin = position;
            Direction = normal;
            Length = length;

            PositiveEnd = AxisEnd.Arrow;
            NegativeEnd = AxisEnd.None;
            ShaftStyle = new StrokeStyle
            {
                Colour = color,
                Width = 1f,
                Pattern = StrokePattern.Solid,
            };
            EndColour = color;
            ArrowLength = 7f;
            ArrowHalfWidth = 3f;
        }

        public Color Colour
        {
            get => ShaftStyle.Colour;
            set
            {
                ShaftStyle.Colour = value;
                EndColour = value;
            }
        }
    }
}
