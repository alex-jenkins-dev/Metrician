// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Graph.Contracts
{
    public interface IValueProvider
    {
        INodeOutput Source { get; }

        Type OutputType { get; }

        object? GetValue();
    }

    public sealed class DirectValueProvider : IValueProvider
    {
        public INodeOutput Source { get; }
        public Type OutputType => Source.ValueType;

        public DirectValueProvider(INodeOutput source) => Source = source;

        public object? GetValue() => Source.Value;
    }

    public sealed class ConvertedValueProvider : IValueProvider
    {
        private readonly Func<object?, object?> _converter;

        public INodeOutput Source { get; }
        public Type OutputType { get; }

        public ConvertedValueProvider(
            INodeOutput source,
            Type outputType,
            Func<object?, object?> converter)
        {
            Source = source;
            OutputType = outputType;
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));
        }

        public object? GetValue() => _converter(Source.Value);
    }
}
