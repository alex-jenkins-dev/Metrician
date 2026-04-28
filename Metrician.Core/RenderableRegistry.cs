// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Renderable.Contracts;

namespace Metrician.Core
{
    public sealed class RenderableRegistry : IRenderableRegistry
    {
        private readonly Dictionary<Type, Func<object, IRenderable>> _factories = new();

        public void Register<T>(IRenderableFactory<T> factory)
        {
            if (factory is null) throw new ArgumentNullException(nameof(factory));
            _factories[typeof(T)] = obj => factory.Create((T)obj);
        }

        public bool TryCreate(object value, out IRenderable? renderable)
        {
            renderable = null;
            if (value is null) return false;

            for (Type? t = value.GetType(); t != null; t = t.BaseType)
            {
                if (_factories.TryGetValue(t, out var fac))
                {
                    renderable = fac(value);
                    return renderable is not null;
                }
            }

            foreach (var iface in value.GetType().GetInterfaces())
            {
                if (_factories.TryGetValue(iface, out var fac))
                {
                    renderable = fac(value);
                    return renderable is not null;
                }
            }

            return false;
        }
    }
}
