// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public enum NodeReadiness { NotReady, Ready }

    public sealed record NodeStatus(NodeReadiness Readiness, IReadOnlyList<string> Reasons);

    public interface INodeStatusSystem
    {
        void Set(NodeId id, NodeStatus status);
        NodeStatus? Get(NodeId id);
        void Clear(NodeId id);

        event EventHandler<NodeId>? Changed;
    }

    public sealed class NodeStatusSystem : INodeStatusSystem
    {
        private readonly Dictionary<NodeId, NodeStatus> _statuses = new();

        public event EventHandler<NodeId>? Changed;

        public void Set(NodeId id, NodeStatus status)
        {
            if (_statuses.TryGetValue(id, out var existing) && StatusEqual(existing, status))
                return;
            _statuses[id] = status;
            Changed?.Invoke(this, id);
        }

        private static bool StatusEqual(NodeStatus a, NodeStatus b) =>
            a.Readiness == b.Readiness && a.Reasons.SequenceEqual(b.Reasons);

        public NodeStatus? Get(NodeId id) =>
            _statuses.TryGetValue(id, out var s) ? s : null;

        public void Clear(NodeId id)
        {
            if (_statuses.Remove(id))
                Changed?.Invoke(this, id);
        }
    }
}
