// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Numerics;
using System.Reflection;
using Metrician.Core.Graph;
using Metrician.Core.Scripting;
using Metrician.Core.ScriptBinding;
using Metrician.Library.Renderables;
using Metrician.Model.Graph;

namespace Metrician.Presentation.Graph
{
    public sealed class PropertyPane : Panel
    {
        private const string PositionRowKey = "__position";
        private const string ColorEditorTag = "color-editor";
        private const int DefaultDescriptionHeight = 120;
        private const int MinPropertiesHeight = 60;
        private const int MinDescriptionHeight = 40;
        private const string PlaceholderText = "No description";

        private readonly IGraphWorld _world;
        private readonly GraphPresenter _presenter;
        private readonly GraphTheme _theme;
        private readonly Dictionary<string, Control> _editorsByProperty =
            new(StringComparer.Ordinal);

        private readonly SplitContainer _split;
        private readonly Panel _scrollHost;
        private readonly TextBox _descriptionBox;

        private NodeId? _node;
        private PinId? _pin;
        private bool _suppressCommit;
        private bool _updatingScrollbars;
        private bool _initialSplitApplied;

        public PropertyPane(IGraphWorld world, GraphPresenter presenter, GraphTheme theme)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _theme = theme ?? throw new ArgumentNullException(nameof(theme));

            BackColor = _theme.Background;
            ForeColor = _theme.Text;
            Padding = Padding.Empty;

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                FixedPanel = FixedPanel.Panel2,
                BackColor = _theme.MenuBorder,
                SplitterWidth = 4,
                Panel1MinSize = MinPropertiesHeight,
                Panel2MinSize = MinDescriptionHeight,
            };
            _split.Panel1.BackColor = _theme.Background;
            _split.Panel2.BackColor = _theme.Background;
            _split.Panel2.Padding = new Padding(8, 6, 8, 8);

