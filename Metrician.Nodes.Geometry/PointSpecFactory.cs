// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Library.Renderables;

namespace Metrician.Nodes.Geometry
{
    public sealed class PointSpecFactory : IRenderableFactory<PointSpec>
    {
        public Type? OptionsType => typeof(PointRenderOptions);

        public IRenderable Create(PointSpec value) => Create(value, null);

        public IRenderable Create(PointSpec value, object? options)
        {
            var opts = options as PointRenderOptions ?? new PointRenderOptions();
            return new PointRenderable
            {
                Position = value.Position,
                Colour = opts.Colour,
                DotRadius = opts.DotRadius,
                ShowLabel = opts.ShowLabel,
            };
        }
    }
}
