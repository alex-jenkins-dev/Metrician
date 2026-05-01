// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Drawing.Drawing2D;
using Metrician.Core;
using Metrician.Contracts.Graph;

namespace Metrician.Graph
{
    /// <summary>
    /// WinForms editor for a <see cref="NodeGraph"/>. Drag pin-to-pin to wire;
    /// click an input pin to disconnect; drag a node to move; right-click for
    /// the canvas or node menu; right-drag to pan; wheel zooms around the cursor.
    /// </summary>
    public class NodeGraphControl : UserControl
    {
        public NodeGraph Graph { get; }
        public NodeGraphTheme Theme { get; }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public IValueConverterRegistry? Converters { get; set; }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public WireConversions Conversions { get; set; } = new();

        private readonly NodePainter _painter;
        private readonly NodeGraphContextMenuBuilder _menuBuilder;

        // Screen = canvas * _zoom + _pan, with pan in screen pixels.
        private PointF _pan = PointF.Empty;
        private float  _zoom = 1f;
        private const float MinZoom = 0.2f;
        private const float MaxZoom = 4f;

        // Right-button click-vs-drag: defer the menu to right-up and only show
        // it when the cursor barely moved, so a pan-drag does not flash a menu.
        private const int  PanDragThresholdPx = 4;
        private bool   _rightButtonDown;
        private bool   _isPanning;
        private Point  _rightDownAt;
        private PointF _panStart;

        private INodeLayout? _draggedNode;
        private PointF       _dragOffset;
        private INodeOutput? _wireSource;
        private PointF       _wireEnd;
        private INode?       _selectedNode;

        public INode? SelectedNode
        {
            get => _selectedNode;
            private set
            {
                if (_selectedNode == value) return;
                _selectedNode = value;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        public event EventHandler? SelectionChanged;

        /// <summary>
        /// Raised after a wire change or node move so the host can re-evaluate.
        /// </summary>
        public event EventHandler? GraphChanged;

        /// <summary>
        /// Raised when the user picks Save Graph; the host owns the file dialog and serialisation.
        /// </summary>
        public event EventHandler? SaveGraphRequested;

        /// <summary>
        /// Raised when the user picks Load Graph; the host should clear then add the loaded nodes.
        /// </summary>
        public event EventHandler? LoadGraphRequested;

        /// <summary>
        /// Raised when the user picks Append Graph; the host merges loaded nodes into the existing graph.
        /// </summary>
        public event EventHandler? AppendGraphRequested;

        /// <summary>
        /// Node types offered in the right-click Add submenu. Changes apply on the next right-click.
        /// </summary>
        public IList<NodeMenuEntry> AvailableNodes { get; } = new List<NodeMenuEntry>();

        public NodeGraphControl(NodeGraph graph)
            : this(graph, NodeGraphTheme.Dark) { }

        public NodeGraphControl(NodeGraph graph, NodeGraphTheme theme)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            Theme = theme ?? throw new ArgumentNullException(nameof(theme));
            _painter = new NodePainter(theme);
            _menuBuilder = new NodeGraphContextMenuBuilder(theme, AvailableNodes);

            BackColor = theme.Background;
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var savedState = g.Save();
            try
            {
                g.TranslateTransform(_pan.X, _pan.Y);
                g.ScaleTransform(_zoom, _zoom);

                foreach (var node in Graph.Nodes)
                    foreach (var input in node.Inputs)
                        if (input.Source != null)
                            NodePainter.DrawWire(g,
                                NodeGeometry.GetPinCanvasPos(input.Source, Theme),
                                NodeGeometry.GetPinCanvasPos(input, Theme),
                                Conversions.IsConverted(input) ? Theme.WireConverted : Theme.Wire);

                foreach (var node in Graph.Nodes)
                    _painter.DrawNode(g, node, ReferenceEquals(node, _selectedNode));

                if (_wireSource != null)
                    NodePainter.DrawWire(g,
                        NodeGeometry.GetPinCanvasPos(_wireSource, Theme),
                        _wireEnd,
                        Theme.WireDrag);
            }
            finally
            {
                g.Restore(savedState);
            }
        }

        private PointF ScreenToCanvas(Point screen) =>
            new PointF((screen.X - _pan.X) / _zoom, (screen.Y - _pan.Y) / _zoom);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Right)
            {
                _rightButtonDown = true;
                _isPanning = false;
                _rightDownAt = e.Location;
                _panStart = _pan;
                Capture = true;
                return;
            }
            if (e.Button != MouseButtons.Left) return;

            PointF canvasPt = ScreenToCanvas(e.Location);

            var outPin = NodeGraphHitTest.FindOutputPinAt(Graph, canvasPt, Theme);
            if (outPin != null)
            {
                _wireSource = outPin;
                _wireEnd = canvasPt;
                Capture = true;
                Invalidate();
                return;
            }

