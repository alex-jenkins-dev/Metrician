// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Core.ScriptBinding
{
    public sealed class NodeTemplateRegistry : INodeTemplateRegistry
    {
        private readonly Dictionary<string, Func<INodeTemplate>> _factories =
            new(StringComparer.Ordinal);

        public IEnumerable<string> Names => _factories.Keys;

        public void Register(string typeName, Func<INodeTemplate> factory)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Type name must be non-empty.", nameof(typeName));
            _factories[typeName] = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public bool Unregister(string typeName) => _factories.Remove(typeName);

        public INodeTemplate? Create(string typeName) =>
            _factories.TryGetValue(typeName, out var factory) ? factory() : null;
    }
}
