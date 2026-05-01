// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Presentation.Graph
{
    internal sealed class DarkContextMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly GraphTheme _theme;

        public DarkContextMenuRenderer(GraphTheme theme)
            : base(new ThemeColorTable(theme))
        {
            _theme = theme;
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(_theme.MenuBackground);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? _theme.MenuText : _theme.MenuDisabledText;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = _theme.MenuArrow;
            base.OnRenderArrow(e);
        }

        private sealed class ThemeColorTable : ProfessionalColorTable
        {
            private readonly GraphTheme _theme;
            public ThemeColorTable(GraphTheme theme) { _theme = theme; }

            public override Color MenuItemSelected               => _theme.MenuHover;
            public override Color MenuItemBorder                 => _theme.MenuBorder;
            public override Color MenuBorder                     => _theme.MenuBorder;
            public override Color MenuItemSelectedGradientBegin  => _theme.MenuHover;
            public override Color MenuItemSelectedGradientEnd    => _theme.MenuHover;
            public override Color MenuItemPressedGradientBegin   => _theme.MenuBackground;
            public override Color MenuItemPressedGradientEnd     => _theme.MenuBackground;
            public override Color ToolStripDropDownBackground    => _theme.MenuBackground;
            public override Color ImageMarginGradientBegin       => _theme.MenuBackground;
            public override Color ImageMarginGradientMiddle      => _theme.MenuBackground;
            public override Color ImageMarginGradientEnd         => _theme.MenuBackground;
            public override Color SeparatorDark                  => _theme.MenuBorder;
            public override Color SeparatorLight                 => _theme.MenuBorder;
        }
    }
}
