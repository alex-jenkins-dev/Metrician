// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core.Graph;

namespace Metrician.App
{
    /// <summary>
    /// Owns <see cref="SessionState"/> and routes mode-change requests between
    /// <see cref="MainForm"/> and <see cref="GraphForm"/>. Either form can close
    /// independently; the radios always reflect the open windows.
    /// </summary>
    internal sealed class WindowController
    {
        private readonly MultiFormApplicationContext _ctx;
        private readonly SessionState _session;
        private int _evaluateScheduled;

        private MainForm? _mainForm;
        private GraphForm? _graphForm;

        // Only meaningful when _mainForm != null.
        private bool _mainShowsViewport = true;

        public WindowController(MultiFormApplicationContext ctx, string? scriptPath = null)
        {
            _ = scriptPath; // scripting is unhooked while the new ECS path beds in.
            _ctx = ctx;
            _session = new SessionState();

            var world = _session.World;
            world.Wires.Connected += (_, _) => Evaluate();
            world.Wires.Disconnected += (_, _) => Evaluate();
            world.Properties.Changed += (_, _) => Evaluate();
            world.DynamicUpdates.UpdateRequested += OnDynamicRefreshRequested;
            _session.RenderSink.Changed += (_, _) => RefreshViewport();

            Evaluate();
        }

        public DisplayMode ActualMode
        {
            get
            {
                if (_mainForm != null && _graphForm != null) return DisplayMode.Both;
                if (_mainForm != null) return _mainShowsViewport ? DisplayMode.ThreeD : DisplayMode.Graph;
                if (_graphForm != null) return DisplayMode.Graph;
                return DisplayMode.ThreeD;
            }
        }

        public void Start() => RequestMode(DisplayMode.ThreeD);

        public void RequestMode(DisplayMode requested)
        {
            if (_session.GraphControl.Parent != null) _session.GraphControl.Parent = null;

            switch (requested)
            {
                case DisplayMode.ThreeD:
                    EnsureMainForm();
                    _mainForm!.ShowViewport();
                    _mainShowsViewport = true;
                    CloseGraphFormIfOpen();
                    break;

                case DisplayMode.Graph:
                    if (_mainForm != null)
                    {
                        _mainForm.HostGraph(_session.GraphControl);
                        _mainShowsViewport = false;
                        CloseGraphFormIfOpen();
                    }
                    else
                    {
                        EnsureGraphForm();
                        _graphForm!.HostGraph(_session.GraphControl);
                    }
                    break;

                case DisplayMode.Both:
                    EnsureMainForm();
                    _mainForm!.ShowViewport();
                    _mainShowsViewport = true;
                    EnsureGraphForm();
                    _graphForm!.HostGraph(_session.GraphControl);
                    break;
            }

            UpdateRadios();
        }

        private void EnsureMainForm()
        {
            if (_mainForm != null) return;
            _mainForm = new MainForm(this);
            _mainForm.FormClosed += OnMainFormClosed;
            _ctx.Track(_mainForm);
            _mainForm.Show();
            RefreshViewport();
        }

        private void EnsureGraphForm()
        {
            if (_graphForm != null) return;
            _graphForm = new GraphForm(this);
            _graphForm.FormClosed += OnGraphFormClosed;
            _ctx.Track(_graphForm);
            _graphForm.Show();
        }

        private void CloseGraphFormIfOpen()
        {
            if (_graphForm == null) return;
            if (_session.GraphControl.Parent == _graphForm) _session.GraphControl.Parent = null;
            var gf = _graphForm;
            _graphForm = null;
            gf.FormClosed -= OnGraphFormClosed;
            gf.Close();
        }

        private void OnMainFormClosed(object? sender, FormClosedEventArgs e)
        {
            if (_mainForm != null) _mainForm.FormClosed -= OnMainFormClosed;
            _mainForm = null;

            if (_graphForm != null)
            {
                if (_session.GraphControl.Parent != _graphForm)
                {
                    if (_session.GraphControl.Parent != null) _session.GraphControl.Parent = null;
                    _graphForm.HostGraph(_session.GraphControl);
                }
                UpdateRadios();
            }
        }

        private void OnGraphFormClosed(object? sender, FormClosedEventArgs e)
        {
            if (_graphForm != null) _graphForm.FormClosed -= OnGraphFormClosed;
            _graphForm = null;

            if (_mainForm != null)
            {
                if (_session.GraphControl.Parent != null) _session.GraphControl.Parent = null;
                _mainForm.ShowViewport();
                _mainShowsViewport = true;
                UpdateRadios();
            }
        }

        private void UpdateRadios()
        {
            var mode = ActualMode;
            _mainForm?.UpdateModeRadio(mode);
            _graphForm?.UpdateModeRadio(mode);
        }

        private void OnDynamicRefreshRequested(object? sender, NodeId id)
        {
            var anchor = (Control?)_mainForm ?? _graphForm;
            if (anchor is null || anchor.IsDisposed || !anchor.IsHandleCreated) return;
            if (Interlocked.Exchange(ref _evaluateScheduled, 1) != 0) return;
            anchor.BeginInvoke(new Action(() =>
            {
                Interlocked.Exchange(ref _evaluateScheduled, 0);
                Evaluate();
                _session.GraphControl.Invalidate();
            }));
        }

        private void Evaluate()
        {
            try { _session.World.Evaluation.EvaluateAll(); }
            catch (Exception) { /* surfaced via NodeErrorSystem in evaluators */ }
        }

        private void RefreshViewport()
        {
            if (_mainForm is null || _mainForm.IsDisposed) return;
            if (!_mainForm.IsHandleCreated) return;
            if (_mainForm.InvokeRequired)
            {
                _mainForm.BeginInvoke(new Action(RefreshViewport));
                return;
            }
            var sink = _session.RenderSink;
            var renderables = _mainForm.Renderables;
            renderables.Clear();
            foreach (var r in sink.All())
                renderables.Add(r);
        }
    }
}
