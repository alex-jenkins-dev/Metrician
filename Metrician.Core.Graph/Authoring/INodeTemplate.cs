// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface INodeTemplate
    {
        string Title { get; }
        string Vendor { get; }
        string Description { get; }

        void Configure(INodeAuthor author);
    }

    public enum WireChangeKind { Connected, Disconnected }

    public sealed record WireChange(WireChangeKind Kind, Wire Wire);

    public interface IDynamicHandle
    {
        void RequestRefresh();
    }
}
