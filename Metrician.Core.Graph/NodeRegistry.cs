// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface INodeRegistry
    {
        Node Add(string title, string vendor, string description);
        void Remove(NodeId id);
        Node? Get(NodeId id);
        IEnumerable<Node> All { get; }

        event EventHandler<Node>? Added;
        event EventHandler<NodeId>? Removed;
    }

    public sealed class NodeRegistry : INodeRegistry
    {
        private readonly Dictionary<NodeId, Node> _nodes = new();

        public IEnumerable<Node> All => _nodes.Values;

        public event EventHandler<Node>? Added;
        public event EventHandler<NodeId>? Removed;

        public Node Add(string title, string vendor, string description)
        {
            var node = new Node(NodeId.New(), title, vendor, description);
            _nodes[node.Id] = node;
            Added?.Invoke(this, node);
            return node;
        }

        public void Remove(NodeId id)
        {
            if (_nodes.Remove(id))
                Removed?.Invoke(this, id);
        }

        public Node? Get(NodeId id) => _nodes.TryGetValue(id, out var n) ? n : null;
    }
}
