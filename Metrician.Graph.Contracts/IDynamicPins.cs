// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Graph.Contracts
{
    /// <summary>
    /// Nodes whose pin layout depends on properties rather than wire state.
    /// The host calls <see cref="RebuildPins"/> after a property change; implementations should
    /// preserve existing pin instances where possible so downstream wires survive.
    /// </summary>
    public interface IDynamicPins
    {
        void RebuildPins();
    }
}