            var inPin = NodeGraphHitTest.FindInputPinAt(Graph, canvasPt, Theme);
            if (inPin != null)
            {
                if (inPin.Source != null)
                {
                    ValueConverterRegistryExtensions.Disconnect(inPin, Conversions);
                    if (inPin.Owner is IVariadicInputs v) v.CompactInputs();
                    GraphChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
                return;
            }

            var hit = NodeGraphHitTest.FindNodeAt(Graph, canvasPt, Theme);
            if (hit != null)
            {
                SelectedNode = hit;
                if (hit is INodeLayout layout)
                {
                    _draggedNode = layout;
                    _dragOffset = new PointF(
                        canvasPt.X - layout.Position.X,
                        canvasPt.Y - layout.Position.Y);
                }
                Capture = true;
                return;
            }

            SelectedNode = null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_rightButtonDown)
            {
                int dx = e.X - _rightDownAt.X;
                int dy = e.Y - _rightDownAt.Y;
                if (!_isPanning && (Math.Abs(dx) > PanDragThresholdPx || Math.Abs(dy) > PanDragThresholdPx))
                {
                    _isPanning = true;
                    Cursor = Cursors.SizeAll;
                }
                if (_isPanning)
                {
                    _pan = new PointF(_panStart.X + dx, _panStart.Y + dy);
                    Invalidate();
                }
                return;
            }

            if (_draggedNode != null)
            {
                var canvasPt = ScreenToCanvas(e.Location);
                _draggedNode.Position = new PointF(
                    canvasPt.X - _dragOffset.X,
                    canvasPt.Y - _dragOffset.Y);
                Invalidate();
            }
            else if (_wireSource != null)
            {
                _wireEnd = ScreenToCanvas(e.Location);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Right && _rightButtonDown)
            {
                _rightButtonDown = false;
                Capture = false;
                if (_isPanning)
                {
                    _isPanning = false;
                    Cursor = Cursors.Default;
                }
                else
                {
                    ShowContextMenu(e.Location);
                }
                return;
            }

            if (_wireSource != null)
            {
                var canvasPt = ScreenToCanvas(e.Location);
                var target = NodeGraphHitTest.FindInputPinAt(Graph, canvasPt, Theme);

                if (target != null && Converters.TryWire(target, _wireSource, Conversions))
                {
                    if (target.Owner is IVariadicInputs v) v.CompactInputs();
                    GraphChanged?.Invoke(this, EventArgs.Empty);
                }

                _wireSource = null;
                Capture = false;
                Invalidate();
            }

            if (_draggedNode != null)
            {
                _draggedNode = null;
                GraphChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            float steps = e.Delta / 120f;
            float factor = MathF.Pow(1.2f, steps);
            float oldZoom = _zoom;
            float newZoom = Math.Clamp(oldZoom * factor, MinZoom, MaxZoom);
            if (Math.Abs(newZoom - oldZoom) < 1e-6f) return;

            float ratio = newZoom / oldZoom;
            _pan = new PointF(
                e.X - (e.X - _pan.X) * ratio,
                e.Y - (e.Y - _pan.Y) * ratio);
            _zoom = newZoom;
            Invalidate();
        }

        private void ShowContextMenu(Point clickLocation)
        {
            PointF canvasPt = ScreenToCanvas(clickLocation);
            var hit = NodeGraphHitTest.FindNodeAt(Graph, canvasPt, Theme);

            ContextMenuStrip menu;
            if (hit != null)
            {
                SelectedNode = hit;
                menu = _menuBuilder.BuildNodeMenu(hit, DeleteNode);
            }
            else
            {
                menu = _menuBuilder.BuildCanvasMenu(
                    canvasPt,
                    onAddNode: AddNodeAt,
                    onAutoLayout: AutoLayout,
                    onClear: ClearGraph,
                    onSaveGraph:   () => SaveGraphRequested?.Invoke(this, EventArgs.Empty),
                    onLoadGraph:   () => LoadGraphRequested?.Invoke(this, EventArgs.Empty),
                    onAppendGraph: () => AppendGraphRequested?.Invoke(this, EventArgs.Empty),
                    hasNodes: Graph.Nodes.Count > 0);
            }
            menu.Show(this, clickLocation);
        }

        /// <summary>
        /// Adds a pre-wired batch of nodes and runs <see cref="AutoLayout"/>.
        /// See <see cref="AddNodes"/> when positions are pre-set.
        /// </summary>
        public void AddNodesWithLayout(IReadOnlyList<INode> nodes)
        {
            if (nodes is null || nodes.Count == 0) return;
            Graph.AddNodesWithLayout(nodes, Theme);
            GraphChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        /// <summary>
        /// Rearranges every node into a left-to-right layered layout.
        /// </summary>
        public void AutoLayout()
        {
            Graph.SugiyamaLayout(Theme);
            GraphChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        private void AddNodeAt(INode node, PointF canvasAt)
        {
            Graph.AddNodeAt(node, canvasAt);
            SelectedNode = node;
            GraphChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        private void DeleteNode(INode node)
        {
            Graph.DeleteNode(node, Conversions);
            if (ReferenceEquals(SelectedNode, node))
                SelectedNode = null;
            GraphChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        public void ClearGraph()
        {
            Graph.Nodes.Clear();
            SelectedNode = null;
            GraphChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        /// <summary>
        /// Adds a pre-wired batch of nodes without running layout, honouring
        /// each node's existing <see cref="INodeLayout.Position"/>.
        /// </summary>
        public void AddNodes(IReadOnlyList<INode> nodes)
        {
            if (nodes is null || nodes.Count == 0) return;
            foreach (var node in nodes) Graph.Nodes.Add(node);
            GraphChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }
}
