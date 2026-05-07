// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class CylinderSpecFactory : IRenderableFactory<CylinderSpec>
    {
        public Type? OptionsType => typeof(CylinderRenderOptions);

        public IRenderable Create(CylinderSpec value) => Create(value, null);

        public IRenderable Create(CylinderSpec value, object? options)
        {
            var opts = options as CylinderRenderOptions ?? new CylinderRenderOptions();
            return new CylinderRenderable
            {
                Center = value.Center,
                Axis = value.Axis,
                Radius = value.Radius,
                Height = value.Height,
                Style = new StrokeStyle { Colour = opts.Colour, Width = opts.LineWidth },
                ShowAxis = opts.ShowAxis,
            };
        }
    }
}
