// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Model.Graph
{
    public enum MouseButton { Left, Right }

    public enum CursorKind { Default, Move }

    public enum IndicatorKind { Status, Dynamic }

    public sealed class MouseInteraction
    {
        private const float MinZoom = 0.2f;
        private const float MaxZoom = 4f;
        private const float PanDragThresholdPx = 4f;

        private readonly IGraphWorld _world;
        private readonly GraphPresenter _presenter;

        private InteractionState _state = new InteractionState.Idle();
        private InteractionState _lastNotifiedState = new InteractionState.Idle();
        private Vector2 _lastScreen;
        private NodeId? _hoveredNode;
        private IndicatorKind? _hoveredIndicator;
        private CursorKind _cursor = CursorKind.Default;
        private string? _tooltip;

        public MouseInteraction(IGraphWorld world, GraphPresenter presenter)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _presenter.ViewChanged += (_, _) => RefreshOverlay();
        }

        public InteractionState State => _state;
        public Vector2 LastScreen => _lastScreen;
        public NodeId? HoveredNode => _hoveredNode;
        public IndicatorKind? HoveredIndicator => _hoveredIndicator;
        public CursorKind Cursor => _cursor;
        public string? Tooltip => _tooltip;

        public event EventHandler? Changed;
        public event EventHandler<Vector2>? ContextMenuRequested;

        public void OnDown(MouseButton button, Vector2 screen)
        {
            _lastScreen = screen;
            switch (button)
            {
                case MouseButton.Left:  HandleLeftDown(screen); break;
                case MouseButton.Right: HandleRightDown(screen); break;
            }
            RefreshOverlay();
        }

        public void OnUp(MouseButton button, Vector2 screen)
        {
            _lastScreen = screen;
            switch (button)
            {
                case MouseButton.Left:  HandleLeftUp(screen); break;
                case MouseButton.Right: HandleRightUp(screen); break;
            }
            RefreshOverlay();
        }

        public void OnMove(Vector2 screen)
        {
            _lastScreen = screen;
            HandleMove(screen);
            RefreshOverlay();
        }

        public void OnWheel(Vector2 screen, float delta)
        {
            _lastScreen = screen;
            HandleWheel(screen, delta);
            RefreshOverlay();
        }

        public void OnLeave()
        {
            bool changed = false;
            changed |= SetHover(null, null);
            changed |= SetCursor(CursorKind.Default);
            changed |= SetTooltip(null);
            if (changed) Changed?.Invoke(this, EventArgs.Empty);
        }

        private void HandleLeftDown(Vector2 screen)
        {
            var canvas = _presenter.ScreenToCanvas(screen);
            var m = _presenter.Metrics;

            var outPin = Geometry.OutputPinAt(_world, canvas, m);
            if (outPin is { } src)
            {
                _state = new InteractionState.DraggingWire(src, canvas);
                return;
            }

            var inPin = Geometry.InputPinAt(_world, canvas, m);
            if (inPin is { } tgt)
            {
                if (_world.Wires.SourceOf(tgt) is not null)
                    _world.Wires.Disconnect(tgt);
                return;
            }

            var hit = Geometry.NodeAt(_world, canvas, m);
            if (hit is { } id)
            {
                _presenter.Select(id);
                var pos = _world.Layout.Get(id) ?? Vector2.Zero;
                _state = new InteractionState.DraggingNode(id, canvas - pos);
                return;
            }

            _presenter.Select(null);
        }

        private void HandleLeftUp(Vector2 screen)
        {
            var canvas = _presenter.ScreenToCanvas(screen);
            var m = _presenter.Metrics;

            if (_state is InteractionState.DraggingWire dw)
            {
                var target = Geometry.InputPinAt(_world, canvas, m);
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
        }

        private void HandleRightDown(Vector2 screen)
        {
            _state = new InteractionState.AwaitingRightClick(_presenter.Pan, screen);
        }

        private void HandleRightUp(Vector2 screen)
        {
            switch (_state)
            {
                case InteractionState.Panning:
                    _state = new InteractionState.Idle();
                    break;
                case InteractionState.AwaitingRightClick:
                    _state = new InteractionState.Idle();
                    ContextMenuRequested?.Invoke(this, screen);
                    break;
            }
        }

        private void HandleMove(Vector2 screen)
        {
            switch (_state)
            {
                case InteractionState.AwaitingRightClick aw:
                {
                    float dx = screen.X - aw.ScreenStart.X;
                    float dy = screen.Y - aw.ScreenStart.Y;
                    if (MathF.Abs(dx) > PanDragThresholdPx || MathF.Abs(dy) > PanDragThresholdPx)
                    {
                        _presenter.ApplyView(
                            new Vector2(aw.PanStart.X + dx, aw.PanStart.Y + dy),
                            _presenter.Zoom);
                        _state = new InteractionState.Panning(aw.PanStart, aw.ScreenStart);
                    }
                    break;
                }
                case InteractionState.Panning p:
                    _presenter.ApplyView(
                        new Vector2(p.PanStart.X + screen.X - p.ScreenStart.X,
                                    p.PanStart.Y + screen.Y - p.ScreenStart.Y),
                        _presenter.Zoom);
                    break;

                case InteractionState.DraggingNode d:
                    var canvas = _presenter.ScreenToCanvas(screen);
                    _world.Layout.Set(d.Node, canvas - d.OffsetCanvas);
                    break;

                case InteractionState.DraggingWire w:
                    _state = w with { EndCanvas = _presenter.ScreenToCanvas(screen) };
                    break;
            }
        }

        private void HandleWheel(Vector2 screen, float delta)
        {
            float steps = delta / 120f;
            float factor = MathF.Pow(1.2f, steps);
            float oldZoom = _presenter.Zoom;
            float newZoom = Math.Clamp(oldZoom * factor, MinZoom, MaxZoom);
            if (MathF.Abs(newZoom - oldZoom) < 1e-6f) return;

            float ratio = newZoom / oldZoom;
            var pan = _presenter.Pan;
            var newPan = new Vector2(
                screen.X - (screen.X - pan.X) * ratio,
                screen.Y - (screen.Y - pan.Y) * ratio);
            _presenter.ApplyView(newPan, newZoom);
        }

        private void RefreshOverlay()
        {
            var canvas = _presenter.ScreenToCanvas(_lastScreen);
            bool changed = false;
            if (!Equals(_state, _lastNotifiedState))
            {
                _lastNotifiedState = _state;
                changed = true;
            }
            changed |= UpdateHover(canvas);
            changed |= UpdateCursor();
            changed |= UpdateTooltip();
            if (changed) Changed?.Invoke(this, EventArgs.Empty);
        }

        private bool UpdateHover(Vector2 canvas)
        {
            NodeId? newNode = null;
            IndicatorKind? newIndicator = null;
            var m = _presenter.Metrics;
            float r = m.HitRadius;
            foreach (var node in _world.Nodes.All)
            {
                if (Vector2.Distance(
                        Geometry.StatusDotPosition(_world, node.Id, m), canvas) <= r)
                {
                    newNode = node.Id;
                    newIndicator = IndicatorKind.Status;
                    break;
                }
                if (_world.DynamicUpdates.HasLifetime(node.Id) &&
                    Vector2.Distance(
                        Geometry.DynamicDotPosition(_world, node.Id, m), canvas) <= r)
                {
                    newNode = node.Id;
                    newIndicator = IndicatorKind.Dynamic;
                    break;
                }
            }
            if (newNode is null && newIndicator is null)
                newNode = Geometry.NodeAt(_world, canvas, m);

            return SetHover(newNode, newIndicator);
        }

        private bool UpdateCursor()
        {
            var newCursor = _state is InteractionState.Panning
                ? CursorKind.Move
                : CursorKind.Default;
            return SetCursor(newCursor);
        }

        private bool UpdateTooltip()
        {
            string? newText = _hoveredIndicator switch
            {
                IndicatorKind.Status when _hoveredNode is { } id => ResolveStatus(id),
                IndicatorKind.Dynamic => "Dynamic",
                _ => null,
            };
            return SetTooltip(newText);
        }

        private string ResolveStatus(NodeId id)
        {
            if (_world.Errors.Get(id).Count > 0) return "Error";
            var status = _world.Status.Get(id);
            if (status?.Readiness == NodeReadiness.Ready) return "Ready";
            return "Not Ready";
        }

        private bool SetHover(NodeId? node, IndicatorKind? indicator)
        {
            if (Nullable.Equals(_hoveredNode, node) && _hoveredIndicator == indicator) return false;
            _hoveredNode = node;
            _hoveredIndicator = indicator;
            return true;
        }

        private bool SetCursor(CursorKind cursor)
        {
            if (_cursor == cursor) return false;
            _cursor = cursor;
            return true;
        }

        private bool SetTooltip(string? text)
        {
            if (_tooltip == text) return false;
            _tooltip = text;
            return true;
        }
    }
}
