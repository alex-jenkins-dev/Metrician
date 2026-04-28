// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Collections.ObjectModel;
using System.ComponentModel;
using Metrician.Renderable.Contracts;
using Metrician.Rendering;

namespace Metrician.Viewport
{
    public sealed class ViewportPaneConfig
    {
        public string Title { get; set; } = "";
        public StandardView DefaultView { get; set; } = StandardView.Isometric;
        public bool LockOrbit { get; set; } = false;
        public bool LockPan { get; set; } = false;
        public bool LockZoom { get; set; } = false;
    }

    /// <summary>
    /// Toolbar plus <see cref="Viewport3DControl"/>: view selector, lock buttons, reset, fit, sync, maximise.
    /// </summary>
    [ToolboxItem(false)]
    public sealed class ViewportPane : UserControl
    {
        public Viewport3DControl Viewport { get; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PaneTitle { get; set; }

        public event EventHandler? MaximiseToggled;

        private readonly ToolStrip _toolbar;
        private readonly ToolStripDropDownButton _viewBtn;
        private readonly ToolStripButton _syncBtn;
        private readonly ToolStripButton _lockOrbitBtn;
        private readonly ToolStripButton _lockPanBtn;
        private readonly ToolStripButton _lockZoomBtn;
        private readonly ToolStripButton _resetBtn;
        private readonly ToolStripButton _fitBtn;
        private readonly ToolStripButton _maximiseBtn;

        private static readonly Color ToolbarBack = Color.FromArgb(45, 45, 48);
        private static readonly Color ToolbarBorder = Color.FromArgb(63, 63, 70);
        private static readonly Color ActivePaneBorder = Color.FromArgb(0, 122, 204);

        private StandardView _currentView = StandardView.Isometric;
        private bool _isActive;

        public event EventHandler<bool>? SyncToggleRequested;

        public ViewportPane(ViewportPaneConfig cfg, ObservableCollection<IRenderable> renderables)
        {
            PaneTitle = cfg.Title;

            // 2 px margin so the active-pane border is not overpainted.
            Padding = new Padding(2);
            BackColor = ToolbarBack;

            Viewport = new Viewport3DControl
            {
                Dock = DockStyle.Fill,
                ShowGrid = true,
                ShowAxisGizmo = true,
                ViewportBackground = Color.FromArgb(30, 30, 35),
            };

            foreach (var r in renderables)
                Viewport.Renderables.Add(r);

            renderables.CollectionChanged += (_, e) =>
            {
                // Reset carries no NewItems/OldItems, so resync from the source.
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
                {
                    Viewport.Renderables.Clear();
                    foreach (var r in renderables)
                        Viewport.Renderables.Add(r);
                    return;
                }
                if (e.NewItems != null)
                    foreach (IRenderable r in e.NewItems)
                        Viewport.Renderables.Add(r);
                if (e.OldItems != null)
                    foreach (IRenderable r in e.OldItems)
                        Viewport.Renderables.Remove(r);
            };

            _toolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                Height = 26,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = ToolbarBack,
                Padding = new Padding(2, 0, 2, 0),
                RenderMode = ToolStripRenderMode.Professional,
                Renderer = new DarkToolStripRenderer(),
            };

            _viewBtn = new ToolStripDropDownButton
            {
                Text = ViewLabel(cfg.DefaultView),
                AutoSize = true,
                ShowDropDownArrow = true,
                ForeColor = Color.FromArgb(220, 220, 220),
            };

            foreach (var v in new[]
            {
                StandardView.Isometric, StandardView.Front, StandardView.Back,
                StandardView.Right,     StandardView.Left,
                StandardView.Top,       StandardView.Bottom,
            })
            {
                var v2 = v;
                var item = new ToolStripMenuItem(ViewLabel(v2));
                item.Click += (_, _) => SetView(v2);
                _viewBtn.DropDownItems.Add(item);
            }

            _toolbar.Items.Add(_viewBtn);
            _toolbar.Items.Add(new ToolStripSeparator());

            _lockOrbitBtn = MakeLockButton("⟳", "Lock Rotate", cfg.LockOrbit);
            _lockPanBtn = MakeLockButton("✥", "Lock Pan", cfg.LockPan);
            _lockZoomBtn = MakeLockButton("⊕", "Lock Zoom", cfg.LockZoom);

            _lockOrbitBtn.Click += (_, _) => Viewport.MouseInteraction.LockOrbit = _lockOrbitBtn.Checked;
            _lockPanBtn.Click += (_, _) => Viewport.MouseInteraction.LockPan = _lockPanBtn.Checked;
            _lockZoomBtn.Click += (_, _) => Viewport.MouseInteraction.LockZoom = _lockZoomBtn.Checked;

            _toolbar.Items.Add(_lockOrbitBtn);
            _toolbar.Items.Add(_lockPanBtn);
            _toolbar.Items.Add(_lockZoomBtn);
            _toolbar.Items.Add(new ToolStripSeparator());

            _resetBtn = new ToolStripButton("↺")
            {
                ToolTipText = "Reset view",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
            };
            _resetBtn.Click += (_, _) =>
            {
                ApplyViewPreset(_currentView);
                Viewport.Invalidate();
            };
            _toolbar.Items.Add(_resetBtn);

            _fitBtn = new ToolStripButton("⛶")
            {
                ToolTipText = "Fit content to view",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
            };
            _fitBtn.Click += (_, _) => Viewport.FitContent();
            _toolbar.Items.Add(_fitBtn);

            ApplyViewPreset(cfg.DefaultView);
            Viewport.MouseInteraction.LockOrbit = cfg.LockOrbit;
            Viewport.MouseInteraction.LockPan = cfg.LockPan;
            Viewport.MouseInteraction.LockZoom = cfg.LockZoom;

            // Right-aligned items stack from the right in addition order;
            // maximise is added first so it sits furthest right.
            _toolbar.Items.Add(new ToolStripSeparator());
            _maximiseBtn = new ToolStripButton("⤢")
            {
                ToolTipText = "Maximise / Restore",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Alignment = ToolStripItemAlignment.Right,
            };
            _maximiseBtn.Click += (_, _) => MaximiseToggled?.Invoke(this, EventArgs.Empty);
            _toolbar.Items.Add(_maximiseBtn);

            _syncBtn = new ToolStripButton("⇄")
            {
                ToolTipText = "Sync (camera pan + zoom across all panes)",
                CheckOnClick = true,
                Checked = false,
                ForeColor = Color.FromArgb(220, 220, 220),
                AutoSize = true,
                Alignment = ToolStripItemAlignment.Right,
            };
            _syncBtn.Click += (_, _) =>
                SyncToggleRequested?.Invoke(this, _syncBtn.Checked);
            _toolbar.Items.Add(_syncBtn);

            SetStyle(ControlStyles.ResizeRedraw, true);
            Controls.Add(Viewport);
            Controls.Add(_toolbar);

            Paint += OnPanePaint;
        }

