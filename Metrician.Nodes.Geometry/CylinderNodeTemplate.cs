// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class CylinderNodeTemplate : INodeTemplate
    {
        public string Title => "Cylinder";
        public string Vendor => "Metrician";
        public string Description =>
            "A finite cylinder defined by its centre, axis direction, diameter, and height. " +
            "The axis is normalised; height extends symmetrically along it. " +
            "Diameter and height must be greater than zero.";

        public void Configure(INodeAuthor a)
        {
            a.Pins.AddOutput<CylinderSpec>("cylinder");

            a.Properties.Define("Center", Vector3.Zero);
            a.Properties.Define("Axis", Vector3.UnitZ);
            a.Properties.Define("Diameter", 2f);
            a.Properties.Define("Height", 2f);
            a.Properties.Define("Colour", Color.LightSteelBlue);

            a.Properties.Constrain("Axis", v =>
                v is Vector3 vec && vec.LengthSquared() >= 1e-12f
                    ? null : "must be non-zero");
            a.Properties.Constrain("Diameter", v =>
                v is float d && d > 0f
                    ? null : "must be greater than zero");
            a.Properties.Constrain("Height", v =>
                v is float h && h > 0f
                    ? null : "must be greater than zero");

            a.Behaviour.OnEvaluate(ctx =>
            {
                ctx.Write("cylinder", new CylinderSpec(
                    a.Properties.Get<Vector3>("Center"),
                    Vector3.Normalize(a.Properties.Get<Vector3>("Axis")),
                    a.Properties.Get<float>("Diameter") * 0.5f,
                    a.Properties.Get<float>("Height"),
                    a.Properties.Get<Color>("Colour")));
            });

            a.Tags.Add("geometry");
            a.Tags.Add("primitive");
        }
    }
}
