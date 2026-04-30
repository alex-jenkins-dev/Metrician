// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core;
using Metrician.Graph.Contracts;

namespace Metrician.SampleNodes
{
    public sealed class OscillatorNode : NodeBase, IDynamicNode, IDisposable
    {
        private readonly NodeOutput<float> _out;
        private readonly System.Threading.Timer _timer;
        private readonly DateTime _start = DateTime.UtcNow;

        public float Frequency { get; set; } = 0.5f;
        public float Amplitude { get; set; } = 2f;

        public event EventHandler? OutputChanged;

        public OscillatorNode()
        {
            Title = "Oscillator";
            Vendor = "Samples";
            _out = AddOutput<float>("Value");
            _timer = new System.Threading.Timer(
                _ => OutputChanged?.Invoke(this, EventArgs.Empty),
                state: null, dueTime: 0, period: 33);
        }

        public override void Evaluate()
        {
            float t = (float)(DateTime.UtcNow - _start).TotalSeconds;
            _out.CurrentValue = MathF.Sin(t * 2f * MathF.PI * Frequency) * Amplitude;
        }

        public void Dispose() => _timer.Dispose();
    }
}
