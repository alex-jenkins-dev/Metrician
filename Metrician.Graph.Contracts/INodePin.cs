// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Graph.Contracts
{
    public interface INodePin
    {
        string Name { get; }

        Type ValueType { get; }

        INode Owner { get; }

        bool IsInput { get; }
    }

    /// <summary>
    /// Input pin: accepts a wire from at most one upstream output.
    /// </summary>
    public interface INodeInput : INodePin
    {
        /// <summary>
        /// Connected upstream output, or null when unwired.
        /// </summary>
        INodeOutput? Source { get; }

        /// <summary>
        /// Wires this input through <paramref name="provider"/>; pass null to
        /// disconnect. Returns false if <see cref="IValueProvider.OutputType"/>
        /// is not assignable to the pin's <see cref="INodePin.ValueType"/>.
        /// </summary>
        bool TryConnect(IValueProvider? provider);
    }

    /// <summary>
    /// Output pin. Multiple inputs may wire to the same output.
    /// </summary>
    public interface INodeOutput : INodePin
    {
        /// <summary>
        /// Most recent value produced by the owning node's Evaluate.
        /// </summary>
        object? Value { get; }
    }

    public interface INodeInput<T> : INodeInput
    {
        /// <summary>
        /// Source value cast to T, or default if unwired or incompatible.
        /// </summary>
        T? CurrentValue { get; }
    }

    public interface INodeOutput<T> : INodeOutput
    {
        T? CurrentValue { get; set; }
    }
}
