// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Collections.ObjectModel;
using System.ComponentModel;
using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Library.Viewport
{
    /// <summary>
    /// 2x2 grid of <see cref="ViewportPane"/>s with resizable dividers, sharing
    /// one renderable collection. Default layout: TL = 3D/Isometric (unlocked),
    /// TR = Front, BL = Top, BR = Right (orbit-locked).
    /// </summary>
    [ToolboxItem(true)]
    [Description("Classic 4-up 3D modelling viewport with resizable dividers.")]
    public sealed class FourUpViewport : UserControl
    {
        private const int DividerThickness = 5;
        private const float MinSplitFraction = 0.1f;
        private const float MaxSplitFraction = 0.9f;

        private float _splitX = 0.5f;
        private float _splitY = 0.5f;
        private int? _maximisedIndex = null;

        private bool _draggingH = false;
        private bool _draggingV = false;

        private DateTime _lastDividerClick = DateTime.MinValue;
        private Point _lastDividerPt = Point.Empty;

        private readonly ViewportPane[] _panes = new ViewportPane[4];
        private int? _activePaneIndex;

        /// <summary>
        /// Renderables shared across all four panes; add geometry here and it appears everywhere.
        /// </summary>
        public ObservableCollection<IRenderable> Renderables { get; } = new();

        public ViewportPane Pane(int index) => _panes[index];

        private bool _syncCameras;
        private bool _syncing;

        /// <summary>
        /// When true, panning and zooming any pane mirrors Target and Distance
        /// to the others. Orbit and projection remain per-pane so plane views
        /// stay axis-aligned.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool SyncCameras
        {
            get => _syncCameras;
            set => SetSyncCameras(value, sourceIdx: 0);
        }

        public FourUpViewport()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = Color.FromArgb(20, 20, 22);

            var configs = new ViewportPaneConfig[]
            {
                new() { Title = "3D",          DefaultView = StandardView.Isometric,
                        LockOrbit = false, LockPan = false, LockZoom = false },
                new() { Title = "Front",       DefaultView = StandardView.Front,
                        LockOrbit = true,  LockPan = false, LockZoom = false },
                new() { Title = "Top",         DefaultView = StandardView.Top,
                        LockOrbit = true,  LockPan = false, LockZoom = false },
                new() { Title = "Right",       DefaultView = StandardView.Right,
                        LockOrbit = true,  LockPan = false, LockZoom = false },
            };

            for (int i = 0; i < 4; i++)
            {
                var pane = new ViewportPane(configs[i], Renderables);
                var idx = i;
                pane.MaximiseToggled += (_, _) => ToggleMaximise(idx);
                pane.SyncToggleRequested += (_, on) => SetSyncCameras(on, idx);
                pane.Enter += (_, _) => SetActivePane(idx);
                _panes[i] = pane;
                Controls.Add(pane);
            }

            MouseDown += OnDividerMouseDown;
            MouseMove += OnDividerMouseMove;
            MouseUp += OnDividerMouseUp;
            MouseLeave += (_, _) => Cursor = Cursors.Default;

            Resize += (_, _) => LayoutPanes();
        }

        private void LayoutPanes()
        {
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            if (_maximisedIndex.HasValue)
            {
                for (int i = 0; i < 4; i++)
                {
                    _panes[i].Visible = (i == _maximisedIndex.Value);
                    if (i == _maximisedIndex.Value)
                        _panes[i].Bounds = new Rectangle(0, 0, w, h);
                }
                return;
            }

            for (int i = 0; i < 4; i++) _panes[i].Visible = true;

            int halfD = DividerThickness / 2;
            int splitPx = (int)(w * _splitX);
            int splitPy = (int)(h * _splitY);

            _panes[0].Bounds = new Rectangle(
                0, 0, splitPx - halfD, splitPy - halfD);
            _panes[1].Bounds = new Rectangle(
                splitPx + halfD + 1, 0,
                w - splitPx - halfD - 1, splitPy - halfD);
            _panes[2].Bounds = new Rectangle(
                0, splitPy + halfD + 1,
                splitPx - halfD, h - splitPy - halfD - 1);
            _panes[3].Bounds = new Rectangle(
                splitPx + halfD + 1, splitPy + halfD + 1,
                w - splitPx - halfD - 1, h - splitPy - halfD - 1);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_maximisedIndex.HasValue) return;

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            int splitPx = (int)(w * _splitX);
            int splitPy = (int)(h * _splitY);

            var g = e.Graphics;
            g.FillRectangle(SystemBrushes.ControlDark,
                splitPx - DividerThickness / 2, 0, DividerThickness, h);
            g.FillRectangle(SystemBrushes.ControlDark,
                0, splitPy - DividerThickness / 2, w, DividerThickness);
            g.FillRectangle(SystemBrushes.Control,
                splitPx - DividerThickness / 2,
                splitPy - DividerThickness / 2,
                DividerThickness, DividerThickness);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
        }

        private enum DividerHit { None, Vertical, Horizontal, Both }

        private DividerHit HitTest(Point pt)
        {
            if (_maximisedIndex.HasValue) return DividerHit.None;

            int splitPx = (int)(ClientSize.Width * _splitX);
            int splitPy = (int)(ClientSize.Height * _splitY);
            int half = DividerThickness / 2 + 1;

            bool onV = Math.Abs(pt.X - splitPx) <= half;
            bool onH = Math.Abs(pt.Y - splitPy) <= half;

            if (onV && onH) return DividerHit.Both;
            if (onV) return DividerHit.Vertical;
            if (onH) return DividerHit.Horizontal;
            return DividerHit.None;
        }

        private void OnDividerMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var hit = HitTest(e.Location);
            if (hit == DividerHit.None) return;

            var now = DateTime.UtcNow;
            bool isDoubleClick =
                (now - _lastDividerClick).TotalMilliseconds <= SystemInformation.DoubleClickTime &&
                Math.Abs(e.X - _lastDividerPt.X) <= SystemInformation.DoubleClickSize.Width &&
                Math.Abs(e.Y - _lastDividerPt.Y) <= SystemInformation.DoubleClickSize.Height;

            _lastDividerClick = now;
            _lastDividerPt = e.Location;

            if (isDoubleClick)
            {
                switch (hit)
                {
                    case DividerHit.Vertical: _splitX = 0.5f; break;
                    case DividerHit.Horizontal: _splitY = 0.5f; break;
                    case DividerHit.Both: _splitX = 0.5f; _splitY = 0.5f; break;
                }
                _lastDividerClick = DateTime.MinValue;
                LayoutPanes();
                return;
            }

            _draggingV = hit == DividerHit.Vertical || hit == DividerHit.Both;
            _draggingH = hit == DividerHit.Horizontal || hit == DividerHit.Both;
            Capture = true;
        }

        private void OnDividerMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_draggingV && !_draggingH)
            {
                Cursor = HitTest(e.Location) switch
                {
                    DividerHit.Vertical => Cursors.VSplit,
                    DividerHit.Horizontal => Cursors.HSplit,
                    DividerHit.Both => Cursors.SizeAll,
                    _ => Cursors.Default,
                };
                return;
            }

            int w = ClientSize.Width;
            int h = ClientSize.Height;

            if (_draggingV && w > 0)
                _splitX = Math.Clamp((float)e.X / w, MinSplitFraction, MaxSplitFraction);
            if (_draggingH && h > 0)
                _splitY = Math.Clamp((float)e.Y / h, MinSplitFraction, MaxSplitFraction);

            LayoutPanes();
        }

        private void OnDividerMouseUp(object? sender, MouseEventArgs e)
        {
            _draggingV = false;
            _draggingH = false;
            Capture = false;
            Cursor = Cursors.Default;
        }

        private void ToggleMaximise(int paneIndex)
        {
            if (_maximisedIndex == paneIndex)
            {
                _maximisedIndex = null;
                foreach (var p in _panes) p.SetMaximised(false);
            }
            else
            {
                _maximisedIndex = paneIndex;
                for (int i = 0; i < 4; i++)
                    _panes[i].SetMaximised(i == paneIndex);
            }

            LayoutPanes();
        }

        private void SetActivePane(int index)
        {
            if (_activePaneIndex == index) return;
            _activePaneIndex = index;
            for (int i = 0; i < 4; i++)
                _panes[i].SetActive(i == index);
        }

        private void SetSyncCameras(bool on, int sourceIdx)
        {
            foreach (var p in _panes) p.SetSyncCheckedSilently(on);

            if (_syncCameras == on) return;
            _syncCameras = on;

            if (on)
            {
                int src = Math.Clamp(sourceIdx, 0, _panes.Length - 1);
                var sourceCam = _panes[src].Viewport.Camera;
                for (int i = 0; i < _panes.Length; i++)
                {
                    if (i == src) continue;
                    var v = _panes[i].Viewport;
                    v.Camera.Target = sourceCam.Target;
                    v.Camera.Distance = sourceCam.Distance;
                    v.Invalidate();
                }
                AttachCameraSync();
            }
            else
            {
                DetachCameraSync();
            }
        }

        private void AttachCameraSync()
        {
            foreach (var p in _panes)
                p.Viewport.MouseInteraction.CameraChanged += OnPaneCameraChanged;
        }

        private void DetachCameraSync()
        {
            foreach (var p in _panes)
                p.Viewport.MouseInteraction.CameraChanged -= OnPaneCameraChanged;
        }

        private void OnPaneCameraChanged(object? sender, EventArgs e)
        {
            if (_syncing) return;

            Viewport3DControl? source = null;
            for (int i = 0; i < _panes.Length; i++)
            {
                if (ReferenceEquals(_panes[i].Viewport.MouseInteraction, sender))
                {
                    source = _panes[i].Viewport;
                    break;
                }
            }
            if (source is null) return;

            _syncing = true;
            try
            {
                var sc = source.Camera;
                for (int i = 0; i < _panes.Length; i++)
                {
                    var v = _panes[i].Viewport;
                    if (v == source) continue;
                    v.Camera.Target = sc.Target;
                    v.Camera.Distance = sc.Distance;
                    v.Invalidate();
                }

                // Synchronous repaint: under continuous mouse movement WM_PAINT
                // can starve siblings, so force every pane to redraw now.
                for (int i = 0; i < _panes.Length; i++)
                    _panes[i].Viewport.Update();
            }
            finally { _syncing = false; }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutPanes();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LayoutPanes();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                foreach (var p in _panes)
                    p.Dispose();
            base.Dispose(disposing);
        }
    }
}
