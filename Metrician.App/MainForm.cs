// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Collections.ObjectModel;

using Metrician.Contracts.Renderables;
using Metrician.Viewport;

namespace Metrician.App
{
    internal sealed class MainForm : Form
    {
        private const string ViewportTitle = "Metrician Viewports";
        private const string GraphTitle = "Metrician Graph";

        private readonly WindowController _controller;
        private readonly FourUpViewport _viewport;

        public MainForm(WindowController controller)
        {
            _controller = controller;

            Text = ViewportTitle;
            ClientSize = new Size(1280, 800);
            BackColor = Color.FromArgb(30, 30, 35);
            ForeColor = Color.FromArgb(220, 220, 220);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            Icon = AppIcon.Load();

            _viewport = new FourUpViewport { Dock = DockStyle.Fill };
        }

        public ObservableCollection<IRenderable> Renderables => _viewport.Renderables;

        /// <summary>Shows only the viewport. Idempotent.</summary>
        public void ShowViewport()
        {
            SuspendLayout();
            for (int i = Controls.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(Controls[i], _viewport))
                    Controls[i].Parent = null;
            }
            if (_viewport.Parent != this)
                Controls.Add(_viewport);
            ResumeLayout(performLayout: true);
            Text = ViewportTitle;
        }

        /// <summary>Hosts the supplied graph control in place of the viewport.</summary>
        public void HostGraph(Control graphHost)
        {
            SuspendLayout();
            for (int i = Controls.Count - 1; i >= 0; i--)
                Controls[i].Parent = null;
            graphHost.Dock = DockStyle.Fill;
            Controls.Add(graphHost);
            ResumeLayout(performLayout: true);
            Text = GraphTitle;
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
                    case SystemMenu.IDM_LICENCE:    ShowLicence();                                return;
                    case SystemMenu.IDM_ABOUT:      ShowAbout();                                  return;
                }
            }
            base.WndProc(ref m);
        }

        private void ShowAbout()
        {
            using var dlg = new AboutBox();
            dlg.ShowDialog(this);
        }

        private void ShowLicence()
        {
            using var dlg = new LicenceBox();
            dlg.ShowDialog(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // The graph host is owned by WindowController; detach before disposal.
            for (int i = Controls.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(Controls[i], _viewport))
                    Controls[i].Parent = null;
            }
            base.OnFormClosing(e);
        }
    }
}
