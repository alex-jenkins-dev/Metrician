// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph.Contracts;

namespace Metrician.Graph
{
    public sealed class NodeMenuEntry
    {
        public string Label { get; }
        public Func<INode> Factory { get; }

        public string Vendor { get; init; } = "";

        /// <summary>
        /// Source assembly name used as a sub-group within a vendor.
        /// </summary>
        public string Source { get; init; } = "";

        /// <summary>
        /// If true, the entry sits directly under Add, skipping vendor grouping.
        /// </summary>
        public bool Pinned { get; init; } = false;

        public NodeMenuEntry(string label, Func<INode> factory)
        {
            Label = label;
            Factory = factory;
        }
    }
}
