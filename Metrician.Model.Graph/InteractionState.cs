// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Model.Graph
{
    public abstract record InteractionState
    {
        public sealed record Idle : InteractionState;

        public sealed record DraggingNode(
            NodeId Node, Vector2 OffsetCanvas) :
            InteractionState;

        public sealed record DraggingWire(
            PinId Source, Vector2 EndCanvas) :
            InteractionState;

        public sealed record AwaitingRightClick(
            Vector2 PanStart, Vector2 ScreenStart) :
            InteractionState;

        public sealed record Panning(
            Vector2 PanStart, Vector2 ScreenStart) :
            InteractionState;
    }
}
