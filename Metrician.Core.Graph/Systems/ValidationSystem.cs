// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IValidationSystem
    {
        void SetHolisticValidator(NodeId id, Func<IReadOnlyList<string>> validator);
        void ClearHolisticValidator(NodeId id);
        void Revalidate(NodeId id);

        event EventHandler<NodeId>? Revalidated;
    }

    public sealed class ValidationSystem : IValidationSystem
    {
        private readonly IPinSystem _pins;
        private readonly IWireSystem _wires;
        private readonly IPropertySystem _properties;
        private readonly IPinConstraintSystem _pinConstraints;
        private readonly IPropertyConstraintSystem _propertyConstraints;
        private readonly INodeStatusSystem _status;
        private readonly Dictionary<NodeId, Func<IReadOnlyList<string>>> _holistic = new();

        public event EventHandler<NodeId>? Revalidated;

        public ValidationSystem(
            IPinSystem pins,
            IWireSystem wires,
            IPropertySystem properties,
            IPinConstraintSystem pinConstraints,
            IPropertyConstraintSystem propertyConstraints,
            INodeStatusSystem status)
        {
            _pins = pins;
            _wires = wires;
            _properties = properties;
            _pinConstraints = pinConstraints;
            _propertyConstraints = propertyConstraints;
            _status = status;

            _properties.Changed += (_, e) => Revalidate(e.Id);
            _properties.Removed += (_, e) => Revalidate(e.Id);
            _propertyConstraints.Changed += (_, e) => Revalidate(e.Id);
            _pins.Added += (_, p) => Revalidate(p.Id.Owner);
            _pins.Removed += (_, p) => Revalidate(p.Owner);
            _pinConstraints.Changed += (_, p) => Revalidate(p.Owner);
            _wires.Connected += (_, w) =>
            {
                Revalidate(w.Source.Owner);
                Revalidate(w.Target.Owner);
            };
            _wires.Disconnected += (_, w) =>
            {
                Revalidate(w.Source.Owner);
                Revalidate(w.Target.Owner);
            };
        }

        public void SetHolisticValidator(NodeId id, Func<IReadOnlyList<string>> validator)
        {
            _holistic[id] = validator;
            Revalidate(id);
        }

        public void ClearHolisticValidator(NodeId id)
        {
            if (_holistic.Remove(id))
                Revalidate(id);
        }

        public void Revalidate(NodeId id)
        {
            var reasons = new List<string>();

            foreach (var prop in _properties.PropertiesOf(id))
            {
                var validator = _propertyConstraints.Get(id, prop.Name);
                if (validator != null && validator(prop.Value) is { } msg)
                    reasons.Add($"{prop.Name}: {msg}");
            }

            foreach (var pin in _pins.Inputs(id).Concat(_pins.Outputs(id)))
            {
                var validator = _pinConstraints.Get(pin.Id);
                if (validator == null) continue;
                var connected = pin.Id.Direction == PinDirection.Input
                    ? _wires.SourceOf(pin.Id) is not null
                    : _wires.All.Any(w => w.Source == pin.Id);
                if (validator(connected) is { } msg)
                    reasons.Add($"{pin.Id.Name}: {msg}");
            }

            if (_holistic.TryGetValue(id, out var holistic))
            {
                try { reasons.AddRange(holistic()); }
                catch (Exception ex) { reasons.Add($"validator threw: {ex.Message}"); }
            }

            _status.Set(id, new NodeStatus(
                reasons.Count == 0 ? NodeReadiness.Ready : NodeReadiness.IllDefined,
                reasons));

            Revalidated?.Invoke(this, id);
        }
    }
}
