// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    /// <summary>
    /// Manages a variadic input set on an authored node.
    /// Pins are named "{prefix}0", "{prefix}1", ... in insertion order, with exactly
    /// one trailing unwired spare. After every connection change the set is rebuilt
    /// so numbering stays consecutive from zero, holes are eliminated, and the
    /// spare is regenerated. Existing wires are reattached to the new pin at the
    /// same slot index, so user wires survive the renumbering.
    /// Use only on nodes whose entire input set is variadic.
    /// </summary>
    public static class VariadicInputs
    {
        public static void Configure<T>(INodeAuthor a, string prefix)
        {
            if (a.Pins.Inputs.Count == 0)
                a.Pins.AddInput<T>($"{prefix}0");

            bool rebuilding = false;
            a.Pins.OnConnectionChanged(change =>
            {
                if (rebuilding) return;
                if (change.Wire.Target.Owner != a.NodeId) return;
                rebuilding = true;
                try { Rebuild<T>(a, prefix); }
                finally { rebuilding = false; }
            });
        }

        private static void Rebuild<T>(INodeAuthor a, string prefix)
        {
            var current = a.Pins.Inputs.ToList();
            var sources = new List<PinId>(current.Count);
            foreach (var pin in current)
            {
                if (a.Pins.SourceOf(pin.Id) is { } src)
                    sources.Add(src);
            }

            foreach (var pin in current)
                a.Pins.RemoveInput(pin.Id.Name);

            for (int i = 0; i <= sources.Count; i++)
                a.Pins.AddInput<T>($"{prefix}{i}");

            for (int i = 0; i < sources.Count; i++)
                a.Pins.TryConnect(
                    sources[i],
                    new PinId(a.NodeId, $"{prefix}{i}", PinDirection.Input));
        }
    }
}
