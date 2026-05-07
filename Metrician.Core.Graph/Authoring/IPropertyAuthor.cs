// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IPropertyAuthor
    {
        void Define<T>(string name, T initial);
        T? Get<T>(string name);
        void Set(string name, object? value);

        void Constrain(string name, Func<object?, string?> validator);
        void Unconstrain(string name);

        void OnChanged(Action<string> handler);
    }
}
