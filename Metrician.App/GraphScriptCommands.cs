// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;
using Metrician.Core.Scripting;
using Metrician.Core.ScriptBinding;
using Metrician.Presentation.Graph;

namespace Metrician.App
{
    internal sealed class GraphScriptCommands : IGraphScriptCommands
    {
        private const string DefaultExtension = "metrician";
        private const string FileFilter =
            "Metrician script (*.metrician;*.txt)|*.metrician;*.txt|All files|*.*";

        private readonly IGraphWorld _world;
        private readonly INodeTemplateRegistry _templates;
        private readonly ITemplateNameSystem _templateNames;
        private readonly Control _owner;

        public GraphScriptCommands(
            IGraphWorld world,
            INodeTemplateRegistry templates,
            ITemplateNameSystem templateNames,
            Control owner)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _templates = templates ?? throw new ArgumentNullException(nameof(templates));
            _templateNames = templateNames ?? throw new ArgumentNullException(nameof(templateNames));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public void Save()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Save Graph",
                Filter = FileFilter,
                DefaultExt = DefaultExtension,
                AddExtension = true,
                OverwritePrompt = true,
            };
            if (dialog.ShowDialog(DialogParent) != DialogResult.OK) return;

            try
            {
                var script = ScriptIntrospector.Introspect(_world, _templateNames);
                var text = ScriptFormatter.Format(script);
                File.WriteAllText(dialog.FileName, text);
            }
            catch (Exception ex)
            {
                ShowError("Save failed", ex);
            }
        }

        public void LoadReplace() => LoadFromDialog(ScriptApplyMode.Replace, anchor: null);

        public void LoadAppend(Vector2 anchor) =>
            LoadFromDialog(ScriptApplyMode.Append, anchor);

        private void LoadFromDialog(ScriptApplyMode mode, Vector2? anchor)
        {
            using var dialog = new OpenFileDialog
            {
                Title = mode == ScriptApplyMode.Replace ? "Load Graph" : "Append Graph",
                Filter = FileFilter,
                DefaultExt = DefaultExtension,
                CheckFileExists = true,
            };
            if (dialog.ShowDialog(DialogParent) != DialogResult.OK) return;

            try
            {
                string text = File.ReadAllText(dialog.FileName);
                var script = ScriptParser.Parse(text);
                var added = ScriptApplier.Apply(_world, script, _templates, _templateNames, mode);
                if (anchor is { } a)
                    AnchorTopLeft(added.Values, a);
            }
            catch (Exception ex)
            {
                ShowError(mode == ScriptApplyMode.Replace ? "Load failed" : "Append failed", ex);
            }
        }

        private void AnchorTopLeft(IEnumerable<NodeId> nodes, Vector2 anchor)
        {
            var positioned = new List<(NodeId Id, Vector2 Position)>();
            foreach (var id in nodes)
                if (_world.Layout.Get(id) is { } position)
                    positioned.Add((id, position));
            if (positioned.Count == 0) return;

            float minX = positioned.Min(t => t.Position.X);
            float minY = positioned.Min(t => t.Position.Y);
            var delta = anchor - new Vector2(minX, minY);

            foreach (var (id, position) in positioned)
                _world.Layout.Set(id, position + delta);
        }

        private IWin32Window DialogParent => _owner.FindForm() ?? (IWin32Window)_owner;

        private void ShowError(string title, Exception ex) =>
            MessageBox.Show(DialogParent, ex.Message, title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
