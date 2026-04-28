// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Graph.Contracts
{
    /// <summary>
    /// Nodes whose input-pin count varies with connection state. The editor calls
    /// <see cref="CompactInputs"/> after every wire change; implementations drop
    /// unconnected inputs and leave exactly one trailing spare for the user.
    /// </summary>
    public interface IVariadicInputs
    {
        void CompactInputs();
    }
}
