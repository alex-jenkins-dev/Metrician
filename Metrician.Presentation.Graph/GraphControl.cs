// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Numerics;
using Metrician.Core.Graph;
using Metrician.Model.Graph;

namespace Metrician.Presentation.Graph
{
    public class GraphControl : UserControl
    {
        public IGraphWorld World { get; }
        public GraphPresenter Presenter { get; }
        public MouseInteraction Mouse { get; }
        public GraphTheme Theme { get; }

        public IList<INodeTemplate> AvailableTemplates { get; } = new List<INodeTemplate>();
        public IList<INodeTemplate> PinnedTemplates { get; } = new List<INodeTemplate>();
        public IDictionary<Keys, INodeTemplate> KeyShortcuts { get; } = new Dictionary<Keys, INodeTemplate>();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IGraphScriptCommands? ScriptCommands { get; set; }

        private readonly GraphPainter _painter;
        private readonly DelayedToolTip _tooltip;

        public GraphControl(IGraphWorld world)
            : this(world, GraphTheme.Dark, LayoutMetrics.Default) { }

        public GraphControl(IGraphWorld world, GraphTheme theme, LayoutMetrics metrics)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Theme = theme ?? throw new ArgumentNullException(nameof(theme));
            Presenter = new GraphPresenter(world, metrics);
            Mouse = new MouseInteraction(world, Presenter);
            _painter = new GraphPainter(theme);
            _tooltip = new DelayedToolTip(this);

            BackColor = theme.Background;
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);
            UpdateStyles();
            TabStop = true;

            Presenter.ViewChanged += (_, _) => Invalidate();
            Mouse.Changed += OnMouseChanged;
            Mouse.ContextMenuRequested += OnContextMenuRequested;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var saved = g.Save();
            try
            {
                g.TranslateTransform(Presenter.Pan.X, Presenter.Pan.Y);
                g.ScaleTransform(Presenter.Zoom, Presenter.Zoom);
                _painter.DrawAll(g, Presenter, Mouse.State);
            }
            finally
            {
                g.Restore(saved);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Focused) Focus();
            if (TryMapButton(e.Button, out var button))
            {
                Mouse.OnDown(button, new Vector2(e.X, e.Y));
                Capture = true;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Mouse.OnMove(new Vector2(e.X, e.Y));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (TryMapButton(e.Button, out var button))
            {
                Mouse.OnUp(button, new Vector2(e.X, e.Y));
                Capture = false;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Mouse.OnWheel(new Vector2(e.X, e.Y), e.Delta);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Mouse.OnLeave();
        }

        private void OnMouseChanged(object? sender, EventArgs e)
        {
            Cursor = MapCursor(Mouse.Cursor);
            UpdateTooltip();
        }

        private void UpdateTooltip()
        {
            _tooltip.Show(
                Mouse.Tooltip,
                new Point((int)Mouse.LastScreen.X + 14, (int)Mouse.LastScreen.Y + 18));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tooltip.Dispose();
            base.Dispose(disposing);
        }

        protected override bool IsInputKey(Keys keyData) =>
            keyData == Keys.Delete
            || KeyShortcuts.ContainsKey(keyData)
            || base.IsInputKey(keyData);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) return;
            if (e.KeyData == Keys.Delete)
            {
                if (Presenter.DeleteSelected())
                    e.Handled = true;
                return;
            }
            if (KeyShortcuts.TryGetValue(e.KeyData, out var template))
            {
                Presenter.Spawn(template, Presenter.ScreenToCanvas(Mouse.LastScreen));
                e.Handled = true;
            }
        }

        private static bool TryMapButton(MouseButtons button, out MouseButton mapped)
        {
            switch (button)
            {
                case MouseButtons.Left:  mapped = MouseButton.Left;  return true;
                case MouseButtons.Right: mapped = MouseButton.Right; return true;
                default: mapped = default; return false;
            }
        }

        private static Cursor MapCursor(CursorKind kind) =>
            kind switch
            {
                CursorKind.Move => Cursors.SizeAll,
                _ => Cursors.Default,
            };

        private void OnContextMenuRequested(object? sender, Vector2 screen)
        {
            var entries = GraphContextMenuBuilder.Build(
                Presenter, screen,
                (IReadOnlyList<INodeTemplate>)AvailableTemplates.ToList(),
                (IReadOnlyList<INodeTemplate>)PinnedTemplates.ToList(),
                ScriptCommands);
            if (entries.Count == 0) return;

            var menu = NewThemedMenu();
            PopulateMenu(menu.Items, entries);
            menu.Show(this, new Point((int)screen.X, (int)screen.Y));
        }

        private void PopulateMenu(ToolStripItemCollection target, IReadOnlyList<ContextMenuItem> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.IsSeparator)
                {
                    target.Add(new ToolStripSeparator());
                    continue;
                }

                var item = ThemedItem(entry.Label);
                item.Enabled = entry.Enabled;
                if (entry.OnClick is { } click)
                    item.Click += (_, _) => click();
                if (entry.Children is { Count: > 0 } children)
                    PopulateMenu(item.DropDownItems, children);
                target.Add(item);
            }
        }

        private ContextMenuStrip NewThemedMenu() => new ContextMenuStrip
        {
            BackColor = Theme.MenuBackground,
            ForeColor = Theme.MenuText,
            ShowImageMargin = false,
            Renderer = new DarkContextMenuRenderer(Theme),
        };

        private ToolStripMenuItem ThemedItem(string label) => new ToolStripMenuItem(label)
        {
            BackColor = Theme.MenuBackground,
            ForeColor = Theme.MenuText,
        };
    }
}
