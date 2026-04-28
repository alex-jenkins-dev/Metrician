// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core;
using Metrician.Graph;

namespace Metrician.App
{
    /// <summary>
    /// Session state that outlives any one window: graph, registry, and the
    /// persistent graph-editor controls. Controls migrate between forms via
    /// reparenting and are disposed only at app exit.
    /// </summary>
    internal sealed class SessionState
    {
        public RenderableRegistry Registry { get; } = new();
        public NodeGraph Graph { get; } = new();
        public NodeGraphControl GraphControl { get; }
        public PropertyGrid PropertyGrid { get; }
        public SplitContainer GraphHost { get; }

        public SessionState()
        {
            GraphControl = new NodeGraphControl(Graph) { Dock = DockStyle.Fill };

            PropertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 35),
                ViewBackColor = Color.FromArgb(40, 40, 45),
                ViewForeColor = Color.FromArgb(220, 220, 220),
                LineColor = Color.FromArgb(63, 63, 70),
                CategoryForeColor = Color.FromArgb(220, 220, 220),
                HelpBackColor = Color.FromArgb(40, 40, 45),
                HelpForeColor = Color.FromArgb(220, 220, 220),
                CommandsBackColor = Color.FromArgb(30, 30, 35),
                CommandsForeColor = Color.FromArgb(220, 220, 220),
                ToolbarVisible = false,
            };

            GraphHost = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                BackColor = Color.FromArgb(30, 30, 35),
            };
            GraphHost.Panel1.Controls.Add(GraphControl);
            GraphHost.Panel2.Controls.Add(PropertyGrid);

            GraphHost.SplitterDistance = (int)(GraphHost.Width * 0.75);
        }
    }
}
