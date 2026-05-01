// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class CircleNodeTemplate : INodeTemplate
    {
        public string Title => "Circle";
        public string Vendor => "Geometry";
        public string Description =>
            "A circle defined by its centre, the normal of the plane it lies in, and a radius.";

        public void Configure(INodeAuthor a)
        {
            a.Pins.AddOutput<CircleSpec>("circle");

            a.Properties.Define("Center", Vector3.Zero);
            a.Properties.Define("Normal", Vector3.UnitZ);
            a.Properties.Define("Radius", 1f);
            a.Properties.Define("Colour", Color.LimeGreen);

            a.Behaviour.OnEvaluate(ctx =>
            {
                var normal = a.Properties.Get<Vector3>("Normal");
                if (normal.LengthSquared() < 1e-12f)
                {
                    ctx.Error("normal must be non-zero");
                    return;
                }
                ctx.Write("circle", new CircleSpec(
                    a.Properties.Get<Vector3>("Center"),
                    Vector3.Normalize(normal),
                    a.Properties.Get<float>("Radius"),
                    a.Properties.Get<Color>("Colour")));
            });

            a.Tags.Add("geometry");
            a.Tags.Add("primitive");
        }
    }
}
