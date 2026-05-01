// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Model.Graph
{
    public sealed class GraphPresenter
    {
        private const float MinZoom = 0.2f;
        private const float MaxZoom = 4f;
        private const float PanDragThresholdPx = 4f;

        private readonly IGraphWorld _world;
        private readonly LayoutMetrics _metrics;

        private Vector2 _pan = Vector2.Zero;
        private float _zoom = 1f;
        private NodeId? _selected;
        private InteractionState _state = new InteractionState.Idle();

        public GraphPresenter(IGraphWorld world, LayoutMetrics? metrics = null)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _metrics = metrics ?? LayoutMetrics.Default;

            _world.Pins.Added += OnWorldChanged;
            _world.Pins.Removed += OnWorldChanged;
            _world.Wires.Connected += OnWorldChanged;
            _world.Wires.Disconnected += OnWorldChanged;
            _world.Nodes.Added += OnWorldChanged;
            _world.Nodes.Removed += OnWorldChanged;
            _world.Layout.Changed += OnWorldChanged;
            _world.PinColours.Changed += OnWorldChanged;
        }

        public IGraphWorld World => _world;
        public LayoutMetrics Metrics => _metrics;
        public Vector2 Pan => _pan;
        public float Zoom => _zoom;
        public NodeId? SelectedNode => _selected;
        public InteractionState State => _state;

        public event EventHandler? ViewChanged;
        public event EventHandler<NodeId?>? SelectionChanged;
        public event EventHandler<Vector2>? ContextMenuRequested;

        public Vector2 ScreenToCanvas(Vector2 screen) =>
            new((screen.X - _pan.X) / _zoom, (screen.Y - _pan.Y) / _zoom);

        public Vector2 CanvasToScreen(Vector2 canvas) =>
            new(canvas.X * _zoom + _pan.X, canvas.Y * _zoom + _pan.Y);

        public void OnLeftDown(Vector2 screen)
        {
            var canvas = ScreenToCanvas(screen);

            var outPin = Geometry.OutputPinAt(_world, canvas, _metrics);
            if (outPin is { } src)
            {
                _state = new InteractionState.DraggingWire(src, canvas);
                Raise();
                return;
            }

            var inPin = Geometry.InputPinAt(_world, canvas, _metrics);
            if (inPin is { } tgt)
            {
                if (_world.Wires.SourceOf(tgt) is not null)
                    _world.Wires.Disconnect(tgt);
                return;
            }

            var hit = Geometry.NodeAt(_world, canvas, _metrics);
            if (hit is { } id)
            {
                Select(id);
                var pos = _world.Layout.Get(id) ?? Vector2.Zero;
                _state = new InteractionState.DraggingNode(id, canvas - pos);
                Raise();
                return;
            }

            Select(null);
        }

        public void OnLeftUp(Vector2 screen)
        {
            var canvas = ScreenToCanvas(screen);

            if (_state is InteractionState.DraggingWire dw)
            {
                var target = Geometry.InputPinAt(_world, canvas, _metrics);
                if (target is { } t)
                {
                    var srcPin = _world.Pins.Get(dw.Source);
                    var tgtPin = _world.Pins.Get(t);
                    if (srcPin is not null && tgtPin is not null &&
                        tgtPin.ValueType.IsAssignableFrom(srcPin.ValueType))
                    {
                        _world.Wires.TryConnect(dw.Source, t);
                    }
                }
            }

            _state = new InteractionState.Idle();
            Raise();
        }

        public void OnRightDown(Vector2 screen)
        {
            _state = new InteractionState.AwaitingRightClick(_pan, screen);
            Raise();
        }

        public void OnRightUp(Vector2 screen)
        {
            switch (_state)
            {
                case InteractionState.Panning:
                    _state = new InteractionState.Idle();
                    Raise();
                    break;
                case InteractionState.AwaitingRightClick:
                    _state = new InteractionState.Idle();
                    Raise();
                    ContextMenuRequested?.Invoke(this, screen);
                    break;
            }
        }

        public void OnMove(Vector2 screen)
        {
            switch (_state)
            {
                case InteractionState.AwaitingRightClick aw:
                    var dx = screen.X - aw.ScreenStart.X;
                    var dy = screen.Y - aw.ScreenStart.Y;
                    if (MathF.Abs(dx) > PanDragThresholdPx || MathF.Abs(dy) > PanDragThresholdPx)
                    {
                        _pan = new Vector2(aw.PanStart.X + dx, aw.PanStart.Y + dy);
                        _state = new InteractionState.Panning(aw.PanStart, aw.ScreenStart);
                        Raise();
                    }
                    break;

                case InteractionState.Panning p:
                    _pan = new Vector2(p.PanStart.X + screen.X - p.ScreenStart.X,
                                       p.PanStart.Y + screen.Y - p.ScreenStart.Y);
                    Raise();
                    break;

                case InteractionState.DraggingNode d:
                    var canvas = ScreenToCanvas(screen);
                    _world.Layout.Set(d.Node, canvas - d.OffsetCanvas);
                    break;

                case InteractionState.DraggingWire w:
                    _state = w with { EndCanvas = ScreenToCanvas(screen) };
                    Raise();
                    break;
            }
        }

        public void OnWheel(Vector2 screen, float delta)
        {
            float steps = delta / 120f;
            float factor = MathF.Pow(1.2f, steps);
            float oldZoom = _zoom;
            float newZoom = Math.Clamp(oldZoom * factor, MinZoom, MaxZoom);
            if (MathF.Abs(newZoom - oldZoom) < 1e-6f) return;

            float ratio = newZoom / oldZoom;
            _pan = new Vector2(
                screen.X - (screen.X - _pan.X) * ratio,
                screen.Y - (screen.Y - _pan.Y) * ratio);
            _zoom = newZoom;
            Raise();
        }

        public void Select(NodeId? id)
        {
            if (Nullable.Equals(_selected, id)) return;
            _selected = id;
            SelectionChanged?.Invoke(this, id);
            Raise();
        }

        public event EventHandler<(NodeId Id, INodeTemplate Template)>? NodeSpawned;

        public NodeId Spawn(INodeTemplate template, Vector2 canvasAt)
        {
            if (template is null) throw new ArgumentNullException(nameof(template));
            var id = _world.Add(template);
            _world.Layout.Set(id, canvasAt);
            Select(id);
            NodeSpawned?.Invoke(this, (id, template));
            return id;
        }

        public bool DeleteSelected()
        {
            if (_selected is not { } id) return false;
            Select(null);
            _world.Remove(id);
            return true;
        }

        public void ResetView()
        {
            _pan = Vector2.Zero;
            _zoom = 1f;
            Raise();
        }

        private void OnWorldChanged<T>(object? sender, T e) => Raise();

        private void Raise() => ViewChanged?.Invoke(this, EventArgs.Empty);
    }
}
