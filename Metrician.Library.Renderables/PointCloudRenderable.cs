// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing.Imaging;
using System.Numerics;
using Metrician.Library.Renderables;
using Metrician.Library.Rendering;

namespace Metrician.Library.Renderables
{
    /// <summary>
    /// Point cloud sized for tens of thousands of points. Small points (&lt;= 2 px)
    /// take a LockBits fast path that writes pixels directly into the back buffer.
    /// </summary>
    public sealed class PointCloudRenderable : IRenderable
    {
        private Vector3[] _points;
        private Color[]? _colors;
        private PointF[] _workspace;

        public Color PointColour { get; set; } = Color.LimeGreen;
        public float PointSize { get; set; } = 2f;
        public bool IsVisible { get; set; } = true;

        private SolidBrush _uniformBrush;
        private Color _lastUniformColor;

        public PointCloudRenderable(Vector3[] points, Color[]? colors = null)
        {
            _points = points ?? throw new ArgumentNullException(nameof(points));
            _colors = colors;
            _workspace = new PointF[points.Length];
            _uniformBrush = new SolidBrush(PointColour);
            _lastUniformColor = PointColour;

            if (colors != null && colors.Length != points.Length)
                throw new ArgumentException("colors length must match points length");

            RecalcBounds();
        }

        public BoundingBox3D? Bounds { get; private set; }

        private void RecalcBounds()
        {
            if (_points.Length == 0) { Bounds = null; return; }
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            foreach (var p in _points)
            {
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Bounds = new BoundingBox3D(min, max);
        }

        /// <summary>Replace point data, e.g. for streaming updates.</summary>
        public void UpdatePoints(Vector3[] points, Color[]? colors = null)
        {
            _points = points;
            _colors = colors;
            if (_workspace.Length < points.Length)
                _workspace = new PointF[points.Length];
            RecalcBounds();
        }

        public void Render(RenderContext ctx)
        {
            if (_points.Length == 0) return;

            if (_lastUniformColor != PointColour)
            {
                _uniformBrush.Dispose();
                _uniformBrush = new SolidBrush(PointColour);
                _lastUniformColor = PointColour;
            }

            if (PointSize <= 2f && ctx.BackBuffer is { } backBuffer)
            {
                RenderViaLockBits(ctx, backBuffer);
                return;
            }

            float half = PointSize * 0.5f;
            var g = ctx.Graphics;

            if (_colors == null)
            {
                int count = 0;
                for (int i = 0; i < _points.Length; i++)
                {
                    var screen = ctx.Project(_points[i]);
                    if (!ctx.IsVisible(screen)) continue;
                    _workspace[count++] = screen;
                }

                for (int i = 0; i < count; i++)
                    g.FillRectangle(_uniformBrush,
                        _workspace[i].X - half, _workspace[i].Y - half,
                        PointSize, PointSize);
            }
            else
            {
                using var brush = new SolidBrush(Color.White);
                for (int i = 0; i < _points.Length; i++)
                {
                    var screen = ctx.Project(_points[i]);
                    if (!ctx.IsVisible(screen)) continue;
                    brush.Color = _colors[i];
                    g.FillRectangle(brush,
                        screen.X - half, screen.Y - half,
                        PointSize, PointSize);
                }
            }
        }

        /// <summary>
        /// Pixel-direct rendering for small points. Back buffer is premultiplied
        /// ARGB; alpha = 255 colours let us write the raw bytes without conversion.
        /// </summary>
        private unsafe void RenderViaLockBits(RenderContext ctx, Bitmap backBuffer)
        {
            int half = PointSize > 1f ? 1 : 0;

            int width = backBuffer.Width;
            int height = backBuffer.Height;

            var lockRect = new Rectangle(0, 0, width, height);
            var data = backBuffer.LockBits(
                lockRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            try
            {
                byte* basePtr = (byte*)data.Scan0;
                int stride = data.Stride;

                bool perColor = _colors != null;
                Color uniform = PointColour;

                for (int i = 0; i < _points.Length; i++)
                {
                    var screen = ctx.Project(_points[i]);

                    int cx = (int)screen.X;
                    int cy = (int)screen.Y;

                    Color c = perColor ? _colors![i] : uniform;
                    byte b = c.B, g = c.G, r = c.R, a = c.A;

                    int x0 = cx - half, x1 = cx + half;
                    int y0 = cy - half, y1 = cy + half;
                    if (x0 < 0) x0 = 0;
                    if (x1 >= width) x1 = width - 1;
                    if (y0 < 0) y0 = 0;
                    if (y1 >= height) y1 = height - 1;
                    if (x0 > x1 || y0 > y1) continue;

                    for (int y = y0; y <= y1; y++)
                    {
                        byte* row = basePtr + y * stride + x0 * 4;
                        for (int x = x0; x <= x1; x++)
                        {
                            row[0] = b;
                            row[1] = g;
                            row[2] = r;
                            row[3] = a;
                            row += 4;
                        }
                    }
                }
            }
            finally
            {
                backBuffer.UnlockBits(data);
            }
        }
    }
}
