// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core;

namespace Metrician.SampleNodes
{
    public sealed class PointNode : NodeBase
    {
        private readonly NodeOutput<Vector3> _out;

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public PointNode()
        {
            Title = "Point";
            Vendor = "Samples";
            _out = AddOutput<Vector3>("Position");
        }

        public override void Evaluate() => _out.CurrentValue = new Vector3(X, Y, Z);
    }
}
