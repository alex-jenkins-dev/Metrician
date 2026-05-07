// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Model.Graph
{
    public sealed record ContextMenuItem(
        string Label = "",
        Action? OnClick = null,
        IReadOnlyList<ContextMenuItem>? Children = null,
        bool Enabled = true,
        bool IsSeparator = false)
    {
        public static ContextMenuItem Separator { get; } = new(IsSeparator: true);
    }
}
