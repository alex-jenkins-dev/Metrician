// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Rendering;

namespace Metrician.Viewport
{
    public enum RotationCenterMode
    {
        /// <summary>
        /// Orbit pivots the world origin; Target is snapped to (0,0,0) on each orbit.
        /// </summary>
        Centre,

        /// <summary>
        /// Orbit pivots whatever the camera is currently focused on (the focal plane).
        /// </summary>
        Eye,
    }

    /// <summary>
    /// Mouse/keyboard binding from a <see cref="Control"/> to a <see cref="Camera3D"/>:
    /// orbit (left drag), pan (right drag or Alt+left), box-zoom (middle drag),
    /// and zoom-toward-cursor on the wheel.
    /// </summary>
    public sealed class MouseInteraction : IDisposable
    {
        private readonly Camera3D _camera;
        private readonly Control _control;

        private Point _lastPos;
        private bool _isOrbiting;
        private bool _isPanning;
        private bool _isBoxZooming;

        // Pan locks the camera frame and focal plane at drag start so cursor
        // rays use a stable inverse VP; using the live camera would feed back
        // into Target and oscillate frame-to-frame.
        private Vector3 _panAnchorWorld;
        private Vector3 _panTargetAtStart;
        private Vector3 _panPlaneNormal;
        private float _panPlaneDist;
        private Matrix4x4 _panInvVPAtStart;
        private int _panVpWidth;
        private int _panVpHeight;
        private Point _boxZoomStart;
        private Point _boxZoomCurrent;

        public float OrbitSensitivity { get; set; } = 0.005f;
        public float ZoomStep { get; set; } = 0.1f;

        public MouseButtons OrbitButton { get; set; } = MouseButtons.Left;
        public MouseButtons PanButton { get; set; } = MouseButtons.Right;
        public MouseButtons BoxZoomButton { get; set; } = MouseButtons.Middle;

        /// <summary>
        /// Min side length in pixels for a box-zoom; below this, treated as a click.
        /// </summary>
        public int BoxZoomMinPixels { get; set; } = 5;

        public bool AltLeftPan { get; set; } = true;

        public bool LockOrbit { get; set; } = false;
        public bool LockPan { get; set; } = false;
        public bool LockZoom { get; set; } = false;
        public bool LockBoxZoom { get; set; } = false;

        public RotationCenterMode RotationCenter { get; set; } = RotationCenterMode.Centre;

        public bool IsBoxZooming => _isBoxZooming;

        /// <summary>
        /// Box-zoom rectangle in client coords with non-negative size, or null when not box-zooming.
        /// </summary>
        public Rectangle? BoxZoomScreenRect => _isBoxZooming
            ? Rectangle.FromLTRB(
                Math.Min(_boxZoomStart.X, _boxZoomCurrent.X),
                Math.Min(_boxZoomStart.Y, _boxZoomCurrent.Y),
                Math.Max(_boxZoomStart.X, _boxZoomCurrent.X),
                Math.Max(_boxZoomStart.Y, _boxZoomCurrent.Y))
            : null;

        /// <summary>
        /// Raised when the camera or box-zoom rectangle changes; the viewport repaints in response.
        /// </summary>
        public event EventHandler? CameraChanged;

        public MouseInteraction(Camera3D camera, Control control)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _control = control ?? throw new ArgumentNullException(nameof(control));

            _control.MouseDown += OnMouseDown;
            _control.MouseMove += OnMouseMove;
            _control.MouseUp += OnMouseUp;
            _control.MouseWheel += OnMouseWheel;
        }

        public void Dispose()
        {
            _control.MouseDown -= OnMouseDown;
            _control.MouseMove -= OnMouseMove;
            _control.MouseUp -= OnMouseUp;
            _control.MouseWheel -= OnMouseWheel;
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            bool altHeld = (Control.ModifierKeys & Keys.Alt) != 0;

            if (e.Button == OrbitButton && !altHeld && !LockOrbit)
                _isOrbiting = true;
            else if (!LockPan &&
                     (e.Button == PanButton ||
                      (e.Button == OrbitButton && altHeld && AltLeftPan)))
                _isPanning = true;
            else if (e.Button == BoxZoomButton && !LockBoxZoom)
                _isBoxZooming = true;

            _lastPos = e.Location;
            _control.Focus();

            if (_isPanning && _control is { Width: > 0, Height: > 0 })
            {
                CapturePanFrame(e.Location);
            }

            if (_isBoxZooming)
            {
                _boxZoomStart = e.Location;
                _boxZoomCurrent = e.Location;
                RaiseCameraChanged();
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isOrbiting && !_isPanning && !_isBoxZooming) return;

            int dx = e.X - _lastPos.X;
            int dy = e.Y - _lastPos.Y;
            _lastPos = e.Location;

            if (_isOrbiting)
            {
                float dAz = -dx * OrbitSensitivity;
                float dEl = dy * OrbitSensitivity;
                if (RotationCenter == RotationCenterMode.Centre)
                    _camera.OrbitAround(Vector3.Zero, dAz, dEl);
                else
                    _camera.Orbit(dAz, dEl);
            }
            else if (_isPanning && _panVpWidth > 0 && _panVpHeight > 0)
            {
                ApplyPan(e.Location);
            }
            else if (_isBoxZooming)
            {
                _boxZoomCurrent = e.Location;
            }

            RaiseCameraChanged();
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == OrbitButton) _isOrbiting = false;
            if (e.Button == PanButton) _isPanning = false;
            if (e.Button == OrbitButton && AltLeftPan) _isPanning = false;

