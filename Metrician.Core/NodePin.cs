// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph.Contracts;

namespace Metrician.Core
{
    public class NodeInput<T> : INodeInput<T>
    {
        public string Name { get; }
        public INode Owner { get; }
        public Type ValueType => typeof(T);
        public bool IsInput => true;
        public INodeOutput? Source { get; private set; }

        public NodeInput(INode owner, string name)
        {
            Owner = owner;
            Name = name;
        }

        public bool TryConnect(INodeOutput? source)
        {
            if (source is null) { Source = null; return true; }
            if (!ValueType.IsAssignableFrom(source.ValueType)) return false;

            Source = source;
            return true;
        }

        public T? CurrentValue
        {
            get
            {
                var v = Source?.Value;
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
