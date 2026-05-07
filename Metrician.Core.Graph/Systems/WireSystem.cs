// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IWireSystem
    {
        bool TryConnect(PinId source, PinId target);
        void Disconnect(PinId target);
        void RemoveAllFor(NodeId owner);
        PinId? SourceOf(PinId target);
        IEnumerable<Wire> All { get; }

        event EventHandler<Wire>? Connected;
        event EventHandler<Wire>? Disconnected;
    }

    public sealed class WireSystem : IWireSystem
    {
        private readonly Dictionary<PinId, PinId> _byTarget = new();

        public IEnumerable<Wire> All => _byTarget.Select(kv => new Wire(kv.Value, kv.Key));

        public event EventHandler<Wire>? Connected;
        public event EventHandler<Wire>? Disconnected;

        public WireSystem(IPinSystem pins)
        {
            pins.Removed += (_, pinId) =>
            {
                if (pinId.Direction == PinDirection.Input)
                {
                    Disconnect(pinId);
                }
                else
                {
                    var targets = _byTarget
                        .Where(kv => kv.Value == pinId)
                        .Select(kv => kv.Key)
                        .ToList();
                    foreach (var t in targets) Disconnect(t);
                }
            };
        }

        public bool TryConnect(PinId source, PinId target)
        {
            if (source.Direction != PinDirection.Output) return false;
            if (target.Direction != PinDirection.Input) return false;
            Disconnect(target);
            _byTarget[target] = source;
            Connected?.Invoke(this, new Wire(source, target));
            return true;
        }

        public void Disconnect(PinId target)
        {
            if (_byTarget.Remove(target, out var source))
                Disconnected?.Invoke(this, new Wire(source, target));
        }

        public void RemoveAllFor(NodeId owner)
        {
            var toRemove = _byTarget
                .Where(kv => kv.Key.Owner == owner || kv.Value.Owner == owner)
                .Select(kv => kv.Key)
                .ToList();
            foreach (var id in toRemove) Disconnect(id);
        }

        public PinId? SourceOf(PinId target) =>
            _byTarget.TryGetValue(target, out var s) ? s : null;
    }
}
