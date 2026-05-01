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
        public GraphTheme Theme { get; }

        public IList<INodeTemplate> AvailableTemplates { get; } = new List<INodeTemplate>();
        public IList<INodeTemplate> PinnedTemplates { get; } = new List<INodeTemplate>();
        public IDictionary<Keys, INodeTemplate> KeyShortcuts { get; } = new Dictionary<Keys, INodeTemplate>();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IGraphScriptCommands? ScriptCommands { get; set; }

        private readonly GraphPainter _painter;
        private Vector2 _lastScreen;

        public GraphControl(IGraphWorld world)
            : this(world, GraphTheme.Dark, LayoutMetrics.Default) { }

        public GraphControl(IGraphWorld world, GraphTheme theme, LayoutMetrics metrics)
        {
            World = world ?? throw new ArgumentNullException(nameof(world));
            Theme = theme ?? throw new ArgumentNullException(nameof(theme));
            Presenter = new GraphPresenter(world, metrics);
            _painter = new GraphPainter(theme);

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
            Presenter.ContextMenuRequested += OnContextMenuRequested;
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
                _painter.DrawAll(g, Presenter);
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
            var screen = new Vector2(e.X, e.Y);
            _lastScreen = screen;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Presenter.OnLeftDown(screen);
                    Capture = true;
                    break;
                case MouseButtons.Right:
                    Presenter.OnRightDown(screen);
                    Capture = true;
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _lastScreen = new Vector2(e.X, e.Y);
            Presenter.OnMove(_lastScreen);
            Cursor = Presenter.State is InteractionState.Panning
                ? Cursors.SizeAll
                : Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            var screen = new Vector2(e.X, e.Y);
            _lastScreen = screen;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    Presenter.OnLeftUp(screen);
                    Capture = false;
                    break;
                case MouseButtons.Right:
                    Presenter.OnRightUp(screen);
                    Capture = false;
                    break;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Presenter.OnWheel(new Vector2(e.X, e.Y), e.Delta);
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
                Presenter.Spawn(template, Presenter.ScreenToCanvas(_lastScreen));
                e.Handled = true;
            }
        }

        private void OnContextMenuRequested(object? sender, Vector2 screen)
        {
            var menu = BuildMenu(screen);
            if (menu is not null)
                menu.Show(this, new Point((int)screen.X, (int)screen.Y));
        }

        protected virtual ContextMenuStrip? BuildMenu(Vector2 screen)
        {
            var canvas = Presenter.ScreenToCanvas(screen);
            var hit = Geometry.NodeAt(World, canvas, Presenter.Metrics);

            var menu = NewThemedMenu();
            if (hit is { } id)
            {
                var node = World.Nodes.Get(id);
                if (node is null) return null;
                var header = ThemedItem(node.Title);
                header.Enabled = false;
                menu.Items.Add(header);
                menu.Items.Add(new ToolStripSeparator());
                var del = ThemedItem("Delete");
                del.Click += (_, _) => World.Remove(id);
                menu.Items.Add(del);
            }
            else
            {
                if (AvailableTemplates.Count + PinnedTemplates.Count > 0)
                {
                    var add = ThemedItem("Add");
                    var grouped = AvailableTemplates
                        .GroupBy(t => string.IsNullOrEmpty(t.Vendor) ? "Other" : t.Vendor)
                        .OrderBy(g => g.Key);

                    foreach (var grouping in grouped)
                    {
                        var vendor = ThemedItem(grouping.Key);
                        foreach (var template in grouping.OrderBy(t => t.Title))
                            vendor.DropDownItems.Add(BuildSpawnItem(template, canvas));
                        add.DropDownItems.Add(vendor);
                    }

                    if (PinnedTemplates.Count > 0)
                    {
                        if (add.DropDownItems.Count > 0)
                            add.DropDownItems.Add(new ToolStripSeparator());
                        foreach (var template in PinnedTemplates)
                            add.DropDownItems.Add(BuildSpawnItem(template, canvas));
                    }

                    menu.Items.Add(add);
                    menu.Items.Add(new ToolStripSeparator());
                }
                var reset = ThemedItem("Reset View");
                reset.Click += (_, _) => Presenter.ResetView();
                menu.Items.Add(reset);

                var clear = ThemedItem("Clear Graph");
                clear.Click += (_, _) =>
                {
                    foreach (var node in World.Nodes.All.ToList())
                        World.Remove(node.Id);
                };
                menu.Items.Add(clear);

                if (ScriptCommands is { } commands)
                {
                    menu.Items.Add(new ToolStripSeparator());

                    var save = ThemedItem("Save Graph");
                    save.Click += (_, _) => commands.Save();
                    menu.Items.Add(save);

                    var load = ThemedItem("Load Graph");
                    load.Click += (_, _) => commands.LoadReplace();
                    menu.Items.Add(load);

                    var anchor = canvas;
                    var append = ThemedItem("Append Graph");
                    append.Click += (_, _) => commands.LoadAppend(anchor);
                    menu.Items.Add(append);
                }
            }
            return menu;
        }

        private ToolStripMenuItem BuildSpawnItem(INodeTemplate template, Vector2 canvasAt)
        {
            var captured = template;
            var pos = canvasAt;
            var item = ThemedItem(captured.Title);
            item.Click += (_, _) => Presenter.Spawn(captured, pos);
            return item;
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
