// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public sealed record NodeError(string Message, Exception? Exception);

    public interface INodeErrorSystem
    {
        void Add(NodeId id, string message, Exception? exception = null);
        void Clear(NodeId id);
        IReadOnlyList<NodeError> Get(NodeId id);

        event EventHandler<NodeId>? Changed;
    }

    public sealed class NodeErrorSystem : INodeErrorSystem
    {
        private readonly Dictionary<NodeId, List<NodeError>> _errors = new();

        public event EventHandler<NodeId>? Changed;

        public void Add(NodeId id, string message, Exception? exception = null)
        {
            if (!_errors.TryGetValue(id, out var list))
                _errors[id] = list = new List<NodeError>();
            list.Add(new NodeError(message, exception));
            Changed?.Invoke(this, id);
        }

        public void Clear(NodeId id)
        {
            if (_errors.Remove(id))
                Changed?.Invoke(this, id);
        }

        public IReadOnlyList<NodeError> Get(NodeId id) =>
            _errors.TryGetValue(id, out var list) ? list : Array.Empty<NodeError>();
    }
}
