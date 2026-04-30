// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Graph.Contracts
{
    /// <summary>
    /// Side-table of input pins whose wires currently read through a converter.
    /// </summary>
    public sealed class WireConversions
    {
        private readonly HashSet<INodeInput> _converted = new();

        public void Mark(INodeInput input) => _converted.Add(input);

        public void Clear(INodeInput input) => _converted.Remove(input);

        public bool IsConverted(INodeInput input) => _converted.Contains(input);
    }
}
