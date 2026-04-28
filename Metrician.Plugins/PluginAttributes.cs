// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Plugins
{
    /// <summary>
    /// Excludes a plugin type from auto-discovery.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MetricianPluginExcludeAttribute : Attribute { }

    /// <summary>
    /// Overrides the menu label for an INode plugin when it should differ from
    /// <see cref="Graph.Contracts.INode.Title"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class MetricianNodeMenuAttribute : Attribute
    {
        public string Label { get; }

        public MetricianNodeMenuAttribute(string label)
        {
            Label = label;
        }
    }
}
