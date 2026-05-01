// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Contracts.Graph
{
    /// <summary>
    /// A node in the data-flow graph. Plugin authors usually inherit
    /// <c>Metrician.Core.NodeBase</c> rather than implementing this directly.
    /// Implement <see cref="IDisposable"/> if the node needs disposing.
    /// </summary>
    public interface INode
    {
        string Title { get; }

        /// <summary>
        /// Author name shown in the node footer as "by &lt;Vendor&gt;".
        /// </summary>
        string Vendor { get; }

        IReadOnlyList<INodeInput> Inputs { get; }

        IReadOnlyList<INodeOutput> Outputs { get; }

        /// <summary>
        /// Re-compute outputs from current inputs.
        /// </summary>
        void Evaluate();
    }
}
