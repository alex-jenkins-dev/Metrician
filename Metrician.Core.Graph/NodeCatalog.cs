// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public sealed record NodeTypeInfo(
        string TypeName,
        string Title,
        string Vendor,
        string Description);

    public interface INodeCatalog
    {
        IReadOnlyList<NodeTypeInfo> All { get; }
        NodeTypeInfo Register(Func<INodeTemplate> factory);
        INodeTemplate Create(string typeName);
    }

    public sealed class NodeCatalog : INodeCatalog
    {
        private readonly Dictionary<string, Func<INodeTemplate>> _factories =
            new(StringComparer.Ordinal);
        private readonly List<NodeTypeInfo> _ordered = new();

        public IReadOnlyList<NodeTypeInfo> All => _ordered;

        public NodeTypeInfo Register(Func<INodeTemplate> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            var probe = factory();
            var info = new NodeTypeInfo(
                probe.GetType().Name,
                probe.Title,
                probe.Vendor,
                probe.Description);
            _factories[info.TypeName] = factory;
            _ordered.Add(info);
            return info;
        }

        public INodeTemplate Create(string typeName) =>
            _factories.TryGetValue(typeName, out var factory)
                ? factory()
                : throw new KeyNotFoundException($"No node type registered for '{typeName}'.");
    }
}
