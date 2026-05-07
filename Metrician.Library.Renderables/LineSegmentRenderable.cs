// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing.Drawing2D;
using System.Numerics;
using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Library.Renderables
{
    public enum AxisEnd
    {
        None,
        Dot,
        Arrow,
        OpenArrow,
    }

    /// <summary>
    /// Base for renderables drawing a single line segment with optional end
    /// decorations and a label.
    /// </summary>
    public abstract class LineSegmentRenderable : IRenderable
    {
        public AxisEnd PositiveEnd { get; set; } = AxisEnd.Arrow;
        public AxisEnd NegativeEnd { get; set; } = AxisEnd.None;

        public StrokeStyle ShaftStyle { get; set; } = StrokeStyle.SolidWhite();

        /// <summary>Colour of arrowheads and dots. Defaults to the shaft colour.</summary>
        public Color? EndColour { get; set; } = null;

        public float DotRadius { get; set; } = 4f;
        public float ArrowHalfWidth { get; set; } = 6f;
        public float ArrowLength { get; set; } = 14f;

        public string? Label { get; set; } = null;
        public Font LabelFont { get; set; } = new Font("Segoe UI", 9f, FontStyle.Regular);
        public Color? LabelColour { get; set; } = null;
        public float LabelOffset { get; set; } = 8f;

        public bool IsVisible { get; set; } = true;

        public BoundingBox3D? Bounds
        {
            get
            {
                GetEndpoints(out Vector3 neg, out Vector3 pos);
                Vector3 min = Vector3.Min(pos, neg) - new Vector3(0.01f);
                Vector3 max = Vector3.Max(pos, neg) + new Vector3(0.01f);
                return new BoundingBox3D(min, max);
            }
        }

        /// <param name="negative">From end; receives <see cref="NegativeEnd"/>.</param>
        /// <param name="positive">To end; receives <see cref="PositiveEnd"/> and is the label anchor.</param>
        protected abstract void GetEndpoints(out Vector3 negative, out Vector3 positive);

        public void Render(RenderContext ctx)
        {
            GetEndpoints(out Vector3 tipNeg, out Vector3 tipPos);

            PointF screenPos = ctx.Project(tipPos);
            PointF screenNeg = ctx.Project(tipNeg);

            float dxShaft = screenPos.X - screenNeg.X;
            float dyShaft = screenPos.Y - screenNeg.Y;
            float shaftLen = MathF.Sqrt(dxShaft * dxShaft + dyShaft * dyShaft);

            Color decorColour = EndColour ?? ShaftStyle.Colour;

            if (shaftLen < 1f)
            {
                DrawDot(ctx.Graphics, screenPos, DotRadius, decorColour);
                return;
            }

            float ux = dxShaft / shaftLen;
            float uy = dyShaft / shaftLen;
            float px = -uy;
            float py = ux;

            float setbackPos = SetbackFor(PositiveEnd);
            float setbackNeg = SetbackFor(NegativeEnd);

            PointF shaftStart = new PointF(
                screenNeg.X + ux * setbackNeg,
                screenNeg.Y + uy * setbackNeg);
            PointF shaftEnd = new PointF(
                screenPos.X - ux * setbackPos,
                screenPos.Y - uy * setbackPos);

            using (var pen = ShaftStyle.CreatePen())
                ctx.Graphics.DrawLine(pen, shaftStart, shaftEnd);

            DrawTermination(ctx.Graphics, screenPos,
                shaftDirX: ux, shaftDirY: uy,
                perpX: px, perpY: py,
                end: PositiveEnd, decorColour: decorColour);

            DrawTermination(ctx.Graphics, screenNeg,
                shaftDirX: -ux, shaftDirY: -uy,
                perpX: px, perpY: py,
                end: NegativeEnd, decorColour: decorColour);

            if (!string.IsNullOrEmpty(Label))
            {
                Color labelCol = LabelColour ?? decorColour;
                float lx = screenPos.X + px * LabelOffset;
                float ly = screenPos.Y + py * LabelOffset
                            - LabelFont.GetHeight(ctx.Graphics) * 0.5f;
                using var brush = new SolidBrush(labelCol);
                ctx.Graphics.DrawString(Label, LabelFont, brush, lx, ly);
            }
        }

        private float SetbackFor(AxisEnd end) => end switch
        {
            AxisEnd.Arrow => ArrowLength,
            AxisEnd.OpenArrow => ArrowLength,
            AxisEnd.Dot => DotRadius,
            _ => 0f,
        };

        private void DrawTermination(
            Graphics g, PointF tipPos,
            float shaftDirX, float shaftDirY,
            float perpX, float perpY,
            AxisEnd end, Color decorColour)
        {
            switch (end)
            {
                case AxisEnd.Dot:
                    DrawDot(g, tipPos, DotRadius, decorColour);
                    break;
                case AxisEnd.Arrow:
                    DrawArrowHead(g, tipPos, shaftDirX, shaftDirY, perpX, perpY, decorColour, filled: true);
                    break;
                case AxisEnd.OpenArrow:
                    DrawArrowHead(g, tipPos, shaftDirX, shaftDirY, perpX, perpY, decorColour, filled: false);
                    break;
            }
        }

        private static void DrawDot(Graphics g, PointF centre, float radius, Color color)
        {
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush,
                centre.X - radius, centre.Y - radius,
                radius * 2f, radius * 2f);
        }

        private void DrawArrowHead(
            Graphics g, PointF tip,
            float shaftDirX, float shaftDirY,
            float perpX, float perpY,
            Color color, bool filled)
        {
            float baseCx = tip.X - shaftDirX * ArrowLength;
            float baseCy = tip.Y - shaftDirY * ArrowLength;

            PointF wingL = new PointF(
                baseCx + perpX * ArrowHalfWidth,
                baseCy + perpY * ArrowHalfWidth);
            PointF wingR = new PointF(
                baseCx - perpX * ArrowHalfWidth,
                baseCy - perpY * ArrowHalfWidth);

            PointF[] triangle = { tip, wingL, wingR };

            if (filled)
            {
                using var brush = new SolidBrush(color);
                g.FillPolygon(brush, triangle);
            }
            else
            {
                using var pen = new Pen(color, ShaftStyle.Width)
                {
                    LineJoin = LineJoin.Round,
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                };
                g.DrawPolygon(pen, triangle);
            }
        }
    }
}
