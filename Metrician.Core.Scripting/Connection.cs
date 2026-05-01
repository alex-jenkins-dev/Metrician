// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Scripting
{
    public sealed record Connection(
        string SourceNodeId,
        int SourcePinIndex,
        string TargetNodeId,
        int TargetPinIndex,
        string? Comment = null);
}
