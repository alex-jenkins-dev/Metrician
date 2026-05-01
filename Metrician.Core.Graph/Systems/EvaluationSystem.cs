// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public delegate void Evaluator(IEvaluationContext context);

    public interface IEvaluationContext
    {
        NodeId Self { get; }
        T? Read<T>(string inputName);
        void Write<T>(string outputName, T value);
        void Error(string message, Exception? exception = null);
    }

    public interface IEvaluationSystem
    {
        void Set(NodeId id, Evaluator evaluator);
        void Clear(NodeId id);
        bool Has(NodeId id);

        void EvaluateAll();

        event EventHandler<NodeId>? EvaluatorChanged;
        event EventHandler? EvaluationCompleted;
    }

    public sealed class EvaluationSystem : IEvaluationSystem
    {
        private readonly IPinSystem _pins;
        private readonly IWireSystem _wires;
        private readonly IValueSystem _values;
        private readonly INodeErrorSystem _errors;
        private readonly Dictionary<NodeId, Evaluator> _evaluators = new();

        public EvaluationSystem(
            IPinSystem pins,
            IWireSystem wires,
            IValueSystem values,
            INodeErrorSystem errors)
        {
            _pins = pins;
            _wires = wires;
            _values = values;
            _errors = errors;
        }

        public event EventHandler<NodeId>? EvaluatorChanged;
        public event EventHandler? EvaluationCompleted;

        public void Set(NodeId id, Evaluator evaluator)
        {
            _evaluators[id] = evaluator;
            EvaluatorChanged?.Invoke(this, id);
        }

        public void Clear(NodeId id)
        {
            if (_evaluators.Remove(id))
                EvaluatorChanged?.Invoke(this, id);
        }

        public bool Has(NodeId id) => _evaluators.ContainsKey(id);

        public void EvaluateAll()
        {
            var failed = new HashSet<NodeId>();

            foreach (var node in TopologicalOrder())
            {
                if (!_evaluators.TryGetValue(node, out var evaluator)) continue;

                if (HasFailedUpstream(node, failed))
                {
                    _errors.Clear(node);
                    _errors.Add(node, "upstream evaluation failed");
                    ClearOutputs(node);
                    failed.Add(node);
                    continue;
                }

                _errors.Clear(node);
                evaluator(new EvaluationContext(node, _wires, _values, _errors));

                if (_errors.Get(node).Count > 0)
                {
                    ClearOutputs(node);
                    failed.Add(node);
                }
            }
            EvaluationCompleted?.Invoke(this, EventArgs.Empty);
        }

        private bool HasFailedUpstream(NodeId node, HashSet<NodeId> failed)
        {
            foreach (var pin in _pins.Inputs(node))
            {
                var src = _wires.SourceOf(pin.Id);
                if (src is { } s && failed.Contains(s.Owner))
                    return true;
            }
            return false;
        }

        private void ClearOutputs(NodeId node)
        {
            foreach (var output in _pins.Outputs(node))
                _values.Clear(output.Id);
        }

        private IReadOnlyList<NodeId> TopologicalOrder()
        {
            var nodes = _evaluators.Keys.ToHashSet();
            var inDegree = nodes.ToDictionary(n => n, _ => 0);

            foreach (var wire in _wires.All)
                if (inDegree.ContainsKey(wire.Source.Owner) && inDegree.ContainsKey(wire.Target.Owner))
                    inDegree[wire.Target.Owner]++;

            var queue = new Queue<NodeId>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var order = new List<NodeId>();
            var seen = new HashSet<NodeId>();

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!seen.Add(node)) continue;
                order.Add(node);

                foreach (var wire in _wires.All)
                {
                    if (wire.Source.Owner != node) continue;
                    var consumer = wire.Target.Owner;
                    if (seen.Contains(consumer)) continue;
                    if (!inDegree.ContainsKey(consumer)) continue;
                    if (--inDegree[consumer] == 0) queue.Enqueue(consumer);
                }
            }

            return order;
        }

        private sealed class EvaluationContext : IEvaluationContext
        {
            private readonly IWireSystem _wires;
            private readonly IValueSystem _values;
            private readonly INodeErrorSystem _errors;

            public NodeId Self { get; }

            public EvaluationContext(NodeId self, IWireSystem wires, IValueSystem values, INodeErrorSystem errors)
            {
                Self = self;
                _wires = wires;
                _values = values;
                _errors = errors;
            }

            public T? Read<T>(string inputName)
            {
                var pin = new PinId(Self, inputName, PinDirection.Input);
                var src = _wires.SourceOf(pin);
                if (src is null) return default;
                var raw = _values.Get(src.Value);
                return raw is T t ? t : default;
            }

            public void Write<T>(string outputName, T value) =>
                _values.Set(new PinId(Self, outputName, PinDirection.Output), value);

            public void Error(string message, Exception? exception = null) =>
                _errors.Add(Self, message, exception);
        }
    }
}
