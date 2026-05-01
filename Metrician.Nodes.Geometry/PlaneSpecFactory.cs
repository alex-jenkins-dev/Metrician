// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Renderable.Contracts;
using Metrician.Renderables;
using Metrician.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class PlaneSpecFactory : IRenderableFactory<PlaneSpec>
    {
        public IRenderable Create(PlaneSpec value) => new PlaneRenderable
        {
            Center = value.Center,
            Normal = value.Normal,
            Width = value.Width,
            Height = value.Height,
            Style = new StrokeStyle { Colour = value.Colour, Width = 1.5f },
        };
    }
}
