// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Library.Bridge;
using Metrician.Core.Graph;
using Metrician.Core.Plugins;
using Metrician.Core.ScriptBinding;
using Metrician.Presentation.Graph;
using Metrician.Library.Renderables;

namespace Metrician.App
{
    internal sealed class SessionState
    {
        private const string PluginsFolderName = "Plugins";
        private const string ExclusionsFileName = "exclusions.txt";

        public GraphWorld World { get; } = new();
        public GraphControl GraphControl { get; }
        public GraphWorkspaceControl Workspace { get; }
        public RenderableRegistry Renderables { get; } = new();
        public RenderSink RenderSink { get; }

        private readonly NodeTemplateRegistry _templates = new();
        private readonly TemplateNameSystem _templateNames;

        public SessionState()
        {
            RenderSink = new RenderSink(World);
            _templateNames = new TemplateNameSystem(World.Nodes);

            GraphControl = new GraphControl(World);
            Workspace = new GraphWorkspaceControl(GraphControl, GraphTheme.Dark)
            {
                Dock = DockStyle.Fill,
            };

            var renderTemplate = new RenderNodeTemplate(Renderables, RenderSink.Publish);
            _templates.Register(nameof(RenderNodeTemplate),
                () => new RenderNodeTemplate(Renderables, RenderSink.Publish));
            GraphControl.PinnedTemplates.Add(renderTemplate);
            GraphControl.KeyShortcuts[Keys.R] = renderTemplate;

            LoadPlugins();

            GraphControl.Presenter.NodeSpawned += (_, e) =>
                _templateNames.Set(e.Id, e.Template.GetType().Name);

            GraphControl.ScriptCommands = new GraphScriptCommands(
                World, _templates, _templateNames, GraphControl);
        }

        private void LoadPlugins()
        {
            var pluginsDir = Path.Combine(AppContext.BaseDirectory, PluginsFolderName);
            var exclusions = PluginExclusions.FromFile(
                Path.Combine(AppContext.BaseDirectory, ExclusionsFileName));
            var contributions = new List<NodeTemplateContribution>();

            PluginLoader.LoadFromDirectory(
                pluginsDir, Renderables, World.Converters, contributions, exclusions);

            foreach (var contribution in contributions)
            {
                _templates.Register(contribution.Name, contribution.Create);
                GraphControl.AvailableTemplates.Add(contribution.Create());
            }
        }
    }

    internal sealed class RenderSink
    {
        private readonly Dictionary<NodeId, IReadOnlyList<IRenderable>> _byNode = new();

        public RenderSink(IGraphWorld world)
        {
            world.Nodes.Removed += (_, id) => Remove(id);
            world.Errors.Changed += (_, id) =>
            {
                if (world.Errors.Get(id).Count > 0) Remove(id);
            };
        }

        public event EventHandler? Changed;

        public void Publish(NodeId node, IReadOnlyList<IRenderable> items)
        {
            _byNode[node] = items;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerable<IRenderable> All()
        {
            foreach (var list in _byNode.Values)
                foreach (var r in list)
                    yield return r;
        }

        private void Remove(NodeId id)
        {
            if (_byNode.Remove(id)) Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
