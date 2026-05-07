// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Nodes.Geometry
{
    public sealed class PlaneSpecFactory : IRenderableFactory<PlaneSpec>
    {
        public Type? OptionsType => typeof(PlaneRenderOptions);

        public IRenderable Create(PlaneSpec value) => Create(value, null);

        public IRenderable Create(PlaneSpec value, object? options)
        {
            var opts = options as PlaneRenderOptions ?? new PlaneRenderOptions();
            return new PlaneRenderable
            {
                Center = value.Center,
                Normal = value.Normal,
                Width = value.Width,
                Height = value.Height,
                Style = new StrokeStyle { Colour = opts.Colour, Width = opts.LineWidth },
                ShowNormal = opts.ShowNormal,
            };
        }
    }
}
