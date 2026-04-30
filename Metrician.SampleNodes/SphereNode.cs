// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core;

namespace Metrician.SampleNodes
{
    public sealed class SphereNode : NodeBase
    {
        private readonly NodeInput<Vector3> _center;
        private readonly NodeOutput<SphereSpec> _out;

        public float Radius { get; set; } = 1f;
        public Color Colour { get; set; } = Color.White;

        public SphereNode()
        {
            Title = "Sphere";
            Vendor = "Samples";
            _center = AddInput<Vector3>("Center");
            _out = AddOutput<SphereSpec>("Sphere");
        }

        public override void Evaluate() =>
            _out.CurrentValue = new SphereSpec(_center.CurrentValue, Radius, Colour);
    }
}
