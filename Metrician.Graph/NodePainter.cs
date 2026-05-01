// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing.Drawing2D;
using Metrician.Contracts.Graph;

namespace Metrician.Graph
{
    public sealed class NodePainter
    {
        private readonly NodeGraphTheme _theme;

        public NodePainter(NodeGraphTheme theme)
        {
            _theme = theme;
        }

        public void DrawNode(Graphics g, INode node, bool isSelected)
        {
            var rect = NodeGeometry.GetNodeRect(node, _theme);
            var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, _theme.HeaderHeight);

            using (var path = RoundedRect(rect, _theme.CornerRadius))
            using (var bg = new SolidBrush(_theme.NodeBackground))
                g.FillPath(bg, path);

            using (var headerPath = RoundedRect(headerRect, _theme.CornerRadius, topOnly: true))
            using (var hb = new SolidBrush(_theme.NodeHeader))
                g.FillPath(hb, headerPath);

            var borderColor = isSelected ? _theme.SelectedBorder : _theme.NodeBorder;
            using (var path = RoundedRect(rect, _theme.CornerRadius))
            using (var border = new Pen(borderColor, isSelected ? 2f : 1f))
                g.DrawPath(border, path);

            using (var titleBrush = new SolidBrush(_theme.Text))
            using (var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                g.DrawString(node.Title, titleFont, titleBrush, rect.X + 10, rect.Y + 5);

            // Small "live" dot in the header for IDynamicNode.
            if (node is IDynamicNode)
            {
                const int dotRadius = 4;
                int dotCx = rect.Right - 12;
                int dotCy = rect.Y + _theme.HeaderHeight / 2;
                using var dotBrush = new SolidBrush(_theme.PinConnected);
                g.FillEllipse(dotBrush,
                    dotCx - dotRadius, dotCy - dotRadius,
                    dotRadius * 2, dotRadius * 2);
            }

            using var labelBrush = new SolidBrush(_theme.Text);
            using var labelFont = new Font("Segoe UI", 8f);

            for (int i = 0; i < node.Inputs.Count; i++)
            {
                var pin = node.Inputs[i];
                var p = NodeGeometry.GetPinCanvasPos(pin, _theme);
                DrawPin(g, p, pin.Source != null);
                g.DrawString(pin.Name, labelFont, labelBrush, p.X + _theme.PinRadius + 4, p.Y - 7);
            }

            for (int i = 0; i < node.Outputs.Count; i++)
            {
                var pin = node.Outputs[i];
                var p = NodeGeometry.GetPinCanvasPos(pin, _theme);
                DrawPin(g, p, connected: true);
                var sz = g.MeasureString(pin.Name, labelFont);
                g.DrawString(pin.Name, labelFont, labelBrush, p.X - _theme.PinRadius - sz.Width - 4, p.Y - 7);
            }

            using var footerFont  = new Font("Segoe UI", 7.5f, FontStyle.Italic);
            using var footerBrush = new SolidBrush(_theme.FooterText);
            string footerText = $"by {node.Vendor}";
            var footerSize = g.MeasureString(footerText, footerFont);
            g.DrawString(footerText, footerFont, footerBrush,
                rect.Left + (rect.Width - footerSize.Width) / 2f,
                rect.Bottom - footerSize.Height - 4);
        }

        /// <summary>
        /// Draws a pin disc; <paramref name="connected"/> picks the live vs idle colour.
        /// </summary>
        public void DrawPin(Graphics g, PointF p, bool connected)
        {
            using var fill = new SolidBrush(connected ? _theme.PinConnected : _theme.Pin);
            g.FillEllipse(fill,
                p.X - _theme.PinRadius, p.Y - _theme.PinRadius,
                _theme.PinRadius * 2, _theme.PinRadius * 2);
        }

        /// <summary>
        /// Bezier wire with horizontal control points so wires flow left to right.
        /// </summary>
        public static void DrawWire(Graphics g, PointF a, PointF b, Color color)
        {
            using var pen = new Pen(color, 2f);
            float dx = MathF.Max(40, MathF.Abs(b.X - a.X) / 2);
            var c1 = new PointF(a.X + dx, a.Y);
            var c2 = new PointF(b.X - dx, b.Y);
            g.DrawBezier(pen, a, c1, c2, b);
        }

        /// <summary
        /// >Rounded-rectangle path; <paramref name="topOnly"/> rounds only the top two corners.
        /// </summary>
        public static GraphicsPath RoundedRect(Rectangle r, int radius, bool topOnly = false)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            if (topOnly)
            {
                path.AddLine(r.Right, r.Y + radius, r.Right, r.Bottom);
                path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
                path.AddLine(r.X, r.Bottom, r.X, r.Y + radius);
            }
            else
            {
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            }
            path.CloseFigure();
            return path;
        }
    }
}
