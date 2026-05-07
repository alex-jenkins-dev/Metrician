// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;
using Metrician.Library.Renderables;

namespace Metrician.Library.Bridge
{
    public sealed class RenderNodeTemplate : INodeTemplate
    {
        private readonly IRenderableRegistry _registry;
        private readonly IRenderOptionsSystem _options;
        private readonly Action<NodeId, IReadOnlyList<IRenderable>> _publish;

        public string Title => "Render";
        public string Vendor => "Metrician";
        public string Description =>
            "Sink that converts wired values into renderables via a registry. " +
            "Per-input render options (colour, line width, …) live on each " +
            "input pin and are applied at evaluation time.";

        public RenderNodeTemplate(
            IRenderableRegistry registry,
            IRenderOptionsSystem options,
            Action<NodeId, IReadOnlyList<IRenderable>> publish)
        {
            _registry = registry;
            _options = options;
            _publish = publish;
        }

        public void Configure(INodeAuthor a)
        {
            VariadicInputs.Configure<object>(a, "in ");

            a.Behaviour.OnEvaluate(ctx =>
            {
                var output = new List<IRenderable>();
                foreach (var pin in a.Pins.Inputs)
                {
                    if (!a.Pins.IsConnected(pin.Id)) continue;
                    var value = ctx.Read<object>(pin.Id.Name);
                    if (value is null) continue;
                    var opts = _options.Get(pin.Id);
                    if (_registry.TryCreate(value, opts, out var r) && r is not null)
                        output.Add(r);
                }
                _publish(ctx.Self, output);
            });

            a.Tags.Add("sink");
            a.Tags.Add("render");
        }
    }
}
