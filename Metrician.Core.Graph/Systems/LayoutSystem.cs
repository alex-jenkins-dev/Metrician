// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;

namespace Metrician.Core.Graph
{
    public interface ILayoutSystem
    {
        Vector2? Get(NodeId id);
        void Set(NodeId id, Vector2 position);
        void Clear(NodeId id);

        event EventHandler<NodeId>? Changed;
    }

    public sealed class LayoutSystem : ILayoutSystem
    {
        private readonly Dictionary<NodeId, Vector2> _positions = new();

        public event EventHandler<NodeId>? Changed;

        public Vector2? Get(NodeId id) =>
            _positions.TryGetValue(id, out var p) ? p : null;

        public void Set(NodeId id, Vector2 position)
        {
            _positions[id] = position;
            Changed?.Invoke(this, id);
        }

        public void Clear(NodeId id)
        {
            if (_positions.Remove(id))
                Changed?.Invoke(this, id);
        }
    }
}
