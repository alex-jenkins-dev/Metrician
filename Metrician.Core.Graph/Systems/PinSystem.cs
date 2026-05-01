// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IPinSystem
    {
        Pin Add(NodeId owner, string name, PinDirection direction, Type valueType);
        void Remove(PinId pinId);
        void RemoveAllFor(NodeId owner);
        Pin? Get(PinId pinId);
        IEnumerable<Pin> Inputs(NodeId owner);
        IEnumerable<Pin> Outputs(NodeId owner);

        event EventHandler<Pin>? Added;
        event EventHandler<PinId>? Removed;
    }

    // Keeps a parallel List<PinId> alongside the lookup dictionary so iteration
    // order is the insertion order even after remove+add cycles. Dictionary on
    // its own reuses freed slots LIFO and would scramble the visible pin order.
    public sealed class PinSystem : IPinSystem
    {
        private readonly Dictionary<PinId, Pin> _byId = new();
        private readonly List<PinId> _order = new();

        public event EventHandler<Pin>? Added;
        public event EventHandler<PinId>? Removed;

        public Pin Add(NodeId owner, string name, PinDirection direction, Type valueType)
        {
            var pin = new Pin(new PinId(owner, name, direction), valueType);
            if (!_byId.ContainsKey(pin.Id))
                _order.Add(pin.Id);
            _byId[pin.Id] = pin;
            Added?.Invoke(this, pin);
            return pin;
        }

        public void Remove(PinId pinId)
        {
            if (_byId.Remove(pinId))
            {
                _order.Remove(pinId);
                Removed?.Invoke(this, pinId);
            }
        }

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var id in _order.Where(k => k.Owner == owner).ToList())
                Remove(id);
        }

        public Pin? Get(PinId pinId) => _byId.TryGetValue(pinId, out var p) ? p : null;

        public IEnumerable<Pin> Inputs(NodeId owner)
        {
            foreach (var id in _order)
                if (id.Owner == owner && id.Direction == PinDirection.Input)
                    yield return _byId[id];
        }

        public IEnumerable<Pin> Outputs(NodeId owner)
        {
            foreach (var id in _order)
                if (id.Owner == owner && id.Direction == PinDirection.Output)
                    yield return _byId[id];
        }
    }
}
