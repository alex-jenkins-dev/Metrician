// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.ComponentModel;
using System.Numerics;
using System.Runtime.InteropServices;
using Metrician.Core.Graph;
using Metrician.Model.Graph;

namespace Metrician.Presentation.Graph
{
    public sealed class SearchPalette : Form
    {
        private sealed record SectionHeader(string Label);

        // The framework's TextBox.PlaceholderText hides on focus (EM_SETCUEBANNER
        // is sent with wParam=0). This subclass resends with wParam=1 so the
        // placeholder stays visible while the box is focused, matching modern
        // search-input conventions.
        private sealed class PlaceholderTextBox : TextBox
        {
            private const int EM_SETCUEBANNER = 0x1501;
            private string _placeholder = string.Empty;

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr SendMessageW(IntPtr hwnd, int msg, IntPtr wParam, string lParam);

            [Browsable(false)]
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public string Placeholder
            {
                get => _placeholder;
                set
                {
                    _placeholder = value ?? string.Empty;
                    Apply();
                }
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                Apply();
            }

            private void Apply()
            {
                if (IsHandleCreated)
                    SendMessageW(Handle, EM_SETCUEBANNER, (IntPtr)1, _placeholder);
            }
        }

        private readonly GraphPresenter _presenter;
        private readonly GraphTheme _theme;
        private readonly IReadOnlyList<INodeTemplate> _templates;
        private readonly Func<Vector2> _spawnAnchorCanvas;
        private readonly PlaceholderTextBox _input;
        private readonly ListBox _list;
        private readonly ToolTip _tip;
        private int _hoverIndex = -1;

        public SearchPalette(
            GraphPresenter presenter,
            GraphTheme theme,
            IReadOnlyList<INodeTemplate> templates,
            Func<Vector2> spawnAnchorCanvas)
        {
            _presenter = presenter;
            _theme = theme;
            _templates = templates;
            _spawnAnchorCanvas = spawnAnchorCanvas;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(420, 360);
            BackColor = theme.MenuBackground;
            ForeColor = theme.MenuText;
            Padding = new Padding(1);

            _input = new PlaceholderTextBox
            {
                Dock = DockStyle.Top,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = theme.NodeBackground,
                ForeColor = theme.MenuText,
                Font = new Font(theme.FontFamily, 11f),
                Placeholder = "Search anything",
            };
            _input.TextChanged += (_, _) => RefreshResults();
            _input.KeyDown += OnInputKey;

            _list = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = theme.MenuBackground,
                ForeColor = theme.MenuText,
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawVariable,
                Font = new Font(theme.FontFamily, 10f),
            };
            _list.MeasureItem += (_, e) => MeasureRow(e);
            _list.DrawItem += (_, e) => DrawRow(e);
            _list.MouseDoubleClick += (_, _) => InvokeSelected();
            _list.SelectedIndexChanged += (_, _) => SkipHeaders();
            _list.MouseMove += (_, e) => SetHover(_list.IndexFromPoint(e.Location));
            _list.MouseLeave += (_, _) => SetHover(-1);

            Controls.Add(_list);
            Controls.Add(_input);

            _tip = new ToolTip
            {
                AutoPopDelay = 15000,
                InitialDelay = 600,
                ReshowDelay = 100,
            };

            Deactivate += (_, _) => Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tip.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _input.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var border = new Pen(_theme.MenuBorder);
            e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        }

