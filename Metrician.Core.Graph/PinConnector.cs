// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IPinConnector
    {
        bool TryConnect(PinId source, PinId target);
        bool CanConnect(PinId source, PinId target);
    }

    public sealed class PinConnector : IPinConnector
    {
        private readonly IPinSystem _pins;
        private readonly IWireSystem _wires;
        private readonly IValueConverterRegistry _converters;
        private readonly IConversionSystem _conversions;

        public PinConnector(
            IPinSystem pins,
            IWireSystem wires,
            IValueConverterRegistry converters,
            IConversionSystem conversions)
        {
            _pins = pins ?? throw new ArgumentNullException(nameof(pins));
            _wires = wires ?? throw new ArgumentNullException(nameof(wires));
            _converters = converters ?? throw new ArgumentNullException(nameof(converters));
            _conversions = conversions ?? throw new ArgumentNullException(nameof(conversions));
        }

        public bool CanConnect(PinId source, PinId target)
        {
            if (source.Direction != PinDirection.Output) return false;
            if (target.Direction != PinDirection.Input) return false;
            var srcPin = _pins.Get(source);
            var tgtPin = _pins.Get(target);
            if (srcPin is null || tgtPin is null) return false;
            if (tgtPin.ValueType.IsAssignableFrom(srcPin.ValueType)) return true;
            return _converters.TryGet(srcPin.ValueType, tgtPin.ValueType, out _);
        }

        public bool TryConnect(PinId source, PinId target)
        {
            if (source.Direction != PinDirection.Output) return false;
            if (target.Direction != PinDirection.Input) return false;

            var srcPin = _pins.Get(source);
            var tgtPin = _pins.Get(target);
            if (srcPin is null || tgtPin is null) return false;

            bool direct = tgtPin.ValueType.IsAssignableFrom(srcPin.ValueType);
            bool needsConversion = !direct &&
                _converters.TryGet(srcPin.ValueType, tgtPin.ValueType, out _);

            if (!direct && !needsConversion) return false;
            if (!_wires.TryConnect(source, target)) return false;

            if (needsConversion) _conversions.Mark(target);
            else _conversions.Clear(target);
            return true;
        }
    }
}
