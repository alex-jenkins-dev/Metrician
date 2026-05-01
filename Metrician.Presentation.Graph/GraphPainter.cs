// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing.Drawing2D;
using System.Numerics;
using Metrician.Core.Graph;
using Metrician.Model.Graph;

namespace Metrician.Presentation.Graph
{
    public sealed class GraphPainter
    {
        private readonly GraphTheme _theme;

        public GraphPainter(GraphTheme theme)
        {
            _theme = theme;
        }

        public void DrawAll(Graphics g, GraphPresenter p, InteractionState interactionState)
        {
            var world = p.World;
            var m = p.Metrics;

            foreach (var wire in world.Wires.All)
            {
                bool sourceFailed = world.Errors.Get(wire.Source.Owner).Count > 0;
                DrawWire(g,
                    Geometry.PinPosition(world, wire.Source, m),
                    Geometry.PinPosition(world, wire.Target, m),
                    sourceFailed ? _theme.WireError : _theme.Wire);
            }

            foreach (var node in world.Nodes.All)
                DrawNode(g, world, node, p, p.SelectedNode is { } sel && sel == node.Id);

            if (interactionState is InteractionState.DraggingWire dw)
                DrawWire(g,
                    Geometry.PinPosition(world, dw.Source, m),
                    dw.EndCanvas,
                    _theme.WireDrag);
        }

        private void DrawNode(
            Graphics g, IGraphWorld world, Node node, GraphPresenter p, bool selected)
        {
            var m = p.Metrics;
            var rect = ToRectangle(Geometry.NodeRect(world, node.Id, m));
            var headerRect = new Rectangle(rect.X, rect.Y, rect.Width, m.HeaderHeight);

            using (var path = RoundedRect(rect, m.CornerRadius))
            using (var bg = new SolidBrush(_theme.NodeBackground))
                g.FillPath(bg, path);

            using (var headerPath = RoundedRect(headerRect, m.CornerRadius, topOnly: true))
            using (var hb = new SolidBrush(_theme.NodeHeader))
                g.FillPath(hb, headerPath);

            var borderColor = selected ? _theme.SelectedBorder : _theme.NodeBorder;
            using (var path = RoundedRect(rect, m.CornerRadius))
            using (var border = new Pen(borderColor, selected ? 2f : 1f))
                g.DrawPath(border, path);

            using (var titleBrush = new SolidBrush(_theme.Text))
            using (var titleFont = new Font(_theme.FontFamily, 9f, FontStyle.Bold))
            {
                var titleSize = g.MeasureString(node.Title, titleFont);
                float titleX = rect.X + (rect.Width - titleSize.Width) / 2f;
                float titleY = rect.Y + (m.HeaderHeight - titleSize.Height) / 2f;
                g.DrawString(node.Title, titleFont, titleBrush, titleX, titleY);
            }

            const int dotRadius = 4;

            var statusCenter = Geometry.StatusDotPosition(world, node.Id, m);
            var statusColour = ResolveStatusColour(world, node.Id);
            using (var statusBrush = new SolidBrush(statusColour))
                g.FillEllipse(statusBrush,
                    statusCenter.X - dotRadius, statusCenter.Y - dotRadius,
                    dotRadius * 2, dotRadius * 2);

            if (world.DynamicUpdates.HasLifetime(node.Id))
            {
                var dynCenter = Geometry.DynamicDotPosition(world, node.Id, m);
                using var dotBrush = new SolidBrush(_theme.DynamicIndicator);
                g.FillEllipse(dotBrush,
                    dynCenter.X - dotRadius, dynCenter.Y - dotRadius,
                    dotRadius * 2, dotRadius * 2);
            }

            using var labelBrush = new SolidBrush(_theme.Text);
            using var labelFont = new Font(_theme.FontFamily, 8f);

            foreach (var pin in world.Pins.Inputs(node.Id))
            {
                var pos = Geometry.PinPosition(world, pin.Id, m);
                bool wired = world.Wires.SourceOf(pin.Id) is not null;
                var colour = ResolvePinColour(world, pin.Id);
                DrawPin(g, pos, colour, m.PinRadius, filled: wired, hollowFill: _theme.PinHollowFill);
                g.DrawString(pin.Id.Name, labelFont, labelBrush,
                    pos.X + m.PinRadius + 4, pos.Y - 7);
            }

            foreach (var pin in world.Pins.Outputs(node.Id))
            {
                var pos = Geometry.PinPosition(world, pin.Id, m);
                bool connected = world.Wires.All.Any(w => w.Source == pin.Id);
                var colour = ResolvePinColour(world, pin.Id);
                DrawPin(g, pos, colour, m.PinRadius, filled: connected, hollowFill: _theme.PinHollowFill);
                var sz = g.MeasureString(pin.Id.Name, labelFont);
                g.DrawString(pin.Id.Name, labelFont, labelBrush,
                    pos.X - m.PinRadius - sz.Width - 4, pos.Y - 7);
            }

            using var footerFont = new Font(_theme.FontFamily, 7.5f, FontStyle.Italic);
            using var footerBrush = new SolidBrush(_theme.FooterText);
            string footerText = $"by {node.Vendor}";
            var footerSize = g.MeasureString(footerText, footerFont);
            g.DrawString(footerText, footerFont, footerBrush,
                rect.Left + (rect.Width - footerSize.Width) / 2f,
                rect.Bottom - footerSize.Height - 4);
        }

        private Color ResolveStatusColour(IGraphWorld world, NodeId id)
        {
            if (world.Errors.Get(id).Count > 0) return _theme.StatusError;
            var status = world.Status.Get(id);
            if (status?.Readiness == NodeReadiness.Ready) return _theme.StatusReady;
            return _theme.StatusNotReady;
        }

        private Color ResolvePinColour(IGraphWorld world, PinId pin)
        {
            var c = world.PinColours.Get(pin);
            if (c is { } pc) return Color.FromArgb(pc.A, pc.R, pc.G, pc.B);
            return _theme.PinConnected;
        }

        public static void DrawPin(
            Graphics g, Vector2 p, Color colour, int radius,
            bool filled = true, Color? hollowFill = null)
        {
            float x = p.X - radius;
            float y = p.Y - radius;
            float d = radius * 2;
            if (filled)
            {
                using var fill = new SolidBrush(colour);
                g.FillEllipse(fill, x, y, d, d);
            }
            else
            {
                if (hollowFill is { } bg)
                {
                    using var fill = new SolidBrush(bg);
                    g.FillEllipse(fill, x, y, d, d);
                }
                using var pen = new Pen(colour, 1.5f);
                g.DrawEllipse(pen, x, y, d, d);
            }
        }

        public static void DrawWire(Graphics g, Vector2 a, Vector2 b, Color colour)
        {
            using var pen = new Pen(colour, 2f);
            float dx = MathF.Max(40, MathF.Abs(b.X - a.X) / 2);
            var c1 = new PointF(a.X + dx, a.Y);
            var c2 = new PointF(b.X - dx, b.Y);
            g.DrawBezier(pen, new PointF(a.X, a.Y), c1, c2, new PointF(b.X, b.Y));
        }

        public static GraphicsPath RoundedRect(
            Rectangle r, int radius, bool topOnly = false)
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

        private static Rectangle ToRectangle(Rect r) =>
            new((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
    }
}
