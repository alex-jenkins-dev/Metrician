// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Globalization;
using System.Text;

namespace Metrician.Core.Scripting
{
    public static class ScriptFormatter
    {
        public static string Format(Script script)
        {
            var output = new StringBuilder();

            foreach (var node in script.Nodes)
                output.Append(node.Id).Append(" = ").AppendLine(node.TypeName);

            if (script.Properties.Count > 0)
            {
                if (output.Length > 0) output.AppendLine();
                foreach (var property in script.Properties)
                    output.Append(property.NodeId).Append('.').Append(property.PropertyName)
                          .Append(" = ").AppendLine(property.Value);
            }

            if (script.Connections.Count > 0)
            {
                if (output.Length > 0) output.AppendLine();
                foreach (var connection in script.Connections)
                {
                    output.Append(connection.SourceNodeId).Append('.')
                          .Append(connection.SourcePinIndex.ToString(CultureInfo.InvariantCulture))
                          .Append(" -> ")
                          .Append(connection.TargetNodeId).Append('.')
                          .Append(connection.TargetPinIndex.ToString(CultureInfo.InvariantCulture));
                    if (!string.IsNullOrWhiteSpace(connection.Comment))
                        output.Append(" // ").Append(connection.Comment);
                    output.AppendLine();
                }
            }

            return output.ToString();
        }
    }
}
