// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.SampleNodes.Ecs
{
    public sealed class PointDistanceNodeTemplate : INodeTemplate
    {
        public string Title => "Point Distance";
        public string Vendor => "Samples";
        public string Description =>
            "Cartesian distance between two points; both inputs required.";

        public void Configure(INodeAuthor a)
        {
            var pa = a.Pins.AddInput<Vector3>("feature a");
            var pb = a.Pins.AddInput<Vector3>("feature b");
            a.Pins.AddOutput<float>("distance");

            a.Pins.Constrain(pa.Id, c => c ? null : "feature a must be wired");
            a.Pins.Constrain(pb.Id, c => c ? null : "feature b must be wired");

            var feature = new PinColour(220, 180, 80);
            a.Pins.Colour(pa.Id, feature);
            a.Pins.Colour(pb.Id, feature);
            a.Pins.Group(pa.Id, "features");
            a.Pins.Group(pb.Id, "features");

            a.Behaviour.OnEvaluate(ctx =>
            {
                var av = ctx.Read<Vector3>("feature a");
                var bv = ctx.Read<Vector3>("feature b");
                ctx.Write("distance", Vector3.Distance(av, bv));
            });

            a.Tags.Add("measurement");
            a.Tags.Add("dimension");
        }
    }
}
