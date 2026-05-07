// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;

namespace Metrician.Library.Rendering
{
    /// <summary>
    /// World-space ray with a unit direction; used for picking.
    /// </summary>
    public readonly struct Ray3D
    {
        public Vector3 Origin { get; }
        public Vector3 Direction { get; }

        public Ray3D(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }
    }
}
