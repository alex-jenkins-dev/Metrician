// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Library.Viewport
{
    public sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Color Back = Color.FromArgb(45, 45, 48);
        private static readonly Color Border = Color.FromArgb(63, 63, 70);
        private static readonly Color HoverBack = Color.FromArgb(62, 62, 64);

        private static readonly Color CheckBack = Color.FromArgb(255, 0, 122, 204);
        private static readonly Color CheckBorder = Color.FromArgb(255, 120, 180, 240);

        public DarkToolStripRenderer()
            : base(new DarkColorTable()) { RoundedEdges = false; }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(Back);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Border);
            var r = e.AffectedBounds;
            e.Graphics.DrawLine(pen, r.Left, r.Bottom - 1, r.Right, r.Bottom - 1);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            var btn = e.Item as ToolStripButton;
            if (btn == null) { base.OnRenderButtonBackground(e); return; }

            var r = new Rectangle(1, 1, e.Item.Width - 2, e.Item.Height - 2);
            Color fill;
            if (btn.Checked) fill = CheckBack;
            else if (btn.Selected || btn.Pressed) fill = HoverBack;
            else return;

            using var brush = new SolidBrush(fill);
            e.Graphics.FillRectangle(brush, r);

            if (btn.Checked)
            {
                using var pen = new Pen(CheckBorder, 1.5f);
                e.Graphics.DrawRectangle(pen, r);
            }
            else if (btn.Pressed)
            {
                using var pen = new Pen(Border);
                e.Graphics.DrawRectangle(pen, r);
            }
        }

        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed) return;
            var r = new Rectangle(1, 1, e.Item.Width - 2, e.Item.Height - 2);
            using var brush = new SolidBrush(HoverBack);
            e.Graphics.FillRectangle(brush, r);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int x = e.Item.Width / 2;
            using var pen = new Pen(Border);
            e.Graphics.DrawLine(pen, x, 3, x, e.Item.Height - 3);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled)
                e.TextColor = Color.FromArgb(100, 100, 100);
            else if (e.Item is ToolStripButton btn && btn.Checked)
                e.TextColor = Color.White;
            else
                e.TextColor = Color.FromArgb(220, 220, 220);

            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Color.FromArgb(200, 200, 200);
            base.OnRenderArrow(e);
        }
    }

    internal sealed class DarkColorTable : ProfessionalColorTable
    {
        private static readonly Color Back = Color.FromArgb(45, 45, 48);
        private static readonly Color Border = Color.FromArgb(63, 63, 70);
        private static readonly Color Hover = Color.FromArgb(62, 62, 64);

        public override Color MenuItemSelected => Hover;
        public override Color MenuItemBorder => Border;
        public override Color MenuBorder => Border;
        public override Color MenuItemSelectedGradientBegin => Hover;
        public override Color MenuItemSelectedGradientEnd => Hover;
        public override Color MenuItemPressedGradientBegin => Back;
        public override Color MenuItemPressedGradientEnd => Back;
        public override Color ToolStripDropDownBackground => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientBegin => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(37, 37, 38);
        public override Color ImageMarginGradientEnd => Color.FromArgb(37, 37, 38);
        public override Color ButtonSelectedHighlight => Hover;
        public override Color ButtonPressedHighlight => Hover;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
    }
}
