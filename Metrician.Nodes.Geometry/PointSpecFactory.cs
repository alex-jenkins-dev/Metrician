// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Globalization;
using Metrician.Contracts.Renderables;
using Metrician.Renderables;

namespace Metrician.Nodes.Geometry
{
    public sealed class PointSpecFactory : IRenderableFactory<PointSpec>
    {
        public IRenderable Create(PointSpec value) =>
            new LabelledPointRenderable(value.Position, FormatLabel(value.Position))
            {
                Colour = value.Colour,
                DotRadius = 4f,
            };

        private static string FormatLabel(System.Numerics.Vector3 p)
        {
            var ci = CultureInfo.InvariantCulture;
            return $"({p.X.ToString("0.###", ci)}, {p.Y.ToString("0.###", ci)}, {p.Z.ToString("0.###", ci)})";
        }
    }
}
