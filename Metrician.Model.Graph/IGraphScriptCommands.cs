// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;

namespace Metrician.Model.Graph
{
    public interface IGraphScriptCommands
    {
        void Save();
        void LoadReplace();
        void LoadAppend(Vector2 anchor);
    }
}
