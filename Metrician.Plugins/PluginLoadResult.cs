// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph.Contracts;
using Metrician.Renderable.Contracts;

namespace Metrician.Plugins
{
    public sealed class DiscoveredNode
    {
        /// <summary>
        /// Menu label; <see cref="MetricianNodeMenuAttribute.Label"/> if present,
        /// otherwise <see cref="INode.Title"/>.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Vendor name; the editor groups menu entries by this.
        /// </summary>
        public string Vendor { get; }

        public Type NodeType { get; }

        public Func<INode> Factory { get; }

        public DiscoveredNode(string label, string vendor, Type nodeType, Func<INode> factory)
        {
            Label = label;
            Vendor = vendor;
            NodeType = nodeType;
            Factory = factory;
        }
    }

    public sealed class DiscoveredFactory
    {
        /// <summary>
        /// The T in <see cref="IRenderableFactory{T}"/>.
        /// </summary>
        public Type DataType { get; }

        /// <summary>
        /// The factory instance, boxed since T is not known at compile time.
        /// </summary>
        public object Factory { get; }

        public Type FactoryType { get; }

        public DiscoveredFactory(Type dataType, object factory, Type factoryType)
        {
            DataType = dataType;
            Factory = factory;
            FactoryType = factoryType;
        }

        /// <summary>
        /// Calls <c>registry.Register&lt;T&gt;(factory)</c> via reflection since T is only known at runtime.
        /// </summary>
        public void RegisterWith(IRenderableRegistry registry)
        {
            var registerMethod = typeof(IRenderableRegistry)
                .GetMethod(nameof(IRenderableRegistry.Register))!;
            var generic = registerMethod.MakeGenericMethod(DataType);
            generic.Invoke(registry, new[] { Factory });
        }
    }

    /// <summary>
    /// Aggregated outcome of one or more loads.
    /// Discoveries are deduplicated by type so <see cref="Merge"/> is safe to call with overlapping inputs.
    /// </summary>
    public sealed class PluginLoadResult
    {
        private readonly List<DiscoveredNode> _nodes = new();
        private readonly List<DiscoveredFactory> _factories = new();
        private readonly List<string> _errors = new();
        private readonly HashSet<Type> _seenTypes = new();

        public IReadOnlyList<DiscoveredNode> Nodes => _nodes;
        public IReadOnlyList<DiscoveredFactory> Factories => _factories;

        /// <summary>Non-fatal load errors, one per failed assembly, file, or type.</summary>
        public IReadOnlyList<string> Errors => _errors;

        internal void AddNode(DiscoveredNode node)
        {
            if (_seenTypes.Add(node.NodeType))
                _nodes.Add(node);
        }

        internal void AddFactory(DiscoveredFactory factory)
        {
            if (_seenTypes.Add(factory.FactoryType))
                _factories.Add(factory);
        }

        internal void AddError(string message) => _errors.Add(message);

        /// <summary>Folds another result into this one, skipping duplicates.</summary>
        public void Merge(PluginLoadResult other)
        {
            foreach (var node in other._nodes) AddNode(node);
            foreach (var factory in other._factories) AddFactory(factory);
            foreach (var err in other._errors) _errors.Add(err);
        }
    }
}
