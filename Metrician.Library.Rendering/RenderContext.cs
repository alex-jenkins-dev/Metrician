// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;

namespace Metrician.Library.Rendering
{
    public sealed class RenderContext
    {
        public Graphics Graphics { get; }

        public Camera3D Camera { get; }

        public int Width { get; }

        public int Height { get; }

        public Matrix4x4 ViewProjection { get; }

        /// <summary>
        /// Bitmap backing <see cref="Graphics"/>, or null.
        /// Available for LockBits direct-pixel writes.
        /// </summary>
        public Bitmap? BackBuffer { get; }

        public RenderContext(
            Graphics g, Camera3D camera, int width, int height,
            Bitmap? backBuffer = null)
        {
            Graphics = g;
            Camera = camera;
            Width = width;
            Height = height;
            ViewProjection = camera.ViewProjection(width, height);
            BackBuffer = backBuffer;
        }

        /// <summary>
        /// World-to-screen projection.
        /// Result may fall outside the viewport; cull with <see cref="IsVisible"/>.
        /// </summary>
        public PointF Project(Vector3 world)
        {
            Vector4 clip = Vector4.Transform(new Vector4(world, 1f), ViewProjection);
            return new PointF(
                ( clip.X + 1f) * 0.5f * Width,
                (-clip.Y + 1f) * 0.5f * Height);
        }

        public bool IsVisible(PointF screen, float margin = 4f) =>
            screen.X >= -margin && screen.X <= Width + margin &&
            screen.Y >= -margin && screen.Y <= Height + margin;
    }
}
