// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IValueSystem
    {
        object? Get(PinId outputPin);
        void Set(PinId outputPin, object? value);
        void Clear(PinId outputPin);
        void RemoveAllFor(NodeId owner);

        event EventHandler<PinId>? Changed;
    }

    public sealed class ValueSystem : IValueSystem
    {
        private readonly Dictionary<PinId, object?> _values = new();

        public event EventHandler<PinId>? Changed;

        public object? Get(PinId outputPin) =>
            _values.TryGetValue(outputPin, out var v) ? v : null;

        public void Set(PinId outputPin, object? value)
        {
            _values[outputPin] = value;
            Changed?.Invoke(this, outputPin);
        }

        public void Clear(PinId outputPin)
        {
            if (_values.Remove(outputPin))
                Changed?.Invoke(this, outputPin);
        }

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var id in _values.Keys.Where(k => k.Owner == owner).ToList())
                Clear(id);
        }
    }
}
