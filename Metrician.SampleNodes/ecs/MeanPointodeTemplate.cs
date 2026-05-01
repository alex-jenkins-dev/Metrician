// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.SampleNodes.Ecs
{
    public sealed class MeanPointodeTemplate : INodeTemplate
    {
        public string Title => "Mean Point";
        public string Vendor => "Samples";
        public string Description =>
            "Centroid of an arbitrary number of probed points; reactive pin set keeps one trailing spare.";

        public void Configure(INodeAuthor a)
        {
            a.Pins.AddOutput<Vector3>("centroid");
            VariadicInputs.Configure<Vector3>(a, "point ");

            a.Behaviour.OnEvaluate(ctx =>
            {
                int n = 0;
                Vector3 sum = Vector3.Zero;
                foreach (var pin in a.Pins.Inputs)
                {
                    if (!a.Pins.IsConnected(pin.Id)) continue;
                    sum += ctx.Read<Vector3>(pin.Id.Name);
                    n++;
                }
                ctx.Write("centroid", n > 0 ? sum / n : Vector3.Zero);
            });

            a.Tags.Add("statistics");
            a.Tags.Add("variadic");
        }
    }
}
