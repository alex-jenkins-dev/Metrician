// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IRenderOptionsSystem
    {
        void Set(PinId target, object options);
        object? Get(PinId target);
        void Clear(PinId target);
        void RemoveAllFor(NodeId owner);

        event EventHandler<PinId>? Changed;
    }

    public sealed class RenderOptionsSystem : IRenderOptionsSystem
    {
        private readonly Dictionary<PinId, object> _byPin = new();

        public event EventHandler<PinId>? Changed;

        public RenderOptionsSystem(IWireSystem wires)
        {
            if (wires is null) throw new ArgumentNullException(nameof(wires));
            wires.Disconnected += (_, w) => Clear(w.Target);
        }

        public void Set(PinId target, object options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            _byPin[target] = options;
            Changed?.Invoke(this, target);
        }

        public object? Get(PinId target) =>
            _byPin.TryGetValue(target, out var o) ? o : null;

        public void Clear(PinId target)
        {
            if (_byPin.Remove(target))
                Changed?.Invoke(this, target);
        }

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var pin in _byPin.Keys.Where(p => p.Owner == owner).ToList())
                Clear(pin);
        }
    }
}
