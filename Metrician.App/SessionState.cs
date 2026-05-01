// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Bridge;
using Metrician.Core;
using Metrician.Core.Graph;
using Metrician.Core.ScriptBinding;
using Metrician.Presentation.Graph;
using Metrician.Renderable.Contracts;
using Metrician.SampleNodes;
using Metrician.SampleNodes.Ecs;

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
            Renderables.Register(new Vector3PointFactory());
            Renderables.Register(new SphereSpecFactory());

            RenderSink = new RenderSink(World);
            _templateNames = new TemplateNameSystem(World.Nodes);

            GraphControl = new GraphControl(World);
            Workspace = new GraphWorkspaceControl(GraphControl, GraphTheme.Dark)
            {
                Dock = DockStyle.Fill,
            };

            var renderTemplate = new RenderNodeTemplate(Renderables, RenderSink.Publish);

            _templates.Register(nameof(NominalPointNodeTemplate), () => new NominalPointNodeTemplate());
            _templates.Register(nameof(PointStreamNodeTemplate), () => new PointStreamNodeTemplate());
            _templates.Register(nameof(MeanPointodeTemplate), () => new MeanPointodeTemplate());
            _templates.Register(nameof(PointDistanceNodeTemplate), () => new PointDistanceNodeTemplate());
            _templates.Register(nameof(ToleranceCheckNodeTemplate), () => new ToleranceCheckNodeTemplate());
            _templates.Register(nameof(RenderNodeTemplate), () => new RenderNodeTemplate(Renderables, RenderSink.Publish));

            // TODO: Pluginise these.
            GraphControl.AvailableTemplates.Add(new NominalPointNodeTemplate());
            GraphControl.AvailableTemplates.Add(new PointStreamNodeTemplate());
            GraphControl.AvailableTemplates.Add(new MeanPointodeTemplate());
            GraphControl.AvailableTemplates.Add(new PointDistanceNodeTemplate());
            GraphControl.AvailableTemplates.Add(new ToleranceCheckNodeTemplate());
            GraphControl.PinnedTemplates.Add(renderTemplate);
            GraphControl.KeyShortcuts[Keys.R] = renderTemplate;

            GraphControl.Presenter.NodeSpawned += (_, e) =>
                _templateNames.Set(e.Id, e.Template.GetType().Name);

            GraphControl.ScriptCommands = new GraphScriptCommands(
                World, _templates, _templateNames, GraphControl);

            SeedDemoGraph(renderTemplate);
        }

        private NodeId AddSeed(string typeName, INodeTemplate template)
        {
            var id = World.Add(template);
            _templateNames.Set(id, typeName);
            return id;
        }

        private void SeedDemoGraph(RenderNodeTemplate renderTemplate)
        {
            var nominal  = AddSeed(nameof(NominalPointNodeTemplate),  new NominalPointNodeTemplate());
            var probe    = AddSeed(nameof(PointStreamNodeTemplate),   new PointStreamNodeTemplate());
            var render   = AddSeed(nameof(RenderNodeTemplate),        renderTemplate);
            var distance = AddSeed(nameof(PointDistanceNodeTemplate), new PointDistanceNodeTemplate());

            World.Layout.Set(nominal,  new Vector2(60, 60));
            World.Layout.Set(probe,    new Vector2(60, 240));
            World.Layout.Set(distance, new Vector2(320, 140));
            World.Layout.Set(render,   new Vector2(320, 360));

            World.Wires.TryConnect(
                new PinId(nominal, "position", PinDirection.Output),
                new PinId(distance, "feature a", PinDirection.Input));
            World.Wires.TryConnect(
                new PinId(probe, "sample", PinDirection.Output),
                new PinId(distance, "feature b", PinDirection.Input));

            World.Wires.TryConnect(
                new PinId(probe, "sample", PinDirection.Output),
                new PinId(render, "in 0", PinDirection.Input));
            World.Wires.TryConnect(
                new PinId(nominal, "position", PinDirection.Output),
                new PinId(render, "in 1", PinDirection.Input));
        }
    }

    internal sealed class RenderSink
    {
        private readonly Dictionary<NodeId, IReadOnlyList<IRenderable>> _byNode = new();

        public RenderSink(IGraphWorld world)
        {
            world.Nodes.Removed += (_, id) =>
            {
                if (_byNode.Remove(id)) Changed?.Invoke(this, EventArgs.Empty);
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
    }
}
