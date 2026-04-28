// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.App
{
    public sealed class LicenceBox : Form
    {
        private const string MitLicence =
            "MIT License\r\n" +
            "\r\n" +
            "Copyright (c) 2026 Alex Jenkins\r\n" +
            "\r\n" +
            "Permission is hereby granted, free of charge, to any person obtaining a copy " +
            "of this software and associated documentation files (the \"Software\"), to deal " +
            "in the Software without restriction, including without limitation the rights " +
            "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell " +
            "copies of the Software, and to permit persons to whom the Software is " +
            "furnished to do so, subject to the following conditions:\r\n" +
            "\r\n" +
            "The above copyright notice and this permission notice shall be included in " +
            "all copies or substantial portions of the Software.\r\n" +
            "\r\n" +
            "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR " +
            "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
            "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE " +
            "AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER " +
            "LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, " +
            "OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN " +
            "THE SOFTWARE.";

        public LicenceBox()
        {
            Text = "Licence";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 35);

            AutoScaleMode = AutoScaleMode.Dpi;

            const int contentWidth = 560;

            var label = new Label
            {
                AutoSize = true,
                MinimumSize = new Size(contentWidth, 0),
                MaximumSize = new Size(contentWidth, 0),
                TextAlign = ContentAlignment.TopLeft,
                Margin = Padding.Empty,
                Padding = new Padding(20),
                Text = MitLicence,
                ForeColor = Color.FromArgb(220, 220, 220),
                BackColor = Color.FromArgb(30, 30, 35),
                Font = new Font("Segoe UI", 9.5f),
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, contentWidth));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            layout.Controls.Add(label, 0, 0);

            Controls.Add(layout);

            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
    }
}
