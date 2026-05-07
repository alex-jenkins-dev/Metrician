// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Nodes.Geometry
{
    public sealed class IntrinsicPointNodeTemplate : INodeTemplate
    {
        public string Title => "Intrinsic Point";
        public string Vendor => "Metrician";
        public string Description =>
            "Reduces any wired value to a point. Accepts a PointSpec directly, or any " +
            "type for which a converter to PointSpec is registered (e.g. a circle's centre). " +
            "Outputs the resulting PointSpec for downstream rendering. " +
            "This is distinct from a constructor node that would declare a point into existence.";

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
