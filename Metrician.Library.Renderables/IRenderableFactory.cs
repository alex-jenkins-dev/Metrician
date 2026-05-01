// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Library.Renderables
{
    public interface IRenderableFactory<in T>
    {
        IRenderable Create(T value);
    }

    public interface IRenderableRegistry
    {
        void Register<T>(IRenderableFactory<T> factory);

        bool TryCreate(object value, out IRenderable? renderable);
    }
}
