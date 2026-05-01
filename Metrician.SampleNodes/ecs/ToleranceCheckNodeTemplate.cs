// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.SampleNodes.Ecs
{
    public sealed class ToleranceCheckNodeTemplate : INodeTemplate
    {
        public string Title => "Tolerance Check";
        public string Vendor => "Samples";
        public string Description =>
            "Compares a measured value against a nominal with bilateral tolerance; " +
            "outputs signed deviation and pass/fail.";

        public void Configure(INodeAuthor a)
        {
            var measured = a.Pins.AddInput<float>("measured");
            a.Pins.AddOutput<float>("deviation");
            a.Pins.AddOutput<bool>("in tolerance");

            a.Pins.Constrain(measured.Id, c => c ? null : "measured must be wired");
            a.Pins.Group(measured.Id, "input");

            a.Properties.Define("Nominal", 0f);
            a.Properties.Define("UpperTolerance", 0.05f);
            a.Properties.Define("LowerTolerance", 0.05f);

            a.Properties.Constrain("UpperTolerance", v => (float)v! >= 0 ? null : "must be >= 0");
            a.Properties.Constrain("LowerTolerance", v => (float)v! >= 0 ? null : "must be >= 0");

            a.Validation.OnValidate(self =>
            {
                var u = self.Properties.Get<float>("UpperTolerance");
                var l = self.Properties.Get<float>("LowerTolerance");
                return u + l > 0
                    ? Array.Empty<string>()
                    : ["Tolerance band must be greater than zero."];
            });

            a.Behaviour.OnEvaluate(ctx =>
            {
                var m = ctx.Read<float>("measured");
                if (float.IsNaN(m) || float.IsInfinity(m))
                {
                    ctx.Error("measured value is not finite");
                    return;
                }

                var nominal = a.Properties.Get<float>("Nominal");
                var upper = a.Properties.Get<float>("UpperTolerance");
                var lower = a.Properties.Get<float>("LowerTolerance");

                var deviation = m - nominal;
                var ok = deviation <= upper && -deviation <= lower;

                ctx.Write("deviation", deviation);
                ctx.Write("in tolerance", ok);
            });

            a.Tags.Add("inspection");
            a.Tags.Add("tolerance");
        }
    }
}
