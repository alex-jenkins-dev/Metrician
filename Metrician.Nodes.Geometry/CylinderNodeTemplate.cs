// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class CylinderNodeTemplate : INodeTemplate
    {
        public string Title => "Cylinder";
        public string Vendor => "Geometry";
        public string Description =>
            "A finite cylinder defined by its centre, axis direction, radius, and height. " +
            "The axis is normalised; height extends symmetrically along it.";

        public void Configure(INodeAuthor a)
        {
            a.Pins.AddOutput<CylinderSpec>("cylinder");

            a.Properties.Define("Center", Vector3.Zero);
            a.Properties.Define("Axis", Vector3.UnitZ);
            a.Properties.Define("Radius", 1f);
            a.Properties.Define("Height", 2f);
            a.Properties.Define("Colour", Color.LightSteelBlue);

            a.Behaviour.OnEvaluate(ctx =>
            {
                var axis = a.Properties.Get<Vector3>("Axis");
                if (axis.LengthSquared() < 1e-12f)
                {
                    ctx.Error("axis must be non-zero");
                    return;
                }
                ctx.Write("cylinder", new CylinderSpec(
                    a.Properties.Get<Vector3>("Center"),
                    Vector3.Normalize(axis),
                    a.Properties.Get<float>("Radius"),
                    a.Properties.Get<float>("Height"),
                    a.Properties.Get<Color>("Colour")));
            });

            a.Tags.Add("geometry");
            a.Tags.Add("primitive");
        }
    }
}
