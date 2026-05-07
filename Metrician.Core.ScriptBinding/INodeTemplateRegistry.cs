// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.Core.ScriptBinding
{
    public interface INodeTemplateRegistry
    {
        IEnumerable<string> Names { get; }

        INodeTemplate? Create(string typeName);
    }
}
