// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Bridge;
using Metrician.Core;
using Metrician.Core.Graph;
using Metrician.Core.ScriptBinding;
using Metrician.Nodes.Geometry;
using Metrician.Presentation.Graph;
using Metrician.Renderable.Contracts;

namespace Metrician.App
{
    internal sealed class SessionState
    {
        public GraphWorld World { get; } = new();
        public GraphControl GraphControl { get; }
        public GraphWorkspaceControl Workspace { get; }
        public RenderableRegistry Renderables { get; } = new();
        public RenderSink RenderSink { get; }

        private readonly NodeTemplateRegistry _templates = new();
        private readonly TemplateNameSystem _templateNames;

        public SessionState()
        {
            Renderables.Register(new CylinderSpecFactory());
            Renderables.Register(new PlaneSpecFactory());
            Renderables.Register(new CircleSpecFactory());
            Renderables.Register(new PointSpecFactory());

            World.Converters.Register(new CircleToPointConverter());

            RenderSink = new RenderSink(World);
            _templateNames = new TemplateNameSystem(World.Nodes);

            GraphControl = new GraphControl(World);
            Workspace = new GraphWorkspaceControl(GraphControl, GraphTheme.Dark)
            {
                Dock = DockStyle.Fill,
            };

            var renderTemplate = new RenderNodeTemplate(Renderables, RenderSink.Publish);

            _templates.Register(nameof(CylinderNodeTemplate),       () => new CylinderNodeTemplate());
            _templates.Register(nameof(PlaneNodeTemplate),          () => new PlaneNodeTemplate());
            _templates.Register(nameof(CircleNodeTemplate),         () => new CircleNodeTemplate());
            _templates.Register(nameof(IntersectionNodeTemplate),   () => new IntersectionNodeTemplate());
            _templates.Register(nameof(LabelledPointNodeTemplate),  () => new LabelledPointNodeTemplate());
            _templates.Register(nameof(RenderNodeTemplate),         () => new RenderNodeTemplate(Renderables, RenderSink.Publish));

            GraphControl.AvailableTemplates.Add(new CylinderNodeTemplate());
            GraphControl.AvailableTemplates.Add(new PlaneNodeTemplate());
            GraphControl.AvailableTemplates.Add(new CircleNodeTemplate());
            GraphControl.AvailableTemplates.Add(new IntersectionNodeTemplate());
            GraphControl.AvailableTemplates.Add(new LabelledPointNodeTemplate());
            GraphControl.PinnedTemplates.Add(renderTemplate);
            GraphControl.KeyShortcuts[Keys.R] = renderTemplate;

            GraphControl.Presenter.NodeSpawned += (_, e) =>
                _templateNames.Set(e.Id, e.Template.GetType().Name);

            GraphControl.ScriptCommands = new GraphScriptCommands(
                World, _templates, _templateNames, GraphControl);
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
