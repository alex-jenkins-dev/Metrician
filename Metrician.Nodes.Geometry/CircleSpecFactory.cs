// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class CircleSpecFactory : IRenderableFactory<CircleSpec>
    {
        public Type? OptionsType => typeof(CircleRenderOptions);

        public IRenderable Create(CircleSpec value) => Create(value, null);

        public IRenderable Create(CircleSpec value, object? options)
        {
            var opts = options as CircleRenderOptions ?? new CircleRenderOptions();
            return new CircleRenderable
            {
                Center = value.Center,
                Normal = value.Normal,
                Radius = value.Radius,
                Style = new StrokeStyle { Colour = opts.Colour, Width = opts.LineWidth },
                ShowNormal = opts.ShowNormal,
            };
        }
    }
}
