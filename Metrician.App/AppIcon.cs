// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.App
{
    internal static class AppIcon
    {
        public static Icon Load()
        {
            using var stream = typeof(AppIcon).Assembly
                .GetManifestResourceStream("metrician.ico")
                ?? throw new InvalidOperationException(
                    "Embedded resource 'metrician.ico' not found.");
            return new Icon(stream);
        }
    }
}
