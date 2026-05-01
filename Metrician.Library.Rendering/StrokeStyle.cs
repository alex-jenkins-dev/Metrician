// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing.Drawing2D;

namespace Metrician.Library.Rendering
{
    public enum StrokePattern
    {
        Solid,
        Dashed,
        Dotted,
        DashDot,
    }

    /// <summary>
    /// Reusable line styling that builds a fresh <see cref="Pen"/> on demand.
    /// </summary>
    public sealed class StrokeStyle
    {
        public Color Colour { get; set; } = Color.White;
        public float Width { get; set; } = 1.5f;
        public StrokePattern Pattern { get; set; } = StrokePattern.Solid;

        public static StrokeStyle SolidWhite(float width = 1.5f) =>
            new() { Colour = Color.White, Width = width, Pattern = StrokePattern.Solid };

        public static StrokeStyle DashedGrey(float width = 0.8f) =>
            new() { Colour = Color.FromArgb(140, 180, 180, 180), Width = width, Pattern = StrokePattern.Dashed };

        public static StrokeStyle DottedGrey(float width = 0.8f) =>
            new() { Colour = Color.FromArgb(100, 180, 180, 180), Width = width, Pattern = StrokePattern.Dotted };

        /// <summary>
        /// Allocates a new <see cref="Pen"/>.
        /// Caller disposes.
        /// </summary>
        public Pen CreatePen()
        {
            var pen = new Pen(Colour, Width) { LineJoin = LineJoin.Round };
            switch (Pattern)
            {
                case StrokePattern.Dashed:
                    pen.DashStyle = DashStyle.Custom;
                    pen.DashPattern = new[] { 5f, 3f };
                    pen.DashCap = DashCap.Round;
                    break;
                case StrokePattern.Dotted:
                    pen.DashStyle = DashStyle.Custom;
                    pen.DashPattern = new[] { 1f, 3f };
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.DashCap = DashCap.Round;
                    break;
                case StrokePattern.DashDot:
                    pen.DashStyle = DashStyle.Custom;
                    pen.DashPattern = new[] { 5f, 2.5f, 1f, 2.5f };
                    pen.DashCap = DashCap.Round;
                    break;
            }
            return pen;
        }
    }
}
