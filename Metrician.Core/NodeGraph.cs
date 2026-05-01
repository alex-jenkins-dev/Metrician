// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Contracts.Graph;

namespace Metrician.Core
{
    /// <summary>
    /// Nodes wired by input-pin sources. <see cref="Evaluate"/>
    /// runs in topological order; cycles are skipped.
    /// </summary>
    public sealed class NodeGraph
    {
        public IList<INode> Nodes { get; } = new List<INode>();

        public void Evaluate()
        {
            var inDegree = new Dictionary<INode, int>(Nodes.Count);
            foreach (var n in Nodes) inDegree[n] = 0;
            foreach (var n in Nodes)
                foreach (var inPin in n.Inputs)
                    if (inPin.Source != null && inDegree.ContainsKey(inPin.Source.Owner))
                        inDegree[n]++;

            var queue = new Queue<INode>();
            foreach (var kv in inDegree) if (kv.Value == 0) queue.Enqueue(kv.Key);

            var processed = new HashSet<INode>();
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!processed.Add(node)) continue;
                node.Evaluate();

                foreach (var consumer in Nodes)
                {
                    if (processed.Contains(consumer)) continue;
                    foreach (var inPin in consumer.Inputs)
                    {
                        if (inPin.Source?.Owner == node)
                        {
                            inDegree[consumer]--;
                            if (inDegree[consumer] == 0) queue.Enqueue(consumer);
                        }
                    }
                }
            }
        }
    }
}
