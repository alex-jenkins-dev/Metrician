// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Numerics;
using Metrician.Renderable.Contracts;
using Metrician.Rendering;

namespace Metrician.Viewport
{
    /// <summary>
    /// WinForms control that renders an <see cref="ObservableCollection{IRenderable}"/>
    /// via GDI+. Double-buffered with per-renderable view-volume culling.
    /// </summary>
    [ToolboxItem(true)]
    [Description("A 3-D viewport control that renders an observable collection of IRenderable objects.")]
    public sealed class Viewport3DControl : UserControl
    {
        [Browsable(false)]
        public Camera3D Camera { get; } = new Camera3D();

        [Browsable(false)]
        public ObservableCollection<IRenderable> Renderables { get; } = new();

        [Browsable(false)]
        public MouseInteraction MouseInteraction { get; }

        [Category("Appearance")]
        [Description("Background fill colour of the 3D viewport.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color ViewportBackground { get; set; } = Color.FromArgb(30, 30, 35);

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowGrid { get; set; } = true;

        [Category("Appearance")]
        [DefaultValue(true)]
        public bool ShowAxisGizmo { get; set; } = true;

        /// <summary>AABB view-volume culling. Disable only for debugging.</summary>
        [Category("Performance")]
        [DefaultValue(true)]
        public bool UseViewVolumeCulling { get; set; } = true;

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BoxZoomOutlineColour { get; set; } = Color.FromArgb(220, 100, 200, 255);

        [Category("Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BoxZoomFillColour { get; set; } = Color.FromArgb(40, 100, 200, 255);

        private Bitmap? _backBuffer;
        private Graphics? _backBufferGraphics;

        private readonly Pen _gridPenMinor = new(Color.FromArgb(50, 255, 255, 255), 1f);
        private readonly Pen _gridPenMajor = new(Color.FromArgb(90, 255, 255, 255), 1f);
        private readonly Pen _axisX = new(Color.FromArgb(210, 220, 60, 60), 2f);
        private readonly Pen _axisY = new(Color.FromArgb(210, 60, 200, 60), 2f);
        private readonly Pen _axisZ = new(Color.FromArgb(210, 60, 100, 220), 2f);

        public Viewport3DControl()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();

            MouseInteraction = new MouseInteraction(Camera, this);
            MouseInteraction.CameraChanged += (_, _) => Invalidate();

            Renderables.CollectionChanged += OnCollectionChanged;

            // Reserve numpad/Home/F so dialog navigation does not swallow them.
            PreviewKeyDown += (_, e) =>
            {
                e.IsInputKey = e.KeyCode is Keys.NumPad1 or Keys.NumPad3
                                         or Keys.NumPad5 or Keys.NumPad7
                                         or Keys.Home or Keys.F;
            };
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (MouseInteraction.HandleKeyDown(e)) { e.Handled = true; return; }

            if (e.KeyCode == Keys.F && !e.Control && !e.Alt && !e.Shift)
            {
                FitContent();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.C && e.Control && !e.Alt && !e.Shift)
            {
                CopyToClipboard();
                e.Handled = true;
                return;
            }
        }

        /// <summary>
        /// Captures the viewport (scene + gizmo + overlays) and copies it to the clipboard.
        /// </summary>
        public void CopyToClipboard()
        {
            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);
            using var bmp = new Bitmap(w, h);
            DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
            try
            {
                Clipboard.SetImage(bmp);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // Clipboard transiently locked; ignore.
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (InvokeRequired)
                BeginInvoke(new Action(Invalidate));
            else
                Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            EnsureBackBuffer();

            var context = new RenderContext(
                _backBufferGraphics!,
                Camera,
                ClientSize.Width,
                ClientSize.Height,
                _backBuffer);

            _backBufferGraphics!.Clear(ViewportBackground);
            _backBufferGraphics.SetClip(ClientRectangle);
            _backBufferGraphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (ShowGrid) DrawGrid(context);

            foreach (var renderable in Renderables)
            {
                if (!renderable.IsVisible) continue;
                if (UseViewVolumeCulling && renderable.Bounds.HasValue)
                    if (!renderable.Bounds.Value.IsInViewVolume(context.ViewProjection))
                        continue;
                renderable.Render(context);
            }

            e.Graphics.DrawImageUnscaled(_backBuffer!, 0, 0);

            // Overlays draw after the blit so they sit on top of the scene clip.
            if (ShowAxisGizmo) DrawAxisGizmo(e.Graphics);
            if (MouseInteraction.IsBoxZooming && MouseInteraction.BoxZoomScreenRect is { } boxRect)
                DrawBoxZoomOverlay(e.Graphics, boxRect);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Suppressed; OnPaint clears the back buffer instead.
        }

        private void EnsureBackBuffer()
        {
            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);

            if (_backBuffer is not null &&
                _backBuffer.Width == w &&
                _backBuffer.Height == h)
                return;

            _backBufferGraphics?.Dispose();
            _backBuffer?.Dispose();

            _backBuffer = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            _backBufferGraphics = Graphics.FromImage(_backBuffer);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        private void DrawGrid(RenderContext ctx)
        {
            const int half = 10;
            const float step = 1f;
            float minor = step;
            float major = step * 5f;

            for (float i = -half; i <= half; i += minor)
            {
                bool isMajor = (Math.Abs(i % major) < 0.001f);
                var pen = isMajor ? _gridPenMajor : _gridPenMinor;

                var p0 = ctx.Project(new Vector3(i, -half, 0f));
                var p1 = ctx.Project(new Vector3(i,  half, 0f));
                ctx.Graphics.DrawLine(pen, p0, p1);

                var p2 = ctx.Project(new Vector3(-half, i, 0f));
                var p3 = ctx.Project(new Vector3( half, i, 0f));
                ctx.Graphics.DrawLine(pen, p2, p3);
            }
        }

        private static readonly Font _gizmoFont = new("Segoe UI", 7f, FontStyle.Bold);

        private void DrawAxisGizmo(Graphics g)
        {
            const int size = 60;
            const int margin = 14;
            int cx = margin + size / 2;
            int cy = ClientSize.Height - margin - size / 2;

            Matrix4x4 view = Camera.ViewMatrix;
            var axes = new (Vector3 dir, Pen pen, string label)[]
            {
                (new Vector3(1, 0, 0), _axisX, "X"),
                (new Vector3(0, 1, 0), _axisY, "Y"),
                (new Vector3(0, 0, 1), _axisZ, "Z"),
            };

            Array.Sort(axes, (a, b) =>
            {
                float da = Vector3.TransformNormal(a.dir, view).Z;
                float db = Vector3.TransformNormal(b.dir, view).Z;
                return da.CompareTo(db);
            });

            foreach (var (dir, pen, label) in axes)
            {
                var screenDir = Vector3.TransformNormal(dir, view);
                float ex = cx + screenDir.X * (size / 2f - 4);
                float ey = cy - screenDir.Y * (size / 2f - 4);

                g.DrawLine(pen, cx, cy, ex, ey);
                using var brush = new SolidBrush(pen.Color);
                g.DrawString(label, _gizmoFont, brush, ex - 5, ey - 7);
            }
        }

        /// <summary>
        /// Re-frames the camera so every visible bounded renderable fits with
        /// a small margin. Orthographic-only: binding uses lateral extents at
        /// the focal plane and ignores depth.
        /// </summary>
        /// <param name="margin">
        /// Multiplier on the binding distance; 1.05 leaves a 5% border.
        /// </param>
        public void FitContent(float margin = 1.05f)
        {
            BoundingBox3D? union = null;
            foreach (var r in Renderables)
            {
                if (!r.IsVisible) continue;
                if (!r.Bounds.HasValue) continue;
                var b = r.Bounds.Value;
                union = union is null
                    ? b
                    : new BoundingBox3D(
                        Vector3.Min(union.Value.Min, b.Min),
                        Vector3.Max(union.Value.Max, b.Max));
            }
            if (union is null) return;

            var u = union.Value;
            var center = u.Center;

            // Re-orthogonalise so any drift in Up cancels.
            var forward = Vector3.Normalize(Camera.Target - Camera.Eye);
            var right = Vector3.Normalize(Vector3.Cross(forward, Camera.Up));
            var up = Vector3.Normalize(Vector3.Cross(right, forward));

            float aspect = ClientSize.Height > 0
                ? (float)ClientSize.Width / ClientSize.Height
                : 1f;
            float fovHalfV = Camera.FieldOfView * MathF.PI / 180f * 0.5f;
            float fovHalfH = MathF.Atan(aspect * MathF.Tan(fovHalfV));
            float tanH = MathF.Tan(fovHalfH);
            float tanV = MathF.Tan(fovHalfV);

            // For each corner: visible half-width = D*tanH, so D = |pu|/tanH (and similarly for V).
            float requiredDistance = 0f;

            for (int dx = 0; dx < 2; dx++)
            for (int dy = 0; dy < 2; dy++)
            for (int dz = 0; dz < 2; dz++)
            {
                var corner = new Vector3(
                    dx == 0 ? u.Min.X : u.Max.X,
                    dy == 0 ? u.Min.Y : u.Max.Y,
                    dz == 0 ? u.Min.Z : u.Max.Z);
                var rel = corner - center;
                float pu = Vector3.Dot(rel, right);
                float pv = Vector3.Dot(rel, up);

                float dh = MathF.Abs(pu) / tanH;
                float dv = MathF.Abs(pv) / tanV;

                float d = MathF.Max(dh, dv);
                if (d > requiredDistance) requiredDistance = d;
            }

            float distance = requiredDistance * margin;

            if (distance < 1e-3f) distance = 1f;

            Camera.Target = center;
            Camera.Distance = Math.Clamp(distance, Camera.MinDistance, Camera.MaxDistance);
            Invalidate();
        }

        private void DrawBoxZoomOverlay(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using var fill = new SolidBrush(BoxZoomFillColour);
            g.FillRectangle(fill, rect);

            using var pen = new Pen(BoxZoomOutlineColour, 1.5f) { DashStyle = DashStyle.Dash };
            g.DrawRectangle(pen, rect);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                MouseInteraction.Dispose();
                Renderables.CollectionChanged -= OnCollectionChanged;

                _backBufferGraphics?.Dispose();
                _backBuffer?.Dispose();

                _gridPenMinor.Dispose();
                _gridPenMajor.Dispose();
                _axisX.Dispose();
                _axisY.Dispose();
                _axisZ.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
