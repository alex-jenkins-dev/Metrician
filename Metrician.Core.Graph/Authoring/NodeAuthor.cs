// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    internal sealed class NodeAuthor :
        INodeAuthor,
        IPinAuthor,
        IPropertyAuthor,
        IBehaviourAuthor,
        IValidationAuthor,
        ITagAuthor,
        IDynamicHandle,
        IDisposable
    {
        private readonly GraphWorld _world;
        private readonly List<IDisposable> _subscriptions = new();

        public NodeId NodeId { get; }

        public IPinAuthor Pins => this;
        public IPropertyAuthor Properties => this;
        public IBehaviourAuthor Behaviour => this;
        public IValidationAuthor Validation => this;
        public ITagAuthor Tags => this;

        public NodeAuthor(NodeId id, GraphWorld world)
        {
            NodeId = id;
            _world = world;
        }

        IReadOnlyList<Pin> IPinAuthor.Inputs => _world.Pins.Inputs(NodeId).ToList();
        IReadOnlyList<Pin> IPinAuthor.Outputs => _world.Pins.Outputs(NodeId).ToList();

        Pin IPinAuthor.AddInput<T>(string name) =>
            _world.Pins.Add(NodeId, name, PinDirection.Input, typeof(T));
        Pin IPinAuthor.AddOutput<T>(string name) =>
            _world.Pins.Add(NodeId, name, PinDirection.Output, typeof(T));

        void IPinAuthor.RemoveInput(string name) =>
            _world.Pins.Remove(new PinId(NodeId, name, PinDirection.Input));
        void IPinAuthor.RemoveOutput(string name) =>
            _world.Pins.Remove(new PinId(NodeId, name, PinDirection.Output));

        bool IPinAuthor.IsConnected(PinId pin) => pin.Direction == PinDirection.Input
            ? _world.Wires.SourceOf(pin) is not null
            : _world.Wires.All.Any(w => w.Source == pin);

        PinId? IPinAuthor.SourceOf(PinId target) => _world.Wires.SourceOf(target);
        bool IPinAuthor.TryConnect(PinId source, PinId target) =>
            _world.Wires.TryConnect(source, target);
        void IPinAuthor.Disconnect(PinId target) => _world.Wires.Disconnect(target);

        void IPinAuthor.Constrain(PinId pin, Func<bool, string?> validator) =>
            _world.PinConstraints.Set(pin, validator);
        void IPinAuthor.Unconstrain(PinId pin) =>
            _world.PinConstraints.Clear(pin);

        void IPinAuthor.Colour(PinId pin, PinColour colour) =>
            _world.PinColours.Set(pin, colour);
        void IPinAuthor.ClearColour(PinId pin) =>
            _world.PinColours.Clear(pin);

        void IPinAuthor.Group(PinId pin, string group) =>
            _world.PinGroups.Set(pin, group);
        void IPinAuthor.ClearGroup(PinId pin) =>
            _world.PinGroups.Clear(pin);

        void IPinAuthor.OnConnectionChanged(Action<WireChange> handler)
        {
            EventHandler<Wire> onConnected = (_, w) =>
            {
                if (Touches(w))
                    handler(new WireChange(WireChangeKind.Connected, w));
            };
            EventHandler<Wire> onDisconnected = (_, w) =>
            {
                if (Touches(w))
                    handler(new WireChange(WireChangeKind.Disconnected, w));
            };
            _world.Wires.Connected += onConnected;
            _world.Wires.Disconnected += onDisconnected;
            _subscriptions.Add(new Subscription(() =>
            {
                _world.Wires.Connected -= onConnected;
                _world.Wires.Disconnected -= onDisconnected;
            }));
        }

        void IPropertyAuthor.Define<T>(string name, T initial) =>
            _world.Properties.Define(NodeId, name, initial);

        T? IPropertyAuthor.Get<T>(string name) where T : default =>
            _world.Properties.Get<T>(NodeId, name);

        void IPropertyAuthor.Set(string name, object? value) =>
            _world.Properties.Set(NodeId, name, value);

        void IPropertyAuthor.Constrain(string name, Func<object?, string?> validator) =>
            _world.PropertyConstraints.Set(NodeId, name, validator);
        void IPropertyAuthor.Unconstrain(string name) =>
            _world.PropertyConstraints.Clear(NodeId, name);

        void IPropertyAuthor.OnChanged(Action<string> handler)
        {
            EventHandler<(NodeId Id, string Name)> onChanged = (_, e) =>
            {
                if (e.Id == NodeId) handler(e.Name);
            };
            _world.Properties.Changed += onChanged;
            _subscriptions.Add(new Subscription(() =>
                _world.Properties.Changed -= onChanged));
        }

        void IBehaviourAuthor.OnEvaluate(Evaluator evaluator) =>
            _world.Evaluation.Set(NodeId, evaluator);

        void IBehaviourAuthor.OnDynamicUpdate(Func<IDynamicHandle, IDisposable> setup) =>
            _world.DynamicUpdates.Register(NodeId, setup(this));
        void IBehaviourAuthor.ClearDynamicUpdate() =>
            _world.DynamicUpdates.Clear(NodeId);

        void IValidationAuthor.OnValidate(Func<INodeAuthor, IReadOnlyList<string>> validate) =>
            _world.Validation.SetHolisticValidator(NodeId, () => validate(this));
        void IValidationAuthor.ClearValidator() =>
            _world.Validation.ClearHolisticValidator(NodeId);

        IReadOnlyCollection<string> ITagAuthor.All => _world.Tags.TagsOf(NodeId);
        void ITagAuthor.Add(string tag) => _world.Tags.Add(NodeId, tag);
        void ITagAuthor.Remove(string tag) => _world.Tags.Remove(NodeId, tag);
        bool ITagAuthor.Has(string tag) => _world.Tags.Has(NodeId, tag);

        void IDynamicHandle.RequestRefresh() => _world.DynamicUpdates.Request(NodeId);

        public void Dispose()
        {
            foreach (var s in _subscriptions) s.Dispose();
            _subscriptions.Clear();
        }

        private bool Touches(Wire w) =>
            w.Source.Owner == NodeId || w.Target.Owner == NodeId;

        private sealed class Subscription : IDisposable
        {
            private readonly Action _dispose;
            public Subscription(Action dispose) { _dispose = dispose; }
            public void Dispose() => _dispose();
        }
    }
}
