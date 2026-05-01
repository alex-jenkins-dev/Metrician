// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.SampleNodes.Ecs
{
    public sealed class NominalPointNodeTemplate : INodeTemplate
    {
        public string Title => "Nominal Point";
        public string Vendor => "Samples";
        public string Description =>
            "Expected location.";

        public void Configure(INodeAuthor a)
        {
            a.Pins.AddOutput<Vector3>("position");

            a.Properties.Define("X", 0f);
            a.Properties.Define("Y", 0f);
            a.Properties.Define("Z", 0f);

            a.Behaviour.OnEvaluate(ctx => ctx.Write("position", new Vector3(
                a.Properties.Get<float>("X"),
                a.Properties.Get<float>("Y"),
                a.Properties.Get<float>("Z"))));

            a.Tags.Add("nominal");
            a.Tags.Add("datum-candidate");
        }
    }
}
