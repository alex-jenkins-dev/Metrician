// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Model.Graph
{
    public sealed class GraphPresenter
    {
        private readonly IGraphWorld _world;
        private readonly LayoutMetrics _metrics;

        private Vector2 _pan = Vector2.Zero;
        private float _zoom = 1f;
        private float _dpiScale = 1f;
        private NodeId? _selectedNode;
        private PinId? _selectedPin;

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
            _world.Status.Changed += OnWorldChanged;
            _world.Errors.Changed += OnWorldChanged;
        }

        public IGraphWorld World => _world;
        public LayoutMetrics Metrics => _metrics;
        public Vector2 Pan => _pan;
        public float Zoom => _zoom;
        public float DpiScale => _dpiScale;
        public NodeId? SelectedNode => _selectedNode;
        public PinId? SelectedPin => _selectedPin;

        public event EventHandler? ViewChanged;
        public event EventHandler<NodeId?>? SelectionChanged;
        public event EventHandler<PinId?>? PinSelectionChanged;
        public event EventHandler<(NodeId Id, INodeTemplate Template)>? NodeSpawned;

        public Vector2 ScreenToCanvas(Vector2 screen)
        {
            float s = _zoom * _dpiScale;
            return new((screen.X - _pan.X) / s, (screen.Y - _pan.Y) / s);
        }

        public Vector2 CanvasToScreen(Vector2 canvas)
        {
            float s = _zoom * _dpiScale;
            return new(canvas.X * s + _pan.X, canvas.Y * s + _pan.Y);
        }

        public void ApplyView(Vector2 pan, float zoom)
        {
            if (_pan == pan && _zoom == zoom) return;
            _pan = pan;
            _zoom = zoom;
            Raise();
        }

        public void SetDpiScale(float dpiScale)
        {
            if (dpiScale <= 0f || MathF.Abs(_dpiScale - dpiScale) < 1e-6f) return;
            _dpiScale = dpiScale;
            Raise();
        }

        public void Select(NodeId? id)
        {
            if (Nullable.Equals(_selectedNode, id)) return;
            _selectedNode = id;
            if (_selectedPin is not null)
            {
                _selectedPin = null;
                PinSelectionChanged?.Invoke(this, null);
            }
            SelectionChanged?.Invoke(this, id);
            Raise();
        }

        public void SelectPin(PinId? pin)
        {
            if (Nullable.Equals(_selectedPin, pin)) return;
            _selectedPin = pin;
            PinSelectionChanged?.Invoke(this, pin);
            Raise();
        }

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
            if (_selectedNode is not { } id) return false;
            Select(null);
            _world.Remove(id);
            return true;
        }

        public void ResetView() => ApplyView(Vector2.Zero, 1f);

        private void OnWorldChanged<T>(object? sender, T e) => Raise();

        private void Raise() => ViewChanged?.Invoke(this, EventArgs.Empty);
    }
}
