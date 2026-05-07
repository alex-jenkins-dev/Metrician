// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IValidationAuthor
    {
        void OnValidate(Func<INodeAuthor, IReadOnlyList<string>> validate);
        void ClearValidator();
    }
}
