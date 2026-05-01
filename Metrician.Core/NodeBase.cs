// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.ComponentModel;
using Metrician.Contracts.Graph;

namespace Metrician.Core
{
    public abstract class NodeBase : INode, INodeLayout
    {
        [Browsable(false)]
        public string Title { get; protected set; } = "Node";

        [Browsable(false)]
        public string Vendor { get; protected set; } = "Unknown";

        /// <summary>Layout hint for the graph editor; not used by evaluation.</summary>
        [Browsable(false)]
        public PointF Position { get; set; }

        private readonly List<INodeInput> _inputs = new();
        private readonly List<INodeOutput> _outputs = new();

        [Browsable(false)]
        public IReadOnlyList<INodeInput> Inputs => _inputs;

        [Browsable(false)]
        public IReadOnlyList<INodeOutput> Outputs => _outputs;

        protected NodeInput<T> AddInput<T>(string name)
        {
            var pin = new NodeInput<T>(this, name);
            _inputs.Add(pin);
            return pin;
        }

        protected NodeOutput<T> AddOutput<T>(string name)
        {
            var pin = new NodeOutput<T>(this, name);
            _outputs.Add(pin);
            return pin;
        }

        protected bool RemoveInput(INodeInput pin) => _inputs.Remove(pin);
        protected bool RemoveOutput(INodeOutput pin) => _outputs.Remove(pin);

        public abstract void Evaluate();
    }
}
