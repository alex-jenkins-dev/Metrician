// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public sealed record Property(NodeId Owner, string Name, Type Type, object? Value);

    public interface IPropertySystem
    {
        void Define<T>(NodeId id, string name, T initial);
        void Define(NodeId id, string name, Type type, object? initial);

        T? Get<T>(NodeId id, string name);
        object? Get(NodeId id, string name);

        void Set(NodeId id, string name, object? value);
        void Remove(NodeId id, string name);
        void RemoveAllFor(NodeId owner);

        IEnumerable<Property> PropertiesOf(NodeId id);

        event EventHandler<(NodeId Id, string Name)>? Changed;
        event EventHandler<(NodeId Id, string Name)>? Removed;
    }

    public sealed class PropertySystem : IPropertySystem
    {
        private readonly Dictionary<(NodeId, string), Property> _props = new();

        public event EventHandler<(NodeId Id, string Name)>? Changed;
        public event EventHandler<(NodeId Id, string Name)>? Removed;

        public void Define<T>(NodeId id, string name, T initial) =>
            Define(id, name, typeof(T), initial);

        public void Define(NodeId id, string name, Type type, object? initial)
        {
            _props[(id, name)] = new Property(id, name, type, initial);
            Changed?.Invoke(this, (id, name));
        }

        public T? Get<T>(NodeId id, string name) =>
            _props.TryGetValue((id, name), out var p) && p.Value is T t ? t : default;

        public object? Get(NodeId id, string name) =>
            _props.TryGetValue((id, name), out var p) ? p.Value : null;

        public void Set(NodeId id, string name, object? value)
        {
            if (!_props.TryGetValue((id, name), out var existing))
                throw new InvalidOperationException($"Property '{name}' is not defined on node {id}.");
            if (value != null && !existing.Type.IsAssignableFrom(value.GetType()))
                throw new InvalidCastException(
                    $"Value of type {value.GetType().Name} is not assignable to {existing.Type.Name}.");
            _props[(id, name)] = existing with { Value = value };
            Changed?.Invoke(this, (id, name));
        }

        public void Remove(NodeId id, string name)
        {
            if (_props.Remove((id, name)))
                Removed?.Invoke(this, (id, name));
        }

        public void RemoveAllFor(NodeId owner)
        {
            foreach (var key in _props.Keys.Where(k => k.Item1 == owner).ToList())
                Remove(key.Item1, key.Item2);
        }

        public IEnumerable<Property> PropertiesOf(NodeId id) =>
            _props.Where(kv => kv.Key.Item1 == id).Select(kv => kv.Value);
    }
}
