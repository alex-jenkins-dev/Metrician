// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Script
{
    /// <summary>
    /// Parsed Metrician graph script: ordered lists of node declarations,
    /// property assignments, and pin connections. Order is part of the
    /// contract; variadic-input nodes depend on connection order for pin
    /// indices after auto-compaction.
    /// </summary>
    public sealed class GraphScript
    {
        public List<NodeDecl> Nodes { get; } = new();
        public List<PropertyAssignment> Properties { get; } = new();
        public List<Connection> Connections { get; } = new();

        /// <summary>
        /// True if any assignment targets <c>Position</c>; callers use this to decide whether to auto-layout.
        /// </summary>
        public bool HasPositions => Properties.Any(p =>
            string.Equals(p.PropertyName, "Position", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>An <c>id = TypeName</c> statement.</summary>
    public sealed record NodeDecl(
        string Id, string TypeName);

    /// <summary>
    /// An <c>id.PropertyName = value</c> statement.
    /// Value is the raw token; coercion happens at apply time.
    /// </summary>
    public sealed record PropertyAssignment(
        string NodeId, string PropertyName, string Value);

    /// <summary>
    /// A <c>src.outPin -&gt; dst.inPin</c> statement.
    /// Pin refs are a name or an integer index.
    /// </summary>
    public sealed record Connection(
        string SourceId, string SourcePin, string TargetId, string TargetPin);
}
