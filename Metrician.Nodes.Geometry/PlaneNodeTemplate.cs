// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class PlaneNodeTemplate : INodeTemplate
    {
        public string Title => "Plane";
        public string Vendor => "Geometry";
        public string Description =>
            "A rectangular plane patch defined by a centre point, a normal direction, " +
            "and a width and height for the wireframe rectangle.";

        public void Configure(INodeAuthor a)
        {
            a.Pins.AddOutput<PlaneSpec>("plane");

            a.Properties.Define("Center", Vector3.Zero);
            a.Properties.Define("Normal", Vector3.UnitZ);
            a.Properties.Define("Width", 2f);
            a.Properties.Define("Height", 2f);
            a.Properties.Define("Colour", Color.Khaki);

            a.Behaviour.OnEvaluate(ctx =>
            {
                var normal = a.Properties.Get<Vector3>("Normal");
                if (normal.LengthSquared() < 1e-12f)
                {
                    ctx.Error("normal must be non-zero");
                    return;
                }
                ctx.Write("plane", new PlaneSpec(
                    a.Properties.Get<Vector3>("Center"),
                    Vector3.Normalize(normal),
                    a.Properties.Get<float>("Width"),
                    a.Properties.Get<float>("Height"),
                    a.Properties.Get<Color>("Colour")));
            });

            a.Tags.Add("geometry");
            a.Tags.Add("primitive");
        }
    }
}
