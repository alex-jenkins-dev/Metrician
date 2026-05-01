// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.Presentation.Graph
{
    public sealed class DelayedToolTip : IDisposable
    {
        private const int DefaultDelayMs = 350;

        private readonly Control _owner;
        private readonly TooltipPopup _popup;
        private readonly System.Windows.Forms.Timer _delay;

        private string? _shown;
        private string? _pending;
        private Point _pendingLocation;

        public DelayedToolTip(Control owner, int delayMs = DefaultDelayMs)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _popup = new TooltipPopup();
            _delay = new System.Windows.Forms.Timer { Interval = delayMs };
            _delay.Tick += OnTick;
        }

        public void Show(string? text, Point screenAt)
        {
            if (string.IsNullOrEmpty(text))
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

            if (_shown is not null) HideInternal();
        }

        public void Hide() => HideInternal();

        public void Dispose()
        {
            _delay.Stop();
            _delay.Tick -= OnTick;
            _delay.Dispose();
            _popup.Dispose();
        }

        private void HideInternal()
        {
            _delay.Stop();
            _pending = null;
            _shown = null;
            if (_popup.Visible) _popup.Hide();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _delay.Stop();
            if (_pending is null) return;
            if (_pending == _shown) return;
            _shown = _pending;

            _popup.SetText(_shown);
            _popup.Location = _owner.PointToScreen(_pendingLocation);
            if (!_popup.Visible) _popup.Show();
        }

        private sealed class TooltipPopup : Form
        {
            private static readonly Color BackgroundColour = Color.FromArgb(45, 45, 48);
            private static readonly Color BorderColour = Color.FromArgb(80, 80, 88);
            private static readonly Color TextColour = Color.FromArgb(220, 220, 220);
            private static readonly Padding TextPadding = new(8, 5, 8, 5);
            private const TextFormatFlags TextFlags =
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.Left;

            private string _text = string.Empty;

            public TooltipPopup()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                TopMost = true;
                StartPosition = FormStartPosition.Manual;
                BackColor = BackgroundColour;
                ForeColor = TextColour;
                Font = new Font("Segoe UI", 9f);
                DoubleBuffered = true;
                Size = new Size(1, 1);
            }

            protected override bool ShowWithoutActivation => true;

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    // WS_EX_TOOLWINDOW: keep out of Alt-Tab and the taskbar.
                    // WS_EX_NOACTIVATE: don't steal focus from the owner control.
                    cp.ExStyle |= 0x00000080 | 0x08000000;
                    return cp;
                }
            }

            public void SetText(string text)
            {
                _text = text ?? string.Empty;
                var measured = TextRenderer.MeasureText(_text, Font, Size.Empty, TextFlags);
                Size = new Size(
                    measured.Width + TextPadding.Horizontal,
                    measured.Height + TextPadding.Vertical);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.Clear(BackgroundColour);
                using (var border = new Pen(BorderColour))
                    e.Graphics.DrawRectangle(border,
                        0, 0, ClientSize.Width - 1, ClientSize.Height - 1);

                var rect = new Rectangle(
                    TextPadding.Left,
                    TextPadding.Top,
                    ClientSize.Width - TextPadding.Horizontal,
                    ClientSize.Height - TextPadding.Vertical);
                TextRenderer.DrawText(e.Graphics, _text, Font, rect, TextColour, TextFlags);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                e.Graphics.Clear(BackgroundColour);
            }
        }
    }
}
