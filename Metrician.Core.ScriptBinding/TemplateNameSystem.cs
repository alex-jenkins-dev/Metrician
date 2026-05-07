// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Core.ScriptBinding
{
    public sealed class TemplateNameSystem : ITemplateNameSystem
    {
        private readonly Dictionary<NodeId, string> _names = new();

        public TemplateNameSystem(INodeRegistry nodes)
        {
            if (nodes is null) throw new ArgumentNullException(nameof(nodes));
            nodes.Removed += (_, id) => _names.Remove(id);
        }

        public void Set(NodeId id, string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name must be non-empty.", nameof(typeName));
            _names[id] = typeName;
        }

        public string? Get(NodeId id) =>
            _names.TryGetValue(id, out var name) ? name : null;

        public void Clear(NodeId id) => _names.Remove(id);
    }
}
