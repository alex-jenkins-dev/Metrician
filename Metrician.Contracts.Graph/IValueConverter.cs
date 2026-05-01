// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Contracts.Graph
{
    public interface IValueConverter<in TFrom, out TTo>
    {
        TTo Convert(TFrom value);
    }

    public interface IValueConverterRegistry
    {
        void Register<TFrom, TTo>(IValueConverter<TFrom, TTo> converter);

        bool TryGet(Type fromType, Type toType, out Func<object?, object?>? converter);
    }

    public static class ValueConverterRegistryExtensions
    {
        public static bool TryWire(
            this IValueConverterRegistry? registry,
            INodeInput input,
            INodeOutput output,
            WireConversions? conversions = null)
        {
            if (input.TryConnect(new DirectValueProvider(output)))
            {
                conversions?.Clear(input);
                return true;
            }
            if (registry != null &&
                registry.TryGet(output.ValueType, input.ValueType, out var converter))
            {
                var provider = new ConvertedValueProvider(output, input.ValueType, converter!);
                if (input.TryConnect(provider))
                {
                    conversions?.Mark(input);
                    return true;
                }
            }
            return false;
        }

        public static void Disconnect(INodeInput input, WireConversions? conversions = null)
        {
            input.TryConnect(null);
            conversions?.Clear(input);
        }
    }
}
