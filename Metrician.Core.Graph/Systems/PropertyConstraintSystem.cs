// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IPropertyConstraintSystem
    {
        void Set(NodeId id, string name, Func<object?, string?> validator);
        void Clear(NodeId id, string name);
        Func<object?, string?>? Get(NodeId id, string name);
        void RemoveAllFor(NodeId owner);

        event EventHandler<(NodeId Id, string Name)>? Changed;
    }

    public sealed class PropertyConstraintSystem : IPropertyConstraintSystem
    {
        private readonly Dictionary<(NodeId, string), Func<object?, string?>> _validators = new();

        public event EventHandler<(NodeId Id, string Name)>? Changed;

        public PropertyConstraintSystem(IPropertySystem properties)
        {
            properties.Removed += (_, e) =>
            {
                if (_validators.Remove((e.Id, e.Name)))
                    Changed?.Invoke(this, (e.Id, e.Name));
            };
        }

        public void Set(NodeId id, string name, Func<object?, string?> validator)
        {
            _validators[(id, name)] = validator;
            Changed?.Invoke(this, (id, name));
        }

        public void Clear(NodeId id, string name)
        {
            if (_validators.Remove((id, name)))
                Changed?.Invoke(this, (id, name));
        }

        public Func<object?, string?>? Get(NodeId id, string name) =>
            _validators.TryGetValue((id, name), out var v) ? v : null;

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var key in _validators.Keys.Where(k => k.Item1 == owner).ToList())
                Clear(key.Item1, key.Item2);
        }
    }
}
