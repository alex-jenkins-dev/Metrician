// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Renderable.Contracts;
using Metrician.Renderables;
using Metrician.Rendering;

namespace Metrician.SampleNodes
{
    public sealed class SphereSpecFactory : IRenderableFactory<SphereSpec>
    {
        public IRenderable Create(SphereSpec value) => new SphereRenderable
        {
            Center = value.Center,
            Radius = value.Radius,
            Style = new StrokeStyle { Colour = value.Colour, Width = 1.5f },
        };
    }
}
