// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Script
{
    /// <summary>
    /// Thrown when a script fails to parse or apply.
    /// <see cref="LineNumber"/> is 1-based; 0 means no specific line.
    /// </summary>
    public sealed class ScriptException : Exception
    {
        public int LineNumber { get; }

        public ScriptException(string message, int lineNumber = 0) : base(message)
        {
            LineNumber = lineNumber;
        }
    }
}
