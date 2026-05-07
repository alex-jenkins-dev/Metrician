// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Core.Graph
{
    public interface IPinAuthor
    {
        IReadOnlyList<Pin> Inputs { get; }
        IReadOnlyList<Pin> Outputs { get; }

        Pin AddInput<T>(string name);
        Pin AddOutput<T>(string name);
        void RemoveInput(string name);
        void RemoveOutput(string name);

        bool IsConnected(PinId pin);
        PinId? SourceOf(PinId target);
        bool TryConnect(PinId source, PinId target);
        void Disconnect(PinId target);

        void Constrain(PinId pin, Func<bool, string?> validator);
        void Unconstrain(PinId pin);

        void Colour(PinId pin, PinColour colour);
        void ClearColour(PinId pin);

        void Group(PinId pin, string group);
        void ClearGroup(PinId pin);

        void OnConnectionChanged(Action<WireChange> handler);
    }
}