        private void OnInputKey(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Down:
                    MoveSelection(+1);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Up:
                    MoveSelection(-1);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Enter:
                    InvokeSelected();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Escape:
                    Close();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void RefreshResults()
        {
            var results = NodeSearch.Run(_templates, _input.Text);
            _hoverIndex = -1;
            _list.BeginUpdate();
            _list.Items.Clear();

            string lastSection = string.Empty;
            int firstSelectable = -1;
            foreach (var r in results)
            {
                string section = NodeSearch.SectionLabel(r.MatchedFieldKind);
                if (section != lastSection)
                {
                    _list.Items.Add(new SectionHeader(section));
                    lastSection = section;
                }
                int idx = _list.Items.Add(r);
                if (firstSelectable < 0) firstSelectable = idx;
            }

            if (firstSelectable >= 0) _list.SelectedIndex = firstSelectable;
            _list.EndUpdate();
        }

        private void InvokeSelected()
        {
            if (_list.SelectedItem is not SearchResult r) return;
            _presenter.Spawn(r.Template, _spawnAnchorCanvas());
            Close();
        }

        private void MoveSelection(int direction)
        {
            int n = _list.Items.Count;
            if (n == 0) return;
            int next = _list.SelectedIndex;
            while (true)
            {
                next += direction;
                if (next < 0 || next >= n) return;
                if (_list.Items[next] is SearchResult)
                {
                    _list.SelectedIndex = next;
                    return;
                }
            }
        }

        private void SetHover(int index)
        {
            if (index >= 0 && index < _list.Items.Count
                && _list.Items[index] is not SearchResult)
                index = -1;
            if (index == _hoverIndex) return;

            int prev = _hoverIndex;
            _hoverIndex = index;
            if (prev >= 0 && prev < _list.Items.Count)
                _list.Invalidate(_list.GetItemRectangle(prev));
            if (index >= 0 && index < _list.Items.Count)
                _list.Invalidate(_list.GetItemRectangle(index));

            UpdateTooltip();
        }

        private void UpdateTooltip()
        {
            string text = string.Empty;
            if (_hoverIndex >= 0 && _hoverIndex < _list.Items.Count
                && _list.Items[_hoverIndex] is SearchResult r
                && r.MatchedFieldKind != "title")
            {
                string line1 = string.IsNullOrEmpty(r.Vendor)
                    ? r.Label
                    : $"{r.Label} - {r.Vendor}";
                string line2 = $"{r.MatchedFieldKind}: {Truncate(r.MatchedFieldText, 300)}";
                text = line1 + Environment.NewLine + Environment.NewLine + line2;
            }
            _tip.SetToolTip(_list, text);
        }

        private static string Truncate(string s, int max)
        {
            if (s.Length <= max) return s;
            return s.Substring(0, max).TrimEnd() + "...";
        }

        private static Color SectionColor(string sectionLabel) => sectionLabel switch
        {
            "Titles" => Color.FromArgb(50, 75, 110),
            "Pin names" => Color.FromArgb(50, 95, 95),
            "Pin types" => Color.FromArgb(80, 60, 100),
            "Vendors" => Color.FromArgb(105, 80, 50),
            "Descriptions" => Color.FromArgb(70, 70, 75),
            _ => Color.FromArgb(60, 60, 66),
        };

        private static Color HoverColor(GraphTheme theme) =>
            Color.FromArgb(
                (theme.MenuBackground.R + theme.MenuHover.R) / 2,
                (theme.MenuBackground.G + theme.MenuHover.G) / 2,
                (theme.MenuBackground.B + theme.MenuHover.B) / 2);

        private void SkipHeaders()
        {
            int idx = _list.SelectedIndex;
            if (idx < 0 || idx >= _list.Items.Count) return;
            if (_list.Items[idx] is not SectionHeader) return;

            for (int i = idx + 1; i < _list.Items.Count; i++)
                if (_list.Items[i] is SearchResult) { _list.SelectedIndex = i; return; }
            for (int i = idx - 1; i >= 0; i--)
                if (_list.Items[i] is SearchResult) { _list.SelectedIndex = i; return; }
        }

        private void MeasureRow(MeasureItemEventArgs e)
        {
            float lineHeight = e.Graphics.MeasureString("Mg", _list.Font).Height;
            int oneLine = (int)Math.Ceiling(lineHeight);
            object? item = e.Index >= 0 && e.Index < _list.Items.Count ? _list.Items[e.Index] : null;
            e.ItemHeight = item switch
            {
                SectionHeader => oneLine + 6,
                SearchResult r when r.MatchedFieldKind != "title" => oneLine * 2 + 8,
                _ => oneLine + 8,
            };
        }

        private void DrawRow(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _list.Items.Count) return;
            switch (_list.Items[e.Index])
            {
                case SectionHeader h:
                    DrawSectionHeader(e, h);
                    break;
                case SearchResult r:
                    DrawResult(e, r);
                    break;
            }
        }

        private void DrawSectionHeader(DrawItemEventArgs e, SectionHeader h)
        {
            using var bg = new SolidBrush(SectionColor(h.Label));
            e.Graphics.FillRectangle(bg, e.Bounds);

            var labelFont = e.Font ?? Font;
            using var headerFont = new Font(labelFont, FontStyle.Bold);
            using var brush = new SolidBrush(_theme.MenuText);
            var size = e.Graphics.MeasureString(h.Label, headerFont);
            float x = e.Bounds.X + (e.Bounds.Width - size.Width) / 2f;
            float y = e.Bounds.Y + (e.Bounds.Height - size.Height) / 2f;
            e.Graphics.DrawString(h.Label, headerFont, brush, x, y);
        }

        private void DrawResult(DrawItemEventArgs e, SearchResult r)
        {
            bool selected = (e.State & DrawItemState.Selected) != 0;
            bool hovered = !selected && e.Index == _hoverIndex;

            Color bgColor = selected
                ? _theme.MenuHover
                : hovered
                    ? HoverColor(_theme)
                    : _theme.MenuBackground;
            using var bg = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bg, e.Bounds);

            var labelFont = e.Font ?? Font;
            using var detailFont = new Font(labelFont, FontStyle.Italic);
            using var labelBrush = new SolidBrush(_theme.MenuText);
            using var detailBrush = new SolidBrush(_theme.FooterText);

            const int padX = 8;
            const int padY = 4;

            e.Graphics.DrawString(r.Label, labelFont, labelBrush,
                e.Bounds.X + padX, e.Bounds.Y + padY);

            if (!string.IsNullOrEmpty(r.Vendor))
            {
                var vendorSize = e.Graphics.MeasureString(r.Vendor, detailFont);
                e.Graphics.DrawString(r.Vendor, detailFont, detailBrush,
                    e.Bounds.Right - vendorSize.Width - padX,
                    e.Bounds.Y + padY);
            }

            if (r.MatchedFieldKind != "title")
            {
                float lineHeight = e.Graphics.MeasureString("Mg", labelFont).Height;
                string snippet = Truncate(r.MatchedFieldText, 70);
                string match = $"{r.MatchedFieldKind}: {snippet}";
                e.Graphics.DrawString(match, detailFont, detailBrush,
                    e.Bounds.X + padX,
                    e.Bounds.Y + padY + lineHeight);
            }
        }

    }
}
