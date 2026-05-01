// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public readonly record struct NodeId(Guid Value)
    {
        public static NodeId New() => new(Guid.NewGuid());
        public override string ToString() => Value.ToString("N")[..8];
    }

    public sealed record Node(NodeId Id, string Title, string Vendor, string Description);

    public enum PinDirection { Input, Output }

    public readonly record struct PinId(NodeId Owner, string Name, PinDirection Direction);

    public sealed record Pin(PinId Id, Type ValueType);

    public sealed record Wire(PinId Source, PinId Target);
}
