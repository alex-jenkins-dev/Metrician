// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Library.Renderables
{
    public interface IRenderableFactory<in T>
    {
        IRenderable Create(T value);

        IRenderable Create(T value, object? options) => Create(value);

        Type? OptionsType => null;
    }

    public interface IRenderableRegistry
    {
        void Register<T>(IRenderableFactory<T> factory);

        bool TryCreate(object value, out IRenderable? renderable);

        bool TryCreate(object value, object? options, out IRenderable? renderable);

        bool TryGetOptionsType(Type dataType, out Type? optionsType);
    }
}
