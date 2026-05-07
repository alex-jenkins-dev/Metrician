// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public readonly record struct PinColour(byte R, byte G, byte B, byte A = 255);

    public interface IPinColourSystem
    {
        void Set(PinId pin, PinColour colour);
        void Clear(PinId pin);
        void RemoveAllFor(NodeId owner);
        PinColour? Get(PinId pin);

        event EventHandler<PinId>? Changed;
    }

    public sealed class PinColourSystem : IPinColourSystem
    {
        private readonly Dictionary<PinId, PinColour> _colours = new();

        public event EventHandler<PinId>? Changed;

        public PinColourSystem(IPinSystem pins)
        {
            pins.Removed += (_, pinId) =>
            {
                if (_colours.Remove(pinId))
                    Changed?.Invoke(this, pinId);
            };
        }

        public void Set(PinId pin, PinColour colour)
        {
            _colours[pin] = colour;
            Changed?.Invoke(this, pin);
        }

        public void Clear(PinId pin)
        {
            if (_colours.Remove(pin))
                Changed?.Invoke(this, pin);
        }

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var pin in _colours.Keys.Where(k => k.Owner == owner).ToList())
                Clear(pin);
        }

        public PinColour? Get(PinId pin) =>
            _colours.TryGetValue(pin, out var c) ? c : null;
    }
}
