// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Scripting
{
    public sealed class ScriptException : Exception
    {
        public int LineNumber { get; }

        public ScriptException(string message, int lineNumber = 0) : base(message)
        {
            LineNumber = lineNumber;
        }
    }
}
