// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class DeclarePlaneNodeTemplate : INodeTemplate
    {
        public string Title => "Declare Plane";
        public string Vendor => "Metrician";
        public string Description =>
            "A rectangular plane patch defined by a centre point, a normal direction, " +
            "and a width and height for the wireframe rectangle. Declared into existence.";

        public void Configure(INodeAuthor a)
        {
            a.Pins.AddOutput<PlaneSpec>("plane");

            a.Properties.Define("Center", Vector3.Zero);
            a.Properties.Define("Normal", Vector3.UnitZ);
            a.Properties.Define("Width", 2f);
            a.Properties.Define("Height", 2f);

            a.Properties.Constrain("Normal", v =>
                v is Vector3 vec && vec.LengthSquared() >= 1e-12f
                    ? null : "must be non-zero");
            a.Properties.Constrain("Width", v =>
                v is float w && w > 0f
                    ? null : "must be greater than zero");
            a.Properties.Constrain("Height", v =>
                v is float h && h > 0f
                    ? null : "must be greater than zero");

            a.Behaviour.OnEvaluate(ctx =>
            {
                ctx.Write("plane", new PlaneSpec(
                    a.Properties.Get<Vector3>("Center"),
                    Vector3.Normalize(a.Properties.Get<Vector3>("Normal")),
                    a.Properties.Get<float>("Width"),
                    a.Properties.Get<float>("Height")));
            });

            a.Tags.Add("geometry");
            a.Tags.Add("primitive");
        }
    }
}
