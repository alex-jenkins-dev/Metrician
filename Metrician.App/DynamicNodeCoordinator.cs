// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using Metrician.Core;
using Metrician.Graph.Contracts;

namespace Metrician.App
{
    internal sealed class DynamicNodeCoordinator
    {
        private readonly NodeGraph _graph;
        private readonly Func<Control?> _anchorProvider;
        private readonly Action _refresh;

        private readonly HashSet<IDynamicNode> _subscribed = new();
        private int _refreshScheduled;

        public DynamicNodeCoordinator(
            NodeGraph graph,
            Func<Control?> anchorProvider,
            Action refresh)
        {
            _graph = graph;
            _anchorProvider = anchorProvider;
            _refresh = refresh;
        }

        /// <summary>
        /// Reconciles subscriptions to match graph membership; call after every mutation.
        /// Removed nodes are unhooked and disposed.
        /// </summary>
        public void Sync()
        {
            var current = new HashSet<IDynamicNode>();
            foreach (var node in _graph.Nodes)
                if (node is IDynamicNode dyn)
                    current.Add(dyn);

            foreach (var dyn in current)
            {
                if (_subscribed.Add(dyn))
                    dyn.OutputChanged += OnDynamicChanged;
            }

            var removed = new List<IDynamicNode>();
            foreach (var dyn in _subscribed)
                if (!current.Contains(dyn))
                    removed.Add(dyn);
            foreach (var dyn in removed)
            {
                dyn.OutputChanged -= OnDynamicChanged;
                (dyn as IDisposable)?.Dispose();
                _subscribed.Remove(dyn);
            }
        }

        // Coalesces events to one refresh.
        // Only the latest value per node is rendered.
        // Good enough.
        private void OnDynamicChanged(object? sender, EventArgs e)
        {
            var anchor = _anchorProvider();
            if (anchor is null || anchor.IsDisposed || !anchor.IsHandleCreated) return;
            if (Interlocked.Exchange(ref _refreshScheduled, 1) != 0) return;
            anchor.BeginInvoke(new Action(() =>
            {
                Interlocked.Exchange(ref _refreshScheduled, 0);
                _refresh();
            }));
        }
    }
}
