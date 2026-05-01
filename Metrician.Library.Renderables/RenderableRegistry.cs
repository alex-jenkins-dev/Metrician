// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Library.Renderables
{
    public sealed class RenderableRegistry : IRenderableRegistry
    {
        private readonly Dictionary<Type, Entry> _factories = new();

        public void Register<T>(IRenderableFactory<T> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            _factories[typeof(T)] = new Entry(
                (obj, opts) => factory.Create((T)obj, opts),
                factory.OptionsType);
        }

        public bool TryCreate(object value, out IRenderable? renderable) =>
            TryCreate(value, null, out renderable);

        public bool TryCreate(object value, object? options, out IRenderable? renderable)
        {
            renderable = null;
            if (value is null) return false;

            for (Type? t = value.GetType(); t != null; t = t.BaseType)
            {
                if (_factories.TryGetValue(t, out var entry))
                {
                    renderable = entry.Create(value, options);
                    return renderable is not null;
                }
            }

            foreach (var iface in value.GetType().GetInterfaces())
            {
                if (_factories.TryGetValue(iface, out var entry))
                {
                    renderable = entry.Create(value, options);
                    return renderable is not null;
                }
            }

            return false;
        }

        public bool TryGetOptionsType(Type dataType, out Type? optionsType)
        {
            if (dataType is null) throw new ArgumentNullException(nameof(dataType));

            for (Type? t = dataType; t != null; t = t.BaseType)
            {
                if (_factories.TryGetValue(t, out var entry))
                {
                    optionsType = entry.OptionsType;
                    return true;
                }
            }

            foreach (var iface in dataType.GetInterfaces())
            {
                if (_factories.TryGetValue(iface, out var entry))
                {
                    optionsType = entry.OptionsType;
                    return true;
                }
            }

            optionsType = null;
            return false;
        }

        private sealed record Entry(
            Func<object, object?, IRenderable> Create,
            Type? OptionsType);
    }
}
