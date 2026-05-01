// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Contracts.Renderables;
using Metrician.Renderables;
using Metrician.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class CircleSpecFactory : IRenderableFactory<CircleSpec>
    {
        public IRenderable Create(CircleSpec value) => new CircleRenderable
        {
            Center = value.Center,
            Normal = value.Normal,
            Radius = value.Radius,
            Style = new StrokeStyle { Colour = value.Colour, Width = 1.5f },
        };
    }
}
