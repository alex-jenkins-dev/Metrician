// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface INodeAuthor
    {
        NodeId NodeId { get; }

        IPinAuthor Pins { get; }
        IPropertyAuthor Properties { get; }
        IBehaviourAuthor Behaviour { get; }
        IValidationAuthor Validation { get; }
        ITagAuthor Tags { get; }
    }
}
