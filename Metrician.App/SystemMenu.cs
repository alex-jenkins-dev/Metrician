// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

using System.Runtime.InteropServices;

namespace Metrician.App
{
    internal enum DisplayMode { ThreeD, Graph, Both }

    /// <summary>
    /// Win32 system-menu helpers shared between forms. Custom IDs sit below
    /// 0xF000 (Windows reserves the high range for SC_* built-ins) and are
    /// spaced by 0x10 because WM_SYSCOMMAND masks the low 4 bits of wParam.
    /// </summary>
    internal static class SystemMenu
    {
        [DllImport("user32.dll")]
        public static extern nint GetSystemMenu(nint hWnd, bool bRevert);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool AppendMenu(nint hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

        [DllImport("user32.dll")]
        public static extern bool CheckMenuRadioItem(
            nint hMenu, uint idFirst, uint idLast, uint idCheck, uint uFlags);

        public const uint MF_STRING     = 0x00000000;
        public const uint MF_SEPARATOR  = 0x00000800;
        public const uint MF_BYCOMMAND  = 0x00000000;
        public const int  WM_SYSCOMMAND = 0x0112;

        public const int IDM_MODE_3D    = 0x1000;
        public const int IDM_MODE_GRAPH = 0x1010;
        public const int IDM_MODE_BOTH  = 0x1020;
        public const int IDM_LICENCE    = 0x1040;
        public const int IDM_ABOUT      = 0x1030;

        /// <summary>Appends the standard 3D / Graph / Both / Licence / About items to the window's system menu.</summary>
        public static void AppendModeAndAboutItems(nint hWnd)
        {
            nint sysMenu = GetSystemMenu(hWnd, false);
            AppendMenu(sysMenu, MF_SEPARATOR, 0, null);
            AppendMenu(sysMenu, MF_STRING, IDM_MODE_3D,    "&3D");
            AppendMenu(sysMenu, MF_STRING, IDM_MODE_GRAPH, "&Graph");
            AppendMenu(sysMenu, MF_STRING, IDM_MODE_BOTH,  "&Both");
            AppendMenu(sysMenu, MF_SEPARATOR, 0, null);
            AppendMenu(sysMenu, MF_STRING, IDM_LICENCE,    "&Licence");
            AppendMenu(sysMenu, MF_STRING, IDM_ABOUT,      "&About");
        }

        public static void UpdateModeRadio(nint hWnd, DisplayMode mode)
        {
            nint sysMenu = GetSystemMenu(hWnd, false);
            uint idCheck = mode switch
            {
                DisplayMode.ThreeD => (uint)IDM_MODE_3D,
                DisplayMode.Graph  => (uint)IDM_MODE_GRAPH,
                DisplayMode.Both   => (uint)IDM_MODE_BOTH,
                _                  => (uint)IDM_MODE_3D,
            };
            CheckMenuRadioItem(sysMenu, IDM_MODE_3D, IDM_MODE_BOTH, idCheck, MF_BYCOMMAND);
        }
    }
}
