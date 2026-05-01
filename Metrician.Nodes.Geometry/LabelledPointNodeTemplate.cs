// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class LabelledPointNodeTemplate : INodeTemplate
    {
        public string Title => "Labelled Point";
        public string Vendor => "Metrician";
        public string Description =>
            "Forwards a point onward as a PointSpec so a render node can draw it as " +
            "a dot annotated with its world-space coordinates. Accepts a CircleSpec " +
            "via the built-in CircleSpec → PointSpec converter.";

        public void Configure(INodeAuthor a)
        {
            var inputPin = a.Pins.AddInput<PointSpec>("point");
            a.Pins.AddOutput<PointSpec>("point");
            a.Pins.Constrain(inputPin.Id, c => c ? null : "must be wired");

            a.Behaviour.OnEvaluate(ctx =>
            {
                var point = ctx.Read<PointSpec>("point");
                if (point is null) return;
                ctx.Write("point", point);
            });

            a.Tags.Add("geometry");
        }
    }
}
