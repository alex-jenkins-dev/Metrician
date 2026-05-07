// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.App
{
    internal sealed class GraphForm : Form
    {
        private readonly WindowController _controller;

        public GraphForm(WindowController controller)
        {
            _controller = controller;

            Text = "Metrician Graph";
            ClientSize = new Size(900, 600);
            BackColor = Color.FromArgb(30, 30, 35);
            ForeColor = Color.FromArgb(220, 220, 220);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = AppIcon.Load();
        }

        public void HostGraph(Control graphHost)
        {
            graphHost.Dock = DockStyle.Fill;
            Controls.Add(graphHost);
        }

        public void UpdateModeRadio(DisplayMode mode)
        {
            if (!IsHandleCreated) return;
            SystemMenu.UpdateModeRadio(Handle, mode);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SystemMenu.AppendModeAndAboutItems(Handle);
            SystemMenu.UpdateModeRadio(Handle, _controller.ActualMode);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == SystemMenu.WM_SYSCOMMAND)
            {
                int cmd = m.WParam.ToInt32() & 0xFFF0;
                switch (cmd)
                {
                    case SystemMenu.IDM_MODE_3D:    _controller.RequestMode(DisplayMode.ThreeD); return;
                    case SystemMenu.IDM_MODE_GRAPH: _controller.RequestMode(DisplayMode.Graph);  return;
                    case SystemMenu.IDM_MODE_BOTH:  _controller.RequestMode(DisplayMode.Both);   return;
                    case SystemMenu.IDM_LICENCE:
                        using (var dlg = new LicenceBox())
                            dlg.ShowDialog(this);
                        return;
                    case SystemMenu.IDM_ABOUT:
                        using (var dlg = new AboutBox())
                            dlg.ShowDialog(this);
                        return;
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // The graph host is owned by WindowController; detach before disposal.
            for (int i = Controls.Count - 1; i >= 0; i--)
                Controls[i].Parent = null;
            base.OnFormClosing(e);
        }
    }
}
