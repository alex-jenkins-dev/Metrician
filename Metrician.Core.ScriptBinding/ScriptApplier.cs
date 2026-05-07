// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;
using Metrician.Core.Scripting;

namespace Metrician.Core.ScriptBinding
{
    public static class ScriptApplier
    {
        public const string PositionPropertyName = "Position";

        public static IReadOnlyDictionary<string, NodeId> Apply(
            IGraphWorld world,
            Script script,
            INodeTemplateRegistry templates,
            ITemplateNameSystem? templateNames = null,
            ScriptApplyMode mode = ScriptApplyMode.Replace)
        {
            if (world is null) throw new ArgumentNullException(nameof(world));
            if (script is null) throw new ArgumentNullException(nameof(script));
            if (templates is null) throw new ArgumentNullException(nameof(templates));

            if (mode == ScriptApplyMode.Replace)
                ClearWorld(world);

            var byScriptId = new Dictionary<string, NodeId>(StringComparer.Ordinal);

            foreach (var declaration in script.Nodes)
            {
                if (byScriptId.ContainsKey(declaration.Id))
                    throw new ScriptException(
                        $"Node id '{declaration.Id}' already defined.");

                var template = templates.Create(declaration.TypeName)
                    ?? throw new ScriptException(
                        $"Unknown node type '{declaration.TypeName}'.");

                NodeId nodeId;
                try { nodeId = world.Add(template); }
                catch (Exception ex)
                {
                    throw new ScriptException(
                        $"Failed to add node '{declaration.Id}' of type '{declaration.TypeName}': {ex.Message}");
                }

                byScriptId[declaration.Id] = nodeId;
                templateNames?.Set(nodeId, declaration.TypeName);
            }

            foreach (var assignment in script.Properties)
            {
                if (!byScriptId.TryGetValue(assignment.NodeId, out var nodeId))
                    throw new ScriptException(
                        $"Unknown node id '{assignment.NodeId}'.");

                ApplyProperty(world, nodeId, assignment);
            }

            foreach (var connection in script.Connections)
            {
                if (!byScriptId.TryGetValue(connection.SourceNodeId, out var source))
                    throw new ScriptException(
                        $"Unknown source node id '{connection.SourceNodeId}'.");
                if (!byScriptId.TryGetValue(connection.TargetNodeId, out var target))
                    throw new ScriptException(
                        $"Unknown target node id '{connection.TargetNodeId}'.");

                var sourcePin = ResolvePin(
                    world, source, connection.SourcePinIndex, PinDirection.Output,
                    connection.SourceNodeId);
                var targetPin = ResolvePin(
                    world, target, connection.TargetPinIndex, PinDirection.Input,
                    connection.TargetNodeId);

                if (!world.Wires.TryConnect(sourcePin, targetPin))
                    throw new ScriptException(
                        $"Could not connect '{connection.SourceNodeId}.{connection.SourcePinIndex}' " +
                        $"to '{connection.TargetNodeId}.{connection.TargetPinIndex}'.");
            }

            return byScriptId;
        }

        private static PinId ResolvePin(
            IGraphWorld world,
            NodeId nodeId,
            int pinIndex,
            PinDirection direction,
            string scriptNodeId)
        {
            var pins = (direction == PinDirection.Output
                ? world.Pins.Outputs(nodeId)
                : world.Pins.Inputs(nodeId)).ToList();

            string directionLabel = direction == PinDirection.Output ? "output" : "input";

            if (pinIndex < 0 || pinIndex >= pins.Count)
                throw new ScriptException(
                    $"{directionLabel} pin index {pinIndex} out of range on '{scriptNodeId}' " +
                    $"(has {pins.Count}).");
            return pins[pinIndex].Id;
        }

        private static void ClearWorld(IGraphWorld world)
        {
            foreach (var node in world.Nodes.All.ToList())
                world.Remove(node.Id);
        }

        private static void ApplyProperty(
            IGraphWorld world, NodeId nodeId, PropertyAssignment assignment)
        {
            if (string.Equals(
                    assignment.PropertyName,
                    PositionPropertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                Vector2 position;
                try { position = (Vector2)PropertyValueText.Parse(assignment.Value, typeof(Vector2))!; }
                catch (Exception ex)
                {
                    throw new ScriptException(
                        $"Cannot parse Position '{assignment.Value}': {ex.Message}");
                }
                world.Layout.Set(nodeId, position);
                return;
            }

            var property = world.Properties.PropertiesOf(nodeId).FirstOrDefault(p =>
                string.Equals(p.Name, assignment.PropertyName, StringComparison.OrdinalIgnoreCase));
            if (property is null)
                throw new ScriptException(
                    $"Property '{assignment.PropertyName}' is not defined on node '{assignment.NodeId}'.");

            object? parsed;
            try { parsed = PropertyValueText.Parse(assignment.Value, property.Type); }
            catch (Exception ex)
            {
                throw new ScriptException(
                    $"Cannot assign '{assignment.Value}' to {property.Type.Name} " +
                    $"property '{assignment.PropertyName}': {ex.Message}");
            }
            world.Properties.Set(nodeId, property.Name, parsed);
        }
    }
}
