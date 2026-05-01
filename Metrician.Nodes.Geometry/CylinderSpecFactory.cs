// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class CylinderSpecFactory : IRenderableFactory<CylinderSpec>
    {
        public IRenderable Create(CylinderSpec value) => new CylinderRenderable
        {
            Center = value.Center,
            Axis = value.Axis,
            Radius = value.Radius,
            Height = value.Height,
            Style = new StrokeStyle { Colour = value.Colour, Width = 1.5f },
        };
    }
}
