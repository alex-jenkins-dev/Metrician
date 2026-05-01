// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Contracts.Graph;

namespace Metrician.Core
{
    public class NodeInput<T> : INodeInput<T>
    {
        public string Name { get; }
        public INode Owner { get; }
        public Type ValueType => typeof(T);
        public bool IsInput => true;

        private IValueProvider? _provider;

        public INodeOutput? Source => _provider?.Source;

        public NodeInput(INode owner, string name)
        {
            Owner = owner;
            Name = name;
        }

        public bool TryConnect(IValueProvider? provider)
        {
            if (provider is null) { _provider = null; return true; }
            if (!ValueType.IsAssignableFrom(provider.OutputType)) return false;
            _provider = provider;
            return true;
        }

        public T? CurrentValue
        {
            get
            {
                var v = _provider?.GetValue();
                return v is T t ? t : default;
            }
        }
    }

    public class NodeOutput<T> : INodeOutput<T>
    {
        public string Name { get; }
        public INode Owner { get; }
        public Type ValueType => typeof(T);
        public bool IsInput => false;

        public T? CurrentValue { get; set; }

        public object? Value => CurrentValue;

        public NodeOutput(INode owner, string name)
        {
            Owner = owner;
            Name = name;
        }
    }
}
