// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.App
{
    /// <summary>
    /// Tracks top-level forms and ends the message loop only when the last one closes.
    /// </summary>
    internal sealed class MultiFormApplicationContext : ApplicationContext
    {
        private readonly HashSet<Form> _open = new();

        public void Track(Form form)
        {
            if (form.IsDisposed) return;
            if (!_open.Add(form)) return;
            form.FormClosed += OnFormClosed;
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            if (sender is not Form f) return;
            f.FormClosed -= OnFormClosed;
            _open.Remove(f);
            if (_open.Count == 0)
                ExitThread();
        }
    }
}
