// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IGraphWorld
    {
        INodeRegistry Nodes { get; }
        IPinSystem Pins { get; }
        IPinColourSystem PinColours { get; }
        IPinGroupSystem PinGroups { get; }
        IWireSystem Wires { get; }
        IValueSystem Values { get; }
        IPropertySystem Properties { get; }
        IEvaluationSystem Evaluation { get; }
        IDynamicUpdateSystem DynamicUpdates { get; }
        ITagSystem Tags { get; }
        ILayoutSystem Layout { get; }
        IConversionSystem Conversions { get; }
        IValueConverterRegistry Converters { get; }
        IPinConnector PinConnector { get; }
        INodeStatusSystem Status { get; }
        INodeErrorSystem Errors { get; }
        IPinConstraintSystem PinConstraints { get; }
        IPropertyConstraintSystem PropertyConstraints { get; }
        IValidationSystem Validation { get; }

        NodeId Add(INodeTemplate template);

        void Remove(NodeId id);

        void Register<T>(T system) where T : class;

        T? Resolve<T>() where T : class;
    }

    public sealed class GraphWorld : IGraphWorld
    {
        private readonly Dictionary<Type, object> _systems = new();
        private readonly Dictionary<NodeId, NodeAuthor> _authors = new();

        public INodeRegistry Nodes { get; }
        public IPinSystem Pins { get; }
        public IPinColourSystem PinColours { get; }
        public IPinGroupSystem PinGroups { get; }
        public IWireSystem Wires { get; }
        public IValueSystem Values { get; }
        public IPropertySystem Properties { get; }
        public IEvaluationSystem Evaluation { get; }
        public IDynamicUpdateSystem DynamicUpdates { get; }
        public ITagSystem Tags { get; }
        public ILayoutSystem Layout { get; }
        public IConversionSystem Conversions { get; }
        public IValueConverterRegistry Converters { get; }
        public IPinConnector PinConnector { get; }
        public INodeStatusSystem Status { get; }
        public INodeErrorSystem Errors { get; }
        public IPinConstraintSystem PinConstraints { get; }
        public IPropertyConstraintSystem PropertyConstraints { get; }
        public IValidationSystem Validation { get; }

        public GraphWorld()
        {
            Nodes = AddBuiltin<INodeRegistry>(new NodeRegistry());
            Pins = AddBuiltin<IPinSystem>(new PinSystem());
            PinColours = AddBuiltin<IPinColourSystem>(new PinColourSystem(Pins));
            PinGroups = AddBuiltin<IPinGroupSystem>(new PinGroupSystem(Pins));
            Wires = AddBuiltin<IWireSystem>(new WireSystem(Pins));
            Values = AddBuiltin<IValueSystem>(new ValueSystem());
            Properties = AddBuiltin<IPropertySystem>(new PropertySystem());
            Errors = AddBuiltin<INodeErrorSystem>(new NodeErrorSystem());
            Status = AddBuiltin<INodeStatusSystem>(new NodeStatusSystem());
            Converters = AddBuiltin<IValueConverterRegistry>(new ValueConverterRegistry());
            Evaluation = AddBuiltin<IEvaluationSystem>(new EvaluationSystem(Pins, Wires, Values, Errors, Status, Converters));
            DynamicUpdates = AddBuiltin<IDynamicUpdateSystem>(new DynamicUpdateSystem());
            Tags = AddBuiltin<ITagSystem>(new TagSystem());
            Layout = AddBuiltin<ILayoutSystem>(new LayoutSystem());
            Conversions = AddBuiltin<IConversionSystem>(new ConversionSystem(Wires));
            PinConnector = AddBuiltin<IPinConnector>(new PinConnector(Pins, Wires, Converters, Conversions));
            PinConstraints = AddBuiltin<IPinConstraintSystem>(new PinConstraintSystem(Pins));
            PropertyConstraints = AddBuiltin<IPropertyConstraintSystem>(new PropertyConstraintSystem(Properties));
            Validation = AddBuiltin<IValidationSystem>(new ValidationSystem(
                Pins, Wires, Properties, PinConstraints, PropertyConstraints, Status));
        }

        public NodeId Add(INodeTemplate template)
        {
            var node = Nodes.Add(template.Title, template.Vendor, template.Description);
            var author = new NodeAuthor(node.Id, this);
            _authors[node.Id] = author;
            template.Configure(author);

            if (!Evaluation.Has(node.Id))
            {
                Remove(node.Id);
                throw new InvalidOperationException(
                    $"Template '{template.Title}' did not call Behaviour.OnEvaluate. " +
                    "Every node must register an evaluator.");
            }

            Validation.Revalidate(node.Id);
            return node.Id;
        }

        public void Remove(NodeId id)
        {
            if (_authors.Remove(id, out var author))
                author.Dispose();

            Validation.ClearHolisticValidator(id);
            Wires.RemoveAllFor(id);
            Conversions.RemoveAllFor(id);
            Values.RemoveAllFor(id);
            PinColours.RemoveAllFor(id);
            PinGroups.RemoveAllFor(id);
            PinConstraints.RemoveAllFor(id);
            PropertyConstraints.RemoveAllFor(id);
            Pins.RemoveAllFor(id);
            Properties.RemoveAllFor(id);
            Evaluation.Clear(id);
            DynamicUpdates.Clear(id);
            Tags.RemoveAllFor(id);
            Layout.Clear(id);
            Status.Clear(id);
            Errors.Clear(id);
            Nodes.Remove(id);
        }

        public void Register<T>(T system) where T : class =>
            _systems[typeof(T)] = system ?? throw new ArgumentNullException(nameof(system));

        public T? Resolve<T>() where T : class =>
            _systems.TryGetValue(typeof(T), out var s) ? (T)s : null;

        private T AddBuiltin<T>(T system) where T : class
        {
            Register(system);
            return system;
        }
    }
}
