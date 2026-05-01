// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.SampleNodes.Ecs
{
    public sealed class PointStreamNodeTemplate : INodeTemplate
    {
        public string Title => "Point Stream";
        public string Vendor => "Samples";
        public string Description =>
            "Simulated emission of noisy point samples around a nominal location at a fixed rate.";

        public void Configure(INodeAuthor a)
        {
            a.Pins.AddOutput<Vector3>("sample");

            a.Properties.Define("TargetX", 0f);
            a.Properties.Define("TargetY", 0f);
            a.Properties.Define("TargetZ", 0f);
            a.Properties.Define("SampleHz", 30f);
            a.Properties.Define("NoiseStdDev", 0.001f);

            a.Properties.Constrain("SampleHz", v => (float)v! > 0
                ? null
                : "must be > 0");
            a.Properties.Constrain("NoiseStdDev", v => (float)v! >= 0
                ? null
                : "must be >= 0");

            var rng = new Random();
            Vector3 latest = default;

            a.Behaviour.OnEvaluate(ctx =>
            {
                if (!float.IsFinite(latest.X) || !float.IsFinite(latest.Y) || !float.IsFinite(latest.Z))
                {
                    ctx.Error("non-finite sample produced");
                    return;
                }
                ctx.Write("sample", latest);
            });

            a.Behaviour.OnDynamicUpdate(handle =>
            {
                int periodMs = (int)Math.Max(1, 1000f / a.Properties.Get<float>("SampleHz"));
                return new System.Threading.Timer(_ =>
                {
                    var sd = a.Properties.Get<float>("NoiseStdDev");
                    latest = new Vector3(
                        a.Properties.Get<float>("TargetX") + (float)((rng.NextDouble() - 0.5) * 2 * sd),
                        a.Properties.Get<float>("TargetY") + (float)((rng.NextDouble() - 0.5) * 2 * sd),
                        a.Properties.Get<float>("TargetZ") + (float)((rng.NextDouble() - 0.5) * 2 * sd));
                    handle.RequestRefresh();
                }, state: null, dueTime: 0, period: periodMs);
            });

            a.Tags.Add("source");
            a.Tags.Add("dynamic");
            a.Tags.Add("probe");
        }
    }
}
