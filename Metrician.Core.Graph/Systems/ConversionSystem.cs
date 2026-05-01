// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IConversionSystem
    {
        void Mark(PinId target);
        void Clear(PinId target);
        void RemoveAllFor(NodeId owner);
        bool IsConverted(PinId target);

        event EventHandler<PinId>? Changed;
    }

    public sealed class ConversionSystem : IConversionSystem
    {
        private readonly HashSet<PinId> _converted = new();

        public event EventHandler<PinId>? Changed;

        public void Mark(PinId target)
        {
            if (_converted.Add(target))
                Changed?.Invoke(this, target);
        }

        public void Clear(PinId target)
        {
            if (_converted.Remove(target))
                Changed?.Invoke(this, target);
        }

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var pin in _converted.Where(p => p.Owner == owner).ToList())
                Clear(pin);
        }

        public bool IsConverted(PinId target) =>
            _converted.Contains(target);
    }
}
