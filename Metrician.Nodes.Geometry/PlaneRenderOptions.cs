// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Nodes.Geometry
{
    public sealed class PlaneRenderOptions
    {
        public Color Colour { get; set; } = Color.Khaki;
        public float LineWidth { get; set; } = 1.5f;
        public bool ShowNormal { get; set; }
    }
}
