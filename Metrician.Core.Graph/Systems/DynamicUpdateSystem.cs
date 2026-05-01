// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IDynamicUpdateSystem
    {
        void Request(NodeId id);
        void Register(NodeId id, IDisposable lifetime);
        void Clear(NodeId id);
        bool HasLifetime(NodeId id);

        event EventHandler<NodeId>? LifetimeChanged;
        event EventHandler<NodeId>? UpdateRequested;
    }

    public sealed class DynamicUpdateSystem : IDynamicUpdateSystem
    {
        private readonly Dictionary<NodeId, IDisposable> _lifetimes = new();

        public event EventHandler<NodeId>? LifetimeChanged;
        public event EventHandler<NodeId>? UpdateRequested;

        public void Request(NodeId id) => UpdateRequested?.Invoke(this, id);

        public void Register(NodeId id, IDisposable lifetime)
        {
            if (_lifetimes.Remove(id, out var prev)) prev.Dispose();
            _lifetimes[id] = lifetime;
            LifetimeChanged?.Invoke(this, id);
        }

        public void Clear(NodeId id)
        {
            if (_lifetimes.Remove(id, out var lifetime))
            {
                lifetime.Dispose();
                LifetimeChanged?.Invoke(this, id);
            }
        }

        public bool HasLifetime(NodeId id) => _lifetimes.ContainsKey(id);
    }
}
