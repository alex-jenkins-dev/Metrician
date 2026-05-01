// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Presentation.Graph
{
    public sealed class DelayedToolTip : IDisposable
    {
        private const int DefaultDelayMs = 350;

        private readonly Control _owner;
        private readonly ToolTip _tooltip = new();
        private readonly System.Windows.Forms.Timer _delay;

        private string? _shown;
        private string? _pending;
        private Point _pendingLocation;

        public DelayedToolTip(Control owner, int delayMs = DefaultDelayMs)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _delay = new System.Windows.Forms.Timer { Interval = delayMs };
            _delay.Tick += OnTick;
        }

        public void Show(string? text, Point screenAt)
        {
            if (text is null)
            {
                HideInternal();
                return;
            }

            _pendingLocation = screenAt;

            if (text == _shown)
            {
                _pending = null;
                _delay.Stop();
                return;
            }

            if (text == _pending) return;

            _pending = text;
            _delay.Stop();
            _delay.Start();

            if (_shown is not null)
            {
                _shown = null;
                _tooltip.Hide(_owner);
            }
        }

        public void Hide() => HideInternal();

        public void Dispose()
        {
            _delay.Stop();
            _delay.Tick -= OnTick;
            _delay.Dispose();
            _tooltip.Dispose();
        }

        private void HideInternal()
        {
            _delay.Stop();
            _pending = null;
            if (_shown is null) return;
            _shown = null;
            _tooltip.Hide(_owner);
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _delay.Stop();
            if (_pending is null) return;
            if (_pending == _shown) return;
            _shown = _pending;
            _tooltip.Show(_shown, _owner, _pendingLocation);
        }
    }
}
