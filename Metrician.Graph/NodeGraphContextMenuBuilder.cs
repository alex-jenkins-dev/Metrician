// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph.Contracts;

namespace Metrician.Graph
{
    /// <summary>
    /// Builds the graph editor's right-click menus. The canvas menu groups
    /// Add entries by vendor and then by source DLL, separated by lines.
    /// </summary>
    internal sealed class NodeGraphContextMenuBuilder
    {
        private readonly NodeGraphTheme _theme;
        private readonly IList<NodeMenuEntry> _availableNodes;

        public NodeGraphContextMenuBuilder(
            NodeGraphTheme theme,
            IList<NodeMenuEntry> availableNodes)
        {
            _theme = theme;
            _availableNodes = availableNodes;
        }

        /// <summary>Builds the canvas menu (no node under cursor).</summary>
        public ContextMenuStrip BuildCanvasMenu(
            PointF canvasAt,
            Action<INode, PointF> onAddNode,
            Action onAutoLayout,
            Action onClear,
            Action onSaveGraph,
            Action onLoadGraph,
            Action onAppendGraph,
            bool hasNodes)
        {
            var menu = NewThemedMenu();

            menu.Items.Add(BuildAddSubmenu(canvasAt, onAddNode));
            menu.Items.Add(new ToolStripSeparator());

            var layoutItem = ThemedItem("Auto Layout");
            layoutItem.Enabled = hasNodes;
            layoutItem.Click += (_, _) => onAutoLayout();
            menu.Items.Add(layoutItem);

            var clearItem = ThemedItem("Clear");
            clearItem.Enabled = hasNodes;
            clearItem.Click += (_, _) => onClear();
            menu.Items.Add(clearItem);

            menu.Items.Add(new ToolStripSeparator());

            var saveItem = ThemedItem("Save Graph");
            saveItem.Enabled = hasNodes;
            saveItem.Click += (_, _) => onSaveGraph();
            menu.Items.Add(saveItem);

            var loadItem = ThemedItem("Load Graph");
            loadItem.Click += (_, _) => onLoadGraph();
            menu.Items.Add(loadItem);

            var appendItem = ThemedItem("Append Graph");
            appendItem.Click += (_, _) => onAppendGraph();
            menu.Items.Add(appendItem);

            return menu;
        }

        public ContextMenuStrip BuildNodeMenu(
            INode node,
            Action<INode> onDelete)
        {
            var menu = NewThemedMenu();

            var deleteItem = ThemedItem("Delete");
            deleteItem.Click += (_, _) => onDelete(node);
            menu.Items.Add(deleteItem);

            return menu;
        }

        // Pinned entries skip vendor grouping and sit below a separator at
        // the bottom of the Add menu (used for fundamental built-ins).
        private ToolStripMenuItem BuildAddSubmenu(PointF canvasAt, Action<INode, PointF> onAddNode)
        {
            var addItem = ThemedItem("Add");
            if (_availableNodes.Count == 0)
            {
                addItem.DropDownItems.Add(EmptyPlaceholder());
                return addItem;
            }

            var grouped = _availableNodes
                .Where(e => !e.Pinned)
                .GroupBy(e => string.IsNullOrEmpty(e.Vendor) ? "Unknown" : e.Vendor)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                var vendorItem = ThemedItem(group.Key);
                AppendBySourceWithSeparators(
                    vendorItem,
                    group,
                    e => e.Label,
                    e => e.Source,
                    captured => (_, _) => onAddNode(captured.Factory(), canvasAt));
                addItem.DropDownItems.Add(vendorItem);
            }

            var pinned = _availableNodes.Where(e => e.Pinned).OrderBy(e => e.Label).ToList();
            if (pinned.Count > 0)
            {
                if (addItem.DropDownItems.Count > 0)
                    addItem.DropDownItems.Add(new ToolStripSeparator());
                foreach (var entry in pinned)
                {
                    var captured = entry;
                    var item = ThemedItem(captured.Label);
                    item.Click += (_, _) => onAddNode(captured.Factory(), canvasAt);
                    addItem.DropDownItems.Add(item);
                }
            }
            return addItem;
        }

        // Sub-groups by Source (DLL) with separators between blocks.
        private void AppendBySourceWithSeparators<T>(
            ToolStripMenuItem parent,
            IEnumerable<T> entries,
            Func<T, string> getLabel,
            Func<T, string?> getSource,
            Func<T, EventHandler> makeClickHandler)
        {
            var bySource = entries
                .GroupBy(e => getSource(e) ?? "")
                .OrderBy(g => g.Key);

            bool firstSourceBlock = true;
            foreach (var sourceGroup in bySource)
            {
                if (!firstSourceBlock)
                    parent.DropDownItems.Add(new ToolStripSeparator());
                firstSourceBlock = false;
                foreach (var entry in sourceGroup.OrderBy(getLabel))
                {
                    var captured = entry;
                    var item = ThemedItem(getLabel(captured));
                    item.Click += makeClickHandler(captured);
                    parent.DropDownItems.Add(item);
                }
            }
        }

        private ContextMenuStrip NewThemedMenu() => new ContextMenuStrip
        {
            BackColor = _theme.NodeBackground,
            ForeColor = _theme.Text,
            ShowImageMargin = false,
            Renderer = new DarkContextMenuRenderer(_theme),
        };

        private ToolStripMenuItem ThemedItem(string label) => new ToolStripMenuItem(label)
        {
            BackColor = _theme.NodeBackground,
            ForeColor = _theme.Text,
        };

        private ToolStripMenuItem EmptyPlaceholder()
        {
            var empty = ThemedItem("(none registered)");
            empty.Enabled = false;
            empty.ForeColor = _theme.MenuDisabledText;
            return empty;
        }
    }
}
