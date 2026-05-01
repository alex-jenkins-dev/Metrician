// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface ITagAuthor
    {
        IReadOnlyCollection<string> All { get; }

        void Add(string tag);
        void Remove(string tag);
        bool Has(string tag);
    }
}
