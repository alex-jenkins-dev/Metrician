// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Presentation.Graph
{
    public sealed class GraphWorkspaceControl : UserControl
    {
        private const float DefaultGraphFraction = 0.80f;
        private const int MinPanelWidth = 80;

        private readonly SplitContainer _split;
        private bool _initialSplitApplied;

        public GraphControl Graph { get; }
        public PropertyPane Properties { get; }
        public GraphTheme Theme { get; }

        public GraphWorkspaceControl(GraphControl graph, GraphTheme theme)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Theme = theme ?? throw new ArgumentNullException(nameof(theme));

            BackColor = theme.Background;
            ForeColor = theme.Text;

            Properties = new PropertyPane(graph.World, graph.Presenter, theme) { Dock = DockStyle.Fill };

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                FixedPanel = FixedPanel.Panel2,
                BackColor = theme.MenuBorder,
                Panel1MinSize = MinPanelWidth,
                Panel2MinSize = MinPanelWidth,
                SplitterWidth = 4,
            };

            graph.Dock = DockStyle.Fill;
            _split.Panel1.BackColor = theme.Background;
            _split.Panel2.BackColor = theme.Background;
            _split.Panel1.Controls.Add(graph);
            _split.Panel2.Controls.Add(Properties);

            Controls.Add(_split);

            graph.Presenter.SelectionChanged += (_, id) => Properties.ShowFor(id);
            graph.Presenter.PinSelectionChanged += (_, pin) => Properties.ShowForPin(pin);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (_initialSplitApplied) return;
            if (_split.Width <= MinPanelWidth * 2) return;
            int distance = (int)(_split.Width * DefaultGraphFraction);
            _split.SplitterDistance = Math.Clamp(
                distance, MinPanelWidth, _split.Width - MinPanelWidth);
            _initialSplitApplied = true;
        }
    }
}
