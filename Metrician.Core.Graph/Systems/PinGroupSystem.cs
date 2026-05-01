// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IPinGroupSystem
    {
        void Set(PinId pin, string group);
        void Clear(PinId pin);
        void RemoveAllFor(NodeId owner);
        string? Get(PinId pin);
        IEnumerable<PinId> PinsIn(string group);

        event EventHandler<PinId>? Changed;
    }

    public sealed class PinGroupSystem : IPinGroupSystem
    {
        private readonly Dictionary<PinId, string> _groups = new();

        public event EventHandler<PinId>? Changed;

        public PinGroupSystem(IPinSystem pins)
        {
            pins.Removed += (_, pinId) =>
            {
                if (_groups.Remove(pinId))
                    Changed?.Invoke(this, pinId);
            };
        }

        public void Set(PinId pin, string group)
        {
            _groups[pin] = group;
            Changed?.Invoke(this, pin);
        }

        public void Clear(PinId pin)
        {
            if (_groups.Remove(pin))
                Changed?.Invoke(this, pin);
        }

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var pin in _groups.Keys.Where(k => k.Owner == owner).ToList())
                Clear(pin);
        }

        public string? Get(PinId pin) =>
            _groups.TryGetValue(pin, out var g) ? g : null;

        public IEnumerable<PinId> PinsIn(string group) =>
            _groups.Where(kv => kv.Value == group).Select(kv => kv.Key);
    }
}