            _scrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _theme.Background,
                AutoScroll = true,
                Padding = Padding.Empty,
            };
            _split.Panel1.Controls.Add(_scrollHost);

            _descriptionBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = ScrollBars.None,
                BackColor = _theme.NodeBackground,
                ForeColor = _theme.Text,
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false,
                Cursor = Cursors.Default,
            };
            _descriptionBox.ClientSizeChanged += (_, _) => UpdateDescriptionScrollbars();
            _descriptionBox.TextChanged += (_, _) => UpdateDescriptionScrollbars();
            _split.Panel2.Controls.Add(_descriptionBox);

            Controls.Add(_split);

            _world.Properties.Changed += OnPropertyChanged;
            _world.Layout.Changed += OnLayoutChanged;
            _world.Nodes.Removed += OnNodeRemoved;
            _world.RenderOptions.Changed += OnRenderOptionsChanged;
            _world.Wires.Connected += OnWireChanged;
            _world.Wires.Disconnected += OnWireChanged;

            BuildEmpty();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (_initialSplitApplied) return;
            int total = _split.Height;
            int needed = MinPropertiesHeight + MinDescriptionHeight + _split.SplitterWidth;
            if (total < needed) return;
            int distance = total - DefaultDescriptionHeight;
            _split.SplitterDistance = Math.Clamp(
                distance, MinPropertiesHeight, total - MinDescriptionHeight);
            _initialSplitApplied = true;
        }

        private void SetDescription(string? text)
        {
            bool hasText = !string.IsNullOrWhiteSpace(text);
            _descriptionBox.Font = new Font(
                _theme.FontFamily, 9,
                hasText ? FontStyle.Regular : FontStyle.Italic);
            _descriptionBox.ForeColor = hasText ? _theme.Text : _theme.FooterText;
            _descriptionBox.Text = hasText ? text! : PlaceholderText;
            _descriptionBox.SelectionStart = 0;
            _descriptionBox.SelectionLength = 0;
            UpdateDescriptionScrollbars();
        }

        public void ShowFor(NodeId? id)
        {
            _node = id;
            if (_pin is { } p && (id is null || p.Owner != id))
                _pin = null;
            Rebuild();
        }

        public void ShowForPin(PinId? pin)
        {
            if (Nullable.Equals(_pin, pin)) return;
            _pin = pin;
            Rebuild();
        }

        private void UpdateDescriptionScrollbars()
        {
            if (_updatingScrollbars) return;
            ScrollBars desired = ScrollBars.None;
            if (!string.IsNullOrEmpty(_descriptionBox.Text) &&
                _descriptionBox.ClientSize.Width > 0 &&
                _descriptionBox.ClientSize.Height > 0)
            {
                var measured = TextRenderer.MeasureText(
                    _descriptionBox.Text,
                    _descriptionBox.Font,
                    new Size(_descriptionBox.ClientSize.Width, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                if (measured.Height > _descriptionBox.ClientSize.Height)
                    desired = ScrollBars.Vertical;
            }
            if (_descriptionBox.ScrollBars == desired) return;
            try
            {
                _updatingScrollbars = true;
                _descriptionBox.ScrollBars = desired;
            }
            finally { _updatingScrollbars = false; }
        }

        private void BuildForRenderNode(NodeId nodeId, Node node)
        {
            var connectedPins = _world.Pins.Inputs(nodeId)
                .Where(p => _world.Wires.SourceOf(p.Id) is not null)
                .ToList();

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = _theme.Background,
                ForeColor = _theme.Text,
                Padding = new Padding(8, 8, 8, 8),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            var header = new Label
            {
                Text = node.Title,
                AutoSize = true,
                Font = new Font(_theme.FontFamily, 10, FontStyle.Bold),
                ForeColor = _theme.Text,
                BackColor = _theme.Background,
                Margin = new Padding(0, 0, 0, 6),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            grid.Controls.Add(header, 0, 0);
            grid.SetColumnSpan(header, 2);
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            int row = 1;

            if (connectedPins.Count == 0)
            {
                var hint = new Label
                {
                    Text = "No connected inputs",
                    AutoSize = true,
                    Font = new Font(_theme.FontFamily, 9, FontStyle.Italic),
                    ForeColor = _theme.FooterText,
                    BackColor = _theme.Background,
                };
                grid.Controls.Add(hint, 0, row);
                grid.SetColumnSpan(hint, 2);
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _scrollHost.Controls.Add(grid);
                if (_pin is not null)
                {
                    _pin = null;
                    _presenter.SelectPin(null);
                }
                SetDescription(node.Description);
                return;
            }

            var activePin = _pin is { } sp && connectedPins.Any(p => p.Id == sp)
                ? sp
                : connectedPins[0].Id;

            if (_pin != activePin)
            {
                _pin = activePin;
                _presenter.SelectPin(activePin);
            }

            var pinCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = _theme.NodeBackground,
                ForeColor = _theme.Text,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 2, 0, 2),
            };
            foreach (var p in connectedPins) pinCombo.Items.Add(p.Id.Name);

            try
            {
                _suppressCommit = true;
                pinCombo.SelectedIndex = connectedPins.FindIndex(p => p.Id == activePin);
            }
            finally { _suppressCommit = false; }

            var pinsCaptured = connectedPins.Select(p => p.Id).ToList();
            pinCombo.SelectedIndexChanged += (_, _) =>
            {
                if (_suppressCommit) return;
                int idx = pinCombo.SelectedIndex;
                if (idx < 0 || idx >= pinsCaptured.Count) return;
                _presenter.SelectPin(pinsCaptured[idx]);
            };

            AddRow(grid, ref row, "Pin", pinCombo);

            BuildOptionsEditors(grid, ref row, activePin);

            _scrollHost.Controls.Add(grid);
            SetDescription(node.Description);
        }

        private void BuildOptionsEditors(TableLayoutPanel grid, ref int row, PinId pin)
        {
            var registry = _world.Resolve<IRenderableRegistry>();
            if (registry is null) return;

            var source = _world.Wires.SourceOf(pin);
            if (source is not { } src) return;
            var sourcePin = _world.Pins.Get(src);
            if (sourcePin is null) return;

            if (!registry.TryGetOptionsType(sourcePin.ValueType, out var optionsType) ||
                optionsType is null) return;

            var existing = _world.RenderOptions.Get(pin);
            var instance = existing is not null && optionsType.IsInstanceOfType(existing)
                ? existing
                : Activator.CreateInstance(optionsType)
                  ?? throw new InvalidOperationException(
                      $"Render options type '{optionsType.Name}' has no parameterless constructor.");

            foreach (var prop in optionsType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                if (prop.GetIndexParameters().Length > 0) continue;
                if (!IsEditableType(prop.PropertyType)) continue;

                var capturedProp = prop;
                var editor = BuildEditor(prop.PropertyType, prop.GetValue(instance), value =>
                {
                    try
                    {
                        capturedProp.SetValue(instance, value);
                        _world.RenderOptions.Set(pin, instance);
                    }
                    catch
                    {
                        /* ignore type mismatch */
                    }
                });
                AddRow(grid, ref row, prop.Name, editor);
            }
        }

        private static bool IsEditableType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying == typeof(bool)) return true;
            if (underlying == typeof(Color)) return true;
            if (underlying.IsEnum) return true;
            return PropertyValueText.IsSupported(underlying);
        }

        private void OnRenderOptionsChanged(object? sender, PinId pin)
        {
            if (_node is { } n && pin.Owner == n) Rebuild();
        }

        private void OnWireChanged(object? sender, Wire wire)
        {
            if (_node is { } n && wire.Target.Owner == n) Rebuild();
        }

        private void BuildEmpty()
        {
            _scrollHost.Controls.Clear();
            _scrollHost.AutoScrollPosition = Point.Empty;
            _editorsByProperty.Clear();
            SetDescription(null);

            var empty = new Label
            {
                Text = "No selection",
                Dock = DockStyle.Top,
                ForeColor = _theme.FooterText,
                BackColor = _theme.Background,
                AutoSize = false,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0),
                Font = new Font(_theme.FontFamily, 9, FontStyle.Italic),
            };
            _scrollHost.Controls.Add(empty);
        }

        private void Rebuild()
        {
            _scrollHost.Controls.Clear();
            _scrollHost.AutoScrollPosition = Point.Empty;
            _editorsByProperty.Clear();

            if (_node is not { } id)
            {
                BuildEmpty();
                return;
            }
            var node = _world.Nodes.Get(id);
            if (node is null)
            {
                BuildEmpty();
                return;
            }

            if (_world.Tags.Has(id, "render"))
            {
                BuildForRenderNode(id, node);
                return;
            }

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                BackColor = _theme.Background,
                ForeColor = _theme.Text,
                Padding = new Padding(8, 8, 8, 8),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            var header = new Label
            {
                Text = node.Title,
                AutoSize = true,
                Font = new Font(_theme.FontFamily, 10, FontStyle.Bold),
                ForeColor = _theme.Text,
                BackColor = _theme.Background,
                Margin = new Padding(0, 0, 0, 6),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            grid.Controls.Add(header, 0, 0);
            grid.SetColumnSpan(header, 2);
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            int row = 1;

            if (_world.Layout.Get(id) is { } position)
            {
                var editor = BuildEditor(typeof(Vector2), position,
                    value => CommitPosition(id, value));
                AddRow(grid, ref row, "Position", editor);
                _editorsByProperty[PositionRowKey] = editor;
            }

            foreach (var property in _world.Properties.PropertiesOf(id))
            {
                if (!PropertyValueText.IsSupported(property.Type)) continue;
                var captured = property;
                var editor = BuildEditor(captured.Type, captured.Value,
                    value => CommitProperty(id, captured.Name, value));
                AddRow(grid, ref row, captured.Name, editor);
                _editorsByProperty[captured.Name] = editor;
            }

            _scrollHost.Controls.Add(grid);

            SetDescription(node.Description);
        }

        private void AddRow(TableLayoutPanel grid, ref int row, string label, Control editor)
        {
            var lbl = new Label
            {
                Text = label,
                AutoSize = false,
                Dock = DockStyle.Fill,
                ForeColor = _theme.Text,
                BackColor = _theme.Background,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 6, 4),
            };
            editor.Margin = new Padding(0, 2, 0, 2);
            grid.Controls.Add(lbl, 0, row);
            grid.Controls.Add(editor, 1, row);
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            row++;
        }

        private Control BuildEditor(Type type, object? value, Action<object?> commit)
        {
            Type underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying == typeof(bool)) return BuildBoolEditor(value, commit);
            if (underlying.IsEnum) return BuildEnumEditor(underlying, value, commit);
            if (underlying == typeof(Color)) return BuildColorEditor(value, commit);
            return BuildTextEditor(type, value, commit);
        }

        private CheckBox BuildBoolEditor(object? value, Action<object?> commit)
        {
            var cb = new CheckBox
            {
                Checked = value is bool b && b,
                BackColor = _theme.Background,
                ForeColor = _theme.Text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
            };
            cb.CheckedChanged += (_, _) =>
            {
                if (_suppressCommit) return;
                commit(cb.Checked);
            };
            return cb;
        }

        private ComboBox BuildEnumEditor(Type enumType, object? value, Action<object?> commit)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = _theme.NodeBackground,
                ForeColor = _theme.Text,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
            };
            foreach (var name in Enum.GetNames(enumType))
                combo.Items.Add(name);
            if (value is not null)
                combo.SelectedItem = value.ToString();

            combo.SelectedIndexChanged += (_, _) =>
            {
                if (_suppressCommit) return;
                if (combo.SelectedItem is string name &&
                    EnumValueParser.TryParse(enumType, name, out var parsed))
                    commit(parsed);
            };
            return combo;
        }

        private Button BuildColorEditor(object? value, Action<object?> commit)
        {
            var initial = value is Color c ? c : Color.Black;
            var btn = new Button
            {
                Text = string.Empty,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Height = 22,
                Tag = ColorEditorTag,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
            };
            btn.FlatAppearance.BorderColor = _theme.NodeBorder;
            btn.FlatAppearance.BorderSize = 1;
            ApplySwatch(btn, initial);

            btn.Click += (_, _) =>
            {
                if (_suppressCommit) return;
                using var dialog = new ColorDialog
                {
                    Color = btn.BackColor,
                    FullOpen = true,
                    AnyColor = true,
                };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                var picked = Color.FromArgb(btn.BackColor.A, dialog.Color);
                ApplySwatch(btn, picked);
                commit(picked);
            };
            return btn;
        }

        private static void ApplySwatch(Button btn, Color colour)
        {
            btn.BackColor = colour;
            btn.FlatAppearance.MouseOverBackColor = colour;
            btn.FlatAppearance.MouseDownBackColor = colour;
        }

        private TextBox BuildTextEditor(Type type, object? value, Action<object?> commit)
        {
            var tb = new TextBox
            {
                Text = PropertyValueText.Format(value),
                BackColor = _theme.NodeBackground,
                ForeColor = _theme.Text,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
            };
            void Commit()
            {
                if (_suppressCommit) return;
                try
                {
                    var parsed = PropertyValueText.Parse(tb.Text, type);
                    tb.BackColor = _theme.NodeBackground;
                    commit(parsed);
                }
                catch
                {
                    tb.BackColor = Color.FromArgb(80, 30, 30);
                }
            }
            tb.LostFocus += (_, _) => Commit();
            tb.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    Commit();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            return tb;
        }

        private void CommitPosition(NodeId id, object? value)
        {
            if (value is Vector2 v)
                _world.Layout.Set(id, v);
        }

        private void CommitProperty(NodeId id, string name, object? value)
        {
            try { _world.Properties.Set(id, name, value); }
            catch { /* type mismatch or unknown property; ignore */ }
        }

        private void OnPropertyChanged(object? sender, (NodeId Id, string Name) e)
        {
            if (_node is not { } current || current != e.Id) return;
            if (!_editorsByProperty.TryGetValue(e.Name, out var editor)) return;
            var prop = _world.Properties.PropertiesOf(current).FirstOrDefault(p =>
                string.Equals(p.Name, e.Name, StringComparison.Ordinal));
            if (prop is null) return;
            UpdateEditor(editor, prop.Value);
        }

        private void OnLayoutChanged(object? sender, NodeId id)
        {
            if (_node is not { } current || current != id) return;
            if (!_editorsByProperty.TryGetValue(PositionRowKey, out var editor)) return;
            if (_world.Layout.Get(id) is { } pos)
                UpdateEditor(editor, pos);
        }

        private void OnNodeRemoved(object? sender, NodeId id)
        {
            if (_node is { } current && current == id)
                ShowFor(null);
        }

        private void UpdateEditor(Control editor, object? value)
        {
            if (editor.Focused) return;
            try
            {
                _suppressCommit = true;
                switch (editor)
                {
                    case CheckBox cb: cb.Checked = value is bool b && b; break;
                    case ComboBox combo: combo.SelectedItem = value?.ToString(); break;
                    case Button btn when (btn.Tag as string) == ColorEditorTag:
                        ApplySwatch(btn, value is Color c ? c : Color.Black);
                        break;
                    case TextBox tb: tb.Text = PropertyValueText.Format(value); break;
                }
            }
            finally { _suppressCommit = false; }
        }
    }
}
