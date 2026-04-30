// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph.Contracts;

namespace Metrician.Core
{
    public sealed class ValueConverterRegistry : IValueConverterRegistry
    {
        private readonly Dictionary<(Type From, Type To), Func<object?, object?>> _converters = new();

        public void Register<TFrom, TTo>(IValueConverter<TFrom, TTo> converter)
        {
            if (converter is null) throw new ArgumentNullException(nameof(converter));
            _converters[(typeof(TFrom), typeof(TTo))] =
                obj => converter.Convert((TFrom)obj!);
        }

        public bool TryGet(Type fromType, Type toType, out Func<object?, object?>? converter)
        {
            if (_converters.TryGetValue((fromType, toType), out converter))
                return true;

            foreach (var kv in _converters)
            {
                if (kv.Key.From.IsAssignableFrom(fromType) &&
                    toType.IsAssignableFrom(kv.Key.To))
                {
                    converter = kv.Value;
                    return true;
                }
            }
            converter = null;
            return false;
        }
    }
}
