// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;

namespace Metrician.Nodes.Geometry
{
    public sealed record CircleSpec(
        Vector3 Center,
        Vector3 Normal,
        float Radius);
}
