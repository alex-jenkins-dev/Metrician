// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using Metrician.Core.Graph;

namespace Metrician.Model.Graph
{
    public static class GraphContextMenuBuilder
    {
        public static IReadOnlyList<ContextMenuItem> Build(
            GraphPresenter presenter,
            Vector2 screen,
            IReadOnlyList<INodeTemplate> available,
            IReadOnlyList<INodeTemplate> pinned,
            IGraphScriptCommands? scriptCommands)
        {
            if (presenter is null) throw new ArgumentNullException(nameof(presenter));

            var world = presenter.World;
            var canvas = presenter.ScreenToCanvas(screen);
            var hit = Geometry.NodeAt(world, canvas, presenter.Metrics);

            if (hit is { } id)
                return BuildForNode(world, id);

            return BuildForCanvas(presenter, canvas, available, pinned, scriptCommands);
        }

        private static IReadOnlyList<ContextMenuItem> BuildForNode(IGraphWorld world, NodeId id)
        {
            var node = world.Nodes.Get(id);
            if (node is null) return Array.Empty<ContextMenuItem>();

            return new[]
            {
                new ContextMenuItem(node.Title, Enabled: false),
                ContextMenuItem.Separator,
                new ContextMenuItem("Delete", () => world.Remove(id)),
            };
        }

        private static IReadOnlyList<ContextMenuItem> BuildForCanvas(
            GraphPresenter presenter,
            Vector2 canvas,
            IReadOnlyList<INodeTemplate> available,
            IReadOnlyList<INodeTemplate> pinned,
            IGraphScriptCommands? scriptCommands)
        {
            var items = new List<ContextMenuItem>();

            if (available.Count + pinned.Count > 0)
            {
                items.Add(new ContextMenuItem("Add",
                    Children: BuildAddSubMenu(presenter, canvas, available, pinned)));
                items.Add(ContextMenuItem.Separator);
            }

            items.Add(new ContextMenuItem("Reset View", presenter.ResetView));

            var world = presenter.World;
            items.Add(new ContextMenuItem("Clear Graph", () =>
            {
                foreach (var n in world.Nodes.All.ToList())
                    world.Remove(n.Id);
            }));

            if (scriptCommands is { } commands)
            {
                items.Add(ContextMenuItem.Separator);
                items.Add(new ContextMenuItem("Save Graph", commands.Save));
                items.Add(new ContextMenuItem("Load Graph", commands.LoadReplace));
                var anchor = canvas;
                items.Add(new ContextMenuItem("Append Graph", () => commands.LoadAppend(anchor)));
            }

            return items;
        }

        private static IReadOnlyList<ContextMenuItem> BuildAddSubMenu(
            GraphPresenter presenter,
            Vector2 canvas,
            IReadOnlyList<INodeTemplate> available,
            IReadOnlyList<INodeTemplate> pinned)
        {
            var items = new List<ContextMenuItem>();

            var grouped = available
                .GroupBy(t => string.IsNullOrEmpty(t.Vendor) ? "Other" : t.Vendor)
                .OrderBy(g => g.Key);

            foreach (var grouping in grouped)
            {
                var vendorChildren = grouping
                    .OrderBy(t => t.Title)
                    .Select(t => SpawnItem(presenter, t, canvas))
                    .ToList();
                items.Add(new ContextMenuItem(grouping.Key, Children: vendorChildren));
            }

            if (pinned.Count > 0)
            {
                if (items.Count > 0) items.Add(ContextMenuItem.Separator);
                foreach (var template in pinned)
                    items.Add(SpawnItem(presenter, template, canvas));
            }

            return items;
        }

        private static ContextMenuItem SpawnItem(
            GraphPresenter presenter, INodeTemplate template, Vector2 canvas)
        {
            var captured = template;
            var pos = canvas;
            return new ContextMenuItem(captured.Title, () => presenter.Spawn(captured, pos));
        }
    }
}
