// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Contracts.Renderables
{
    public interface IRenderableFactory<in T>
    {
        IRenderable Create(T value);
    }

    /// <summary>
    /// Registry of <see cref="IRenderableFactory{T}"/> instances keyed by value type.
    /// </summary>
    public interface IRenderableRegistry
    {
        void Register<T>(IRenderableFactory<T> factory);

        /// <summary>
        /// Looks up a factory for <paramref name="value"/>'s runtime type, walking
        /// base classes and then interfaces. Returns false when no factory matches.
        /// </summary>
        bool TryCreate(object value, out IRenderable? renderable);
    }
}
