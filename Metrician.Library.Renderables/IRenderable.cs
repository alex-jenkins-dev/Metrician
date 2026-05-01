// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Library.Rendering;

namespace Metrician.Library.Renderables
{
    public interface IRenderable
    {
        void Render(RenderContext context);

        BoundingBox3D? Bounds { get; }

        bool IsVisible { get; }
    }
}
