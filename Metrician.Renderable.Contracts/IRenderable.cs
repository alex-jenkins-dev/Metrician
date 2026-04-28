// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Rendering;

namespace Metrician.Renderable.Contracts
{
    public interface IRenderable
    {
        void Render(RenderContext context);

        /// <summary>
        /// AABB used for view-volume culling.
        /// Return null to always render.
        /// </summary>
        BoundingBox3D? Bounds { get; }

        bool IsVisible { get; }
    }
}
