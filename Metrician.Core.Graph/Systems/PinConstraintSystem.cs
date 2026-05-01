// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IPinConstraintSystem
    {
        void Set(PinId pin, Func<bool, string?> validator);
        void Clear(PinId pin);
        Func<bool, string?>? Get(PinId pin);
        void RemoveAllFor(NodeId owner);

        event EventHandler<PinId>? Changed;
    }

    public sealed class PinConstraintSystem : IPinConstraintSystem
    {
        private readonly Dictionary<PinId, Func<bool, string?>> _validators = new();

        public event EventHandler<PinId>? Changed;

        public PinConstraintSystem(IPinSystem pins)
        {
            pins.Removed += (_, pinId) =>
            {
                if (_validators.Remove(pinId))
                    Changed?.Invoke(this, pinId);
            };
        }

        public void Set(PinId pin, Func<bool, string?> validator)
        {
            _validators[pin] = validator;
            Changed?.Invoke(this, pin);
        }

        public void Clear(PinId pin)
        {
            if (_validators.Remove(pin))
                Changed?.Invoke(this, pin);
        }

        public Func<bool, string?>? Get(PinId pin) =>
            _validators.TryGetValue(pin, out var v) ? v : null;

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var pin in _validators.Keys.Where(k => k.Owner == owner).ToList())
                Clear(pin);
        }
    }
}
