// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph.Contracts;

namespace Metrician.Script
{
    /// <summary>
    /// A node type a script can instantiate, identified by both short and fully-qualified name.
    /// </summary>
    public sealed class ScriptNodeFactory
    {
        public string ShortName { get; }

        public string FullName { get; }

        public Func<INode> Create { get; }

        public ScriptNodeFactory(
            string shortName, string fullName, Func<INode> create)
        {
            ShortName = shortName;
            FullName = fullName;
            Create = create;
        }
    }
}
