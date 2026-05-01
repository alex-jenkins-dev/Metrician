// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;

namespace Metrician.Library.Rendering
{
    public readonly struct BoundingBox3D
    {
        public Vector3 Min { get; }
        public Vector3 Max { get; }

        public BoundingBox3D(Vector3 min, Vector3 max) { Min = min; Max = max; }

        public Vector3 Center => (Min + Max) * 0.5f;

        /// <summary>
        /// Conservative test that the AABB intersects the visible volume of <paramref name="vp"/>.
        /// Tests against NDC bounds directly (X, Y in [-1, +1], Z in [0, 1]); valid because the
        /// orthographic projection preserves W = 1. Uses the same outcode idea as Cohen-Sutherland
        /// line clipping: if all eight corners lie outside the same half-space, the box is culled.
        /// https://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm
        /// </summary>
        public bool IsInViewVolume(Matrix4x4 vp)
        {
            Span<Vector3> corners = stackalloc Vector3[8]
            {
                new(Min.X, Min.Y, Min.Z), new(Max.X, Min.Y, Min.Z),
                new(Min.X, Max.Y, Min.Z), new(Max.X, Max.Y, Min.Z),
                new(Min.X, Min.Y, Max.Z), new(Max.X, Min.Y, Max.Z),
                new(Min.X, Max.Y, Max.Z), new(Max.X, Max.Y, Max.Z),
            };

            int outLeft = 0, outRight = 0, outTop = 0, outBottom = 0,
                outNear = 0, outFar = 0;

            foreach (var c in corners)
            {
                var clip = Vector4.Transform(new Vector4(c, 1f), vp);
                if (clip.X < -1f) outLeft++;
                if (clip.X >  1f) outRight++;
                if (clip.Y < -1f) outBottom++;
                if (clip.Y >  1f) outTop++;
                if (clip.Z <  0f) outNear++;
                if (clip.Z >  1f) outFar++;
            }

            return !(outLeft == 8 || outRight == 8 ||
                     outTop == 8 || outBottom == 8 ||
                     outNear == 8 || outFar == 8);
        }
    }
}
