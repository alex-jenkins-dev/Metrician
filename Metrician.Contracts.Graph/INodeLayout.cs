// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing;

namespace Metrician.Contracts.Graph
{
    /// <summary>
    /// Optional contract for nodes that expose a canvas-space position to the
    /// graph editor. Plugin authors get this for free via <c>NodeBase</c>;
    /// nodes that implement <see cref="INode"/> directly can opt in to be
    /// positionable rather than fixed at the origin.
    /// </summary>
    public interface INodeLayout
    {
        PointF Position { get; set; }
    }
}
