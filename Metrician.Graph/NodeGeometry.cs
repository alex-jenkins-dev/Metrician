// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Graph.Contracts;

namespace Metrician.Graph
{
    public static class NodeGeometry
    {
        /// <summary>
        /// Bounding rectangle of <paramref name="node"/>. Height scales with the
        /// larger of input/output pin counts; nodes that do not implement
        /// <see cref="INodeLayout"/> sit at the origin.
        /// </summary>
        public static Rectangle GetNodeRect(INode node, NodeGraphTheme theme)
        {
            int rows = Math.Max(node.Inputs.Count, node.Outputs.Count);
            int height = theme.HeaderHeight
                       + Math.Max(rows, 1) * theme.RowHeight
                       + theme.FooterHeight;
            var pos = (node as INodeLayout)?.Position ?? PointF.Empty;
            return new Rectangle((int)pos.X, (int)pos.Y, theme.NodeWidth, height);
        }

        /// <summary>
        /// Canvas-space centre of <paramref name="pin"/>; inputs left, outputs right.
        /// </summary>
        public static PointF GetPinCanvasPos(INodePin pin, NodeGraphTheme theme)
        {
            var rect = GetNodeRect(pin.Owner, theme);
            int idx = pin.IsInput
                ? IndexOfRef(pin.Owner.Inputs, pin)
                : IndexOfRef(pin.Owner.Outputs, pin);
            float y = rect.Y
                    + theme.HeaderHeight
                    + idx * theme.RowHeight
                    + theme.RowHeight / 2f;
            float x = pin.IsInput ? rect.Left : rect.Right;
            return new PointF(x, y);
        }

        private static int IndexOfRef<T>(IReadOnlyList<T> list, T item) where T : class
        {
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], item)) return i;
            return -1;
        }
    }
}
