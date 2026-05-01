// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Core.ScriptBinding
{
    public interface ITemplateNameSystem
    {
        void Set(NodeId id, string typeName);
        string? Get(NodeId id);
        void Clear(NodeId id);
    }
}