        public void SetMaximised(bool maximised)
        {
            _maximiseBtn.Text = maximised ? "↙" : "↗";
            _maximiseBtn.ToolTipText = maximised ? "Restore" : "Maximise";
        }

        private void SetView(StandardView v)
        {
            _currentView = v;
            _viewBtn.Text = ViewLabel(v);
            ApplyViewPreset(v);

            // Plane views lock orbit by default; isometric unlocks it.
            bool isIsometric = (v == StandardView.Isometric);
            _lockOrbitBtn.Checked = !isIsometric;
            Viewport.MouseInteraction.LockOrbit = !isIsometric;

            _lockPanBtn.Checked = false;
            Viewport.MouseInteraction.LockPan = false;

            Viewport.Invalidate();
        }

        private void ApplyViewPreset(StandardView v)
        {
            _currentView = v;
            Viewport.Camera.SetView(v);

            Viewport.Camera.FieldOfView = 45f;
            Viewport.Camera.Distance = 10f;
            Viewport.Camera.Target = System.Numerics.Vector3.Zero;
        }

        private static string ViewLabel(StandardView v) => v switch
        {
            StandardView.Isometric => "3D",
            StandardView.Front => "Front (XZ-)",
            StandardView.Back => "Back (XZ+)",
            StandardView.Right => "Right (YZ+)",
            StandardView.Left => "Left (YZ-)",
            StandardView.Top => "Top (XY+)",
            StandardView.Bottom => "Bottom (XY-)",
            _ => v.ToString(),
        };

        private static ToolStripButton MakeLockButton(string text, string tip, bool initiallyLocked)
        {
            return new ToolStripButton(text)
            {
                ToolTipText = tip,
                CheckOnClick = true,
                Checked = initiallyLocked,
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
            };
        }

        private void OnPanePaint(object? sender, PaintEventArgs e)
        {
            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color color = _isActive ? ActivePaneBorder : ToolbarBorder;
            float width = _isActive ? 2f : 1f;
            using var pen = new Pen(color, width);
            e.Graphics.DrawRectangle(pen, r);
        }

        /// <summary>
        /// Toggles the active-pane border accent.
        /// </summary>
        internal void SetActive(bool active)
        {
            if (_isActive == active) return;
            _isActive = active;
            Invalidate();
        }

        /// <summary>
        /// Sets the Sync button's checked state without raising <see cref="SyncToggleRequested"/>.
        /// </summary>
        internal void SetSyncCheckedSilently(bool isChecked)
        {
            _syncBtn.Checked = isChecked;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Viewport.Dispose();
                _toolbar.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
