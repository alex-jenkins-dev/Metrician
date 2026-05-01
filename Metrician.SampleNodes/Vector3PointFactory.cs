// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Renderable.Contracts;
using Metrician.Renderables;

namespace Metrician.SampleNodes
{
    public sealed class Vector3PointFactory : IRenderableFactory<Vector3>
    {
        public IRenderable Create(Vector3 value) =>
            new LabelledPointRenderable(value, "")
            {
                Colour = Color.LimeGreen,
                DotRadius = 4f,
            };
    }
}
