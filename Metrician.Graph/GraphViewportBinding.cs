// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Renderable.Contracts;
using Metrician.Core;

namespace Metrician.Graph
{
    public sealed class GraphViewportBinding
    {
        private readonly NodeGraph _graph;
        private readonly IList<IRenderable> _sink;

        public GraphViewportBinding(NodeGraph graph, IList<IRenderable> sink)
        {
            _graph = graph;
            _sink = sink;
        }

        public void Refresh()
        {
            _graph.Evaluate();

            _sink.Clear();
            foreach (var node in _graph.Nodes)
            {
                if (node is RenderNode rn)
                {
                    foreach (var renderable in rn.Output)
                        _sink.Add(renderable);
                }
            }
        }
    }
}
