// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Model.Graph
{
    public sealed class LayoutMetrics
    {
        public int NodeWidth { get; init; } = 180;
        public int HeaderHeight { get; init; } = 26;
        public int RowHeight { get; init; } = 22;
        public int FooterHeight { get; init; } = 28;
        public int PinRadius { get; init; } = 5;
        public int HitRadius { get; init; } = 10;
        public int CornerRadius { get; init; } = 6;
        public int IndicatorInset { get; init; } = 12;

        public static LayoutMetrics Default { get; } = new();
    }
}
