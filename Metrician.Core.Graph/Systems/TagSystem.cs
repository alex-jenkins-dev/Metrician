// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface ITagSystem
    {
        void Add(NodeId id, string tag);
        void Remove(NodeId id, string tag);
        void RemoveAllFor(NodeId id);
        bool Has(NodeId id, string tag);
        IReadOnlyCollection<string> TagsOf(NodeId id);
        IEnumerable<NodeId> NodesWith(string tag);

        event EventHandler<(NodeId Id, string Tag)>? Added;
        event EventHandler<(NodeId Id, string Tag)>? Removed;
    }

    public sealed class TagSystem : ITagSystem
    {
        private readonly Dictionary<NodeId, HashSet<string>> _byNode = new();

        public event EventHandler<(NodeId Id, string Tag)>? Added;
        public event EventHandler<(NodeId Id, string Tag)>? Removed;

        public void Add(NodeId id, string tag)
        {
            if (!_byNode.TryGetValue(id, out var set))
                _byNode[id] = set = new HashSet<string>(StringComparer.Ordinal);
            if (set.Add(tag))
                Added?.Invoke(this, (id, tag));
        }

        public void Remove(NodeId id, string tag)
        {
            if (_byNode.TryGetValue(id, out var set) && set.Remove(tag))
                Removed?.Invoke(this, (id, tag));
        }

        public void RemoveAllFor(NodeId id)
        {
            if (!_byNode.TryGetValue(id, out var set)) return;
            foreach (var tag in set.ToList()) Remove(id, tag);
            _byNode.Remove(id);
        }

        public bool Has(NodeId id, string tag) =>
            _byNode.TryGetValue(id, out var set) && set.Contains(tag);

        public IReadOnlyCollection<string> TagsOf(NodeId id) =>
            _byNode.TryGetValue(id, out var set) ? set : Array.Empty<string>();

        public IEnumerable<NodeId> NodesWith(string tag) =>
            _byNode.Where(kv => kv.Value.Contains(tag)).Select(kv => kv.Key);
    }
}