            if (e.Button == BoxZoomButton && _isBoxZooming)
            {
                _isBoxZooming = false;
                var rect = Rectangle.FromLTRB(
                    Math.Min(_boxZoomStart.X, e.X),
                    Math.Min(_boxZoomStart.Y, e.Y),
                    Math.Max(_boxZoomStart.X, e.X),
                    Math.Max(_boxZoomStart.Y, e.Y));

                if (rect.Width >= BoxZoomMinPixels && rect.Height >= BoxZoomMinPixels)
                    FitToScreenRect(rect);

                RaiseCameraChanged();
            }
        }

        private void OnMouseWheel(object? sender, MouseEventArgs e)
        {
            if (LockZoom) return;

            // notches > 0 = scroll up = zoom in (factor < 1).
            float notches = e.Delta / 120f;
            float factor = 1f - notches * ZoomStep;

            if (_control is { Width: > 0, Height: > 0 })
            {
                var ray = _camera.ScreenToRay(e.Location, _control.Width, _control.Height);
                var viewDir = Vector3.Normalize(_camera.Target - _camera.Eye);
                float denom = Vector3.Dot(viewDir, ray.Direction);
                if (MathF.Abs(denom) > 1e-6f)
                {
                    float t = Vector3.Dot(viewDir, _camera.Target - ray.Origin) / denom;
                    var hit = ray.Origin + ray.Direction * t;
                    _camera.ZoomToward(hit, factor);
                }
                else
                {
                    _camera.Zoom(factor);
                }
            }
            else
            {
                _camera.Zoom(factor);
            }

            RaiseCameraChanged();
        }

        private void CapturePanFrame(PointF cursorAtStart)
        {
            _panVpWidth = _control.Width;
            _panVpHeight = _control.Height;

            var vp = _camera.ViewProjection(_panVpWidth, _panVpHeight);
            Matrix4x4.Invert(vp, out _panInvVPAtStart);

            var ray = Camera3D.ScreenToRayFromInvVP(_panInvVPAtStart, cursorAtStart, _panVpWidth, _panVpHeight);
            _panPlaneNormal = Vector3.Normalize(_camera.Target - _camera.Eye);
            _panAnchorWorld = ray.Origin + ray.Direction * _camera.Distance;
            _panPlaneDist = Vector3.Dot(_panPlaneNormal, _panAnchorWorld);
            _panTargetAtStart = _camera.Target;
        }

        private void ApplyPan(PointF cursor)
        {
            // Intersect the (drag-start frame) cursor ray with the fixed focal
            // plane: delta = anchor - hit, applied to the start Target.
            // https://en.wikipedia.org/wiki/Line%E2%80%93plane_intersection
            var ray = Camera3D.ScreenToRayFromInvVP(_panInvVPAtStart, cursor, _panVpWidth, _panVpHeight);
            float denom = Vector3.Dot(_panPlaneNormal, ray.Direction);
            if (MathF.Abs(denom) <= 1e-6f) return;

            float t = (_panPlaneDist - Vector3.Dot(_panPlaneNormal, ray.Origin)) / denom;
            var hit = ray.Origin + ray.Direction * t;
            var delta = _panAnchorWorld - hit;
            _camera.Target = _panTargetAtStart + delta;
        }

        /// <summary>
        /// Fits the camera so <paramref name="rect"/> fills the viewport.
        /// Visible scale at the focal plane is 2 * Distance * tan(FOV / 2), so
        /// scaling Distance by the rect's fractional size gives an exact fit.
        /// </summary>
        private void FitToScreenRect(Rectangle rect)
        {
            int vw = _control.Width;
            int vh = _control.Height;
            if (vw <= 0 || vh <= 0) return;

            var centre = new PointF(rect.Left + rect.Width / 2f,
                                    rect.Top + rect.Height / 2f);
            var ray = _camera.ScreenToRay(centre, vw, vh);
            var viewDir = Vector3.Normalize(_camera.Target - _camera.Eye);
            float denom = Vector3.Dot(viewDir, ray.Direction);
            if (MathF.Abs(denom) < 1e-6f) return;

            float t = Vector3.Dot(viewDir, _camera.Target - ray.Origin) / denom;
            _camera.Target = ray.Origin + ray.Direction * t;

            float fx = (float)rect.Width / vw;
            float fy = (float)rect.Height / vh;
            _camera.Zoom(MathF.Max(fx, fy));
        }

        /// <summary>
        /// Handles Numpad and Home view shortcuts; returns true when the key is consumed.
        /// </summary>
        public bool HandleKeyDown(KeyEventArgs e)
        {
            StandardView? view = e.KeyCode switch
            {
                Keys.NumPad1 when !e.Control => StandardView.Front,
                Keys.NumPad1 when e.Control => StandardView.Back,
                Keys.NumPad3 when !e.Control => StandardView.Right,
                Keys.NumPad3 when e.Control => StandardView.Left,
                Keys.NumPad7 when !e.Control => StandardView.Top,
                Keys.NumPad7 when e.Control => StandardView.Bottom,
                Keys.NumPad5 => StandardView.Isometric,
                _ => null
            };

            if (view is not null)
            {
                _camera.SetView(view.Value);
                RaiseCameraChanged();
                return true;
            }

            if (e.KeyCode == Keys.Home)
            {
                _camera.Reset();
                RaiseCameraChanged();
                return true;
            }

            return false;
        }

        private void RaiseCameraChanged() => CameraChanged?.Invoke(this, EventArgs.Empty);
    }
}
