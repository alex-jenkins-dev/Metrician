// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class CircleToPointConverter : IValueConverter<CircleSpec, PointSpec>
    {
        public PointSpec Convert(CircleSpec value) =>
            new(value.Center);
    }
}
