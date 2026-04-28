// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph.Contracts;
using Metrician.Renderable.Contracts;

namespace Metrician.Core
{
    public sealed class RenderNode : NodeBase, IVariadicInputs
    {
        private readonly IRenderableRegistry _registry;
        private readonly List<NodeInput<object>> _renderInputs = new();

        public IList<IRenderable> Output { get; } = new List<IRenderable>();

        public RenderNode(IRenderableRegistry registry)
        {
            Title = "Render";
            Vendor = "Metrician";
            _registry = registry;
            CompactInputs();
        }

        public void CompactInputs()
        {
            for (int i = _renderInputs.Count - 1; i >= 0; i--)
            {
                if (_renderInputs[i].Source == null)
                {
                    RemoveInput(_renderInputs[i]);
                    _renderInputs.RemoveAt(i);
                }
            }

            var spare = AddInput<object>("in");
            _renderInputs.Add(spare);
        }

        public override void Evaluate()
        {
            Output.Clear();
            foreach (var pin in _renderInputs)
            {
                var value = pin.Source?.Value;
                if (value is null) continue;
                if (_registry.TryCreate(value, out var renderable) && renderable is not null)
                    Output.Add(renderable);
            }
        }
    }
}
