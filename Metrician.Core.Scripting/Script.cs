// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Scripting
{
    public sealed class Script
    {
        public List<NodeDeclaration> Nodes { get; } = new();
        public List<PropertyAssignment> Properties { get; } = new();
        public List<Connection> Connections { get; } = new();
    }
}
