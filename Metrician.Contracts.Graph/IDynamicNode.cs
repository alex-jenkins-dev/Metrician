// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Contracts.Graph
{
    /// <summary>
    /// Nodes that produce new output on their own schedule (timers, file watchers, IPC).
    /// <see cref="OutputChanged"/> may fire from any thread.
    /// </summary>
    public interface IDynamicNode
    {
        /// <summary>
        /// Raised when the node has new output and the graph should re-evaluate.
        /// </summary>
        event EventHandler? OutputChanged;
    }
}
