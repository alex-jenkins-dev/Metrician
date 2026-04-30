// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph;
using Metrician.Graph.Contracts;
using Metrician.Plugins;
using Metrician.Script;

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
        private readonly DynamicNodeCoordinator _dynamics;

        private GraphViewportBinding? _binding;

        private MainForm? _mainForm;
        private GraphForm? _graphForm;

        // Only meaningful when _mainForm != null.
        private bool _mainShowsViewport = true;

        private readonly PluginLoadResult _plugins;

        public WindowController(MultiFormApplicationContext ctx, string? scriptPath = null)
        {
            _ctx = ctx;
            _session = new SessionState();

            _dynamics = new DynamicNodeCoordinator(
                _session.Graph,
                anchorProvider: () => (Control?)_mainForm ?? _graphForm,
                refresh: () => _binding?.Refresh());

            _session.GraphControl.GraphChanged       += OnGraphChanged;
            _session.GraphControl.SelectionChanged   += OnSelectionChanged;
            _session.GraphControl.SaveGraphRequested  += OnSaveGraphRequested;
            _session.GraphControl.LoadGraphRequested  += OnLoadGraphRequested;
            _session.GraphControl.AppendGraphRequested+= OnAppendGraphRequested;
            _session.PropertyGrid.PropertyValueChanged += OnPropertyChanged;

            _plugins = PluginInstaller.Install(_session);
            if (scriptPath != null)
                LoadStartupScript(scriptPath);
        }

        private void LoadStartupScript(string scriptPath)
        {
            try
            {
                var script = GraphScriptText.ReadFile(scriptPath);
                var factories = PluginInstaller.ScriptFactories(_session, _plugins);
                var nodes = GraphScriptApplier.Apply(
                    script, factories, _session.Converters, _session.Conversions);
                if (nodes.Count == 0) return;
                if (script.HasPositions)
                    _session.GraphControl.AddNodes(nodes);
                else
                    _session.GraphControl.AddNodesWithLayout(nodes);
            }
            catch (ScriptException ex)
            {
                ShowScriptError($"Failed to load script '{scriptPath}'", ex);
            }
        }

        private void OnSaveGraphRequested(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "Metrician script (*.metrician)|*.metrician|All files (*.*)|*.*",
                DefaultExt = "metrician",
                FileName = "graph.metrician",
                AddExtension = true,
                OverwritePrompt = true,
            };
            if (dlg.ShowDialog((Control?)_mainForm ?? _graphForm) != DialogResult.OK) return;

            try
            {
                var script = GraphScriptIntrospector.Introspect(_session.Graph.Nodes);
                GraphScriptText.WriteFile(dlg.FileName, script);
            }
            catch (ScriptException ex)
            {
                ShowScriptError($"Failed to save graph to '{dlg.FileName}'", ex);
            }
        }

        private void OnLoadGraphRequested(object? sender, EventArgs e)
        {
            if (TryPickAndApplyScript(out var nodes, out var hadPositions))
            {
                _session.GraphControl.ClearGraph();
                if (hadPositions)
                    _session.GraphControl.AddNodes(nodes);
                else
                    _session.GraphControl.AddNodesWithLayout(nodes);
            }
        }

        // Auto-layout when the script has no positions, otherwise the
        // appended nodes stack on the origin.
        private void OnAppendGraphRequested(object? sender, EventArgs e)
        {
            if (TryPickAndApplyScript(out var nodes, out var hadPositions))
            {
                if (hadPositions)
                    _session.GraphControl.AddNodes(nodes);
                else
                    _session.GraphControl.AddNodesWithLayout(nodes);
            }
        }

        // Shared open-dialog + parse + apply for Load and Append.
        private bool TryPickAndApplyScript(
            out IReadOnlyList<INode> nodes, out bool hadPositions)
        {
            nodes = Array.Empty<INode>();
            hadPositions = false;

            using var dlg = new OpenFileDialog
            {
                Filter = "Metrician script (*.metrician)|*.metrician|All files (*.*)|*.*",
                DefaultExt = "metrician",
            };
            if (dlg.ShowDialog((Control?)_mainForm ?? _graphForm) != DialogResult.OK)
                return false;

            try
            {
                var script = GraphScriptText.ReadFile(dlg.FileName);
                var factories = PluginInstaller.ScriptFactories(_session, _plugins);
                nodes = GraphScriptApplier.Apply(
                    script, factories, _session.Converters, _session.Conversions);
                hadPositions = script.HasPositions;
                return nodes.Count > 0;
            }
            catch (ScriptException ex)
            {
                ShowScriptError($"Failed to load graph from '{dlg.FileName}'", ex);
                return false;
            }
        }

        private void ShowScriptError(string headline, ScriptException ex)
        {
            string where = ex.LineNumber > 0 ? $" (line {ex.LineNumber})" : "";
            MessageBox.Show(
                $"{headline}{where}:\n\n{ex.Message}",
                "Metrician",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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

        /// <summary>
        /// Routes a mode-change from either form's title-bar menu, creating and destroying windows as needed.
        /// </summary>
        public void RequestMode(DisplayMode requested)
        {
            if (_session.GraphHost.Parent != null) _session.GraphHost.Parent = null;

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
                        _mainForm.HostGraph(_session.GraphHost);
                        _mainShowsViewport = false;
                        CloseGraphFormIfOpen();
                    }
                    else
                    {
                        EnsureGraphForm();
                        _graphForm!.HostGraph(_session.GraphHost);
                    }
                    break;

                case DisplayMode.Both:
                    EnsureMainForm();
                    _mainForm!.ShowViewport();
                    _mainShowsViewport = true;
                    EnsureGraphForm();
                    _graphForm!.HostGraph(_session.GraphHost);
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

            _binding = new GraphViewportBinding(_session.Graph, _mainForm.Renderables);
            _binding.Refresh();
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
            if (_session.GraphHost.Parent == _graphForm) _session.GraphHost.Parent = null;
            var gf = _graphForm;
            // Clear the field before Close so FormClosed does not re-enter.
            _graphForm = null;
            gf.FormClosed -= OnGraphFormClosed;
            gf.Close();
        }

        private void OnMainFormClosed(object? sender, FormClosedEventArgs e)
        {
            if (_mainForm != null) _mainForm.FormClosed -= OnMainFormClosed;
            _mainForm = null;
            _binding = null;

            if (_graphForm != null)
            {
                if (_session.GraphHost.Parent != _graphForm)
                {
                    if (_session.GraphHost.Parent != null) _session.GraphHost.Parent = null;
                    _graphForm.HostGraph(_session.GraphHost);
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
                if (_session.GraphHost.Parent != null) _session.GraphHost.Parent = null;
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

        private void OnGraphChanged(object? sender, EventArgs e)
        {
            _dynamics.Sync();
            _binding?.Refresh();
        }

        private void OnSelectionChanged(object? sender, EventArgs e)
            => _session.PropertyGrid.SelectedObject = _session.GraphControl.SelectedNode;

        private void OnPropertyChanged(object? sender, PropertyValueChangedEventArgs e)
        {
            // Let IDynamicPins nodes resize their pin set before re-evaluation.
            if (_session.GraphControl.SelectedNode is IDynamicPins dyn)
            {
                dyn.RebuildPins();
                _session.GraphControl.Invalidate();
            }
            _binding?.Refresh();
        }
    }
}
