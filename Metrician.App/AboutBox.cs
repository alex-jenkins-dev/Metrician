// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using System.Reflection;
using Metrician.Library.Rendering;
using Metrician.Library.Renderables;
using Metrician.Library.Viewport;

namespace Metrician.App
{
    public sealed class AboutBox : Form
    {
        private const int N = 32;

        private readonly Viewport3DControl _viewport;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Vector3[] _points = new Vector3[N];
        private readonly Color[] _pointColors = new Color[N];
        private readonly PointCloudRenderable _pointCloud;
        private readonly LineSetRenderable _lineSet;
        private readonly SurfaceNormalRenderable[] _normals = new SurfaceNormalRenderable[N];
        private float _phase;

        public AboutBox()
        {
            Text = "About Metrician";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 35);

            AutoScaleMode = AutoScaleMode.Dpi;

            const int contentWidth = 560;
            const int viewportHeight = 240;
            const int bottomPanelHeight = 56;

            _viewport = new Viewport3DControl
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                ShowGrid = false,
                ShowAxisGizmo = false,
                ViewportBackground = Color.FromArgb(20, 20, 28),
            };
            _viewport.MouseInteraction.LockOrbit = true;
            _viewport.MouseInteraction.LockPan = true;
            _viewport.MouseInteraction.LockZoom = true;
            _viewport.MouseInteraction.LockBoxZoom = true;
            _viewport.Camera.SetView(StandardView.Isometric);
            _viewport.Camera.FieldOfView = 32f;
            _viewport.Camera.Distance = 13f;
            _viewport.Camera.Target = Vector3.Zero;

            for (int i = 0; i < N; i++)
            {
                float u = i / (float)(N - 1);
                _pointColors[i] = Color.FromArgb(
                    (int)Lerp(80, 130, u),
                    (int)Lerp(200, 230, u),
                    (int)Lerp(255, 200, u));
            }

            _pointCloud = new PointCloudRenderable(new Vector3[N], (Color[])_pointColors.Clone())
            {
                PointSize = 4f,
            };
            _lineSet = new LineSetRenderable
            {
                LineColour = Color.FromArgb(160, 180, 220, 255),
                LineWidth = 1.4f,
            };
            for (int i = 0; i < N; i++)
            {
                _normals[i] = new SurfaceNormalRenderable(
                    Vector3.Zero, Vector3.UnitZ, length: 0.55f,
                    color: Color.FromArgb(255, 200, 100));
            }

            UpdateWaveGeometry();

            _viewport.Renderables.Add(_pointCloud);
            _viewport.Renderables.Add(_lineSet);
            foreach (var n in _normals) _viewport.Renderables.Add(n);

            var label = new Label
            {
                AutoSize = true,
                MinimumSize = new Size(contentWidth, 0),
                MaximumSize = new Size(contentWidth, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = Padding.Empty,
                Padding = new Padding(20),
                Text =
                    $"Metrician (v{GetVersion()})\r\n" +
                    "\r\n" +
                    "The greatest metrology diagnostics app in the world, maybe\r\n" +
                    "\r\n" +
                    "by Alex Jenkins",
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(30, 30, 35),
                Font = new Font("Segoe UI", 9.5f),
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, contentWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, viewportHeight));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, bottomPanelHeight));
            layout.Controls.Add(_viewport, 0, 0);
            layout.Controls.Add(label, 0, 1);

            Controls.Add(layout);

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            _timer = new System.Windows.Forms.Timer { Interval = 33 };
            _timer.Tick += (_, _) =>
            {
                _phase += 0.08f;
                UpdateWaveGeometry();
                _viewport.Invalidate();
            };
            _timer.Start();

            FormClosed += (_, _) => _timer.Dispose();
        }

        private void UpdateWaveGeometry()
        {
            const float halfExtent = 4f;
            const float frequency = 0.8f;
            const float amplitude = 1.2f;

            for (int i = 0; i < N; i++)
            {
                float u = i / (float)(N - 1);
                float x = (u * 2f - 1f) * halfExtent;
                float z = MathF.Sin(x * frequency + _phase) * amplitude;
                _points[i] = new Vector3(x, 0f, z);

                // Normal = 90 deg rotation of the tangent (1, 0, dz/dx) in XZ.
                float dzdx = MathF.Cos(x * frequency + _phase) * amplitude * frequency;
                _normals[i].Origin = _points[i];
                _normals[i].Direction = Vector3.Normalize(new Vector3(-dzdx, 0f, 1f));
            }

            _pointCloud.UpdatePoints(_points, _pointColors);

            _lineSet.Clear();
            for (int i = 0; i < N - 1; i++)
                _lineSet.Add(_points[i], _points[i + 1]);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        // InformationalVersion carries the Directory.Build.props Version, with
        // a "+commit" suffix appended by SourceLink that we strip for display.
        private static string GetVersion()
        {
            var asm = typeof(AboutBox).Assembly;
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(info))
            {
                int plus = info.IndexOf('+');
                return plus < 0 ? info : info[..plus];
            }
            return asm.GetName().Version?.ToString(3) ?? "?";
        }
    }
}
