// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Nodes.Geometry
{
    public sealed class PointRenderOptions
    {
        public Color Colour { get; set; } = Color.Yellow;
        public float DotRadius { get; set; } = 4f;
        public bool ShowLabel { get; set; } = true;
    }
}
