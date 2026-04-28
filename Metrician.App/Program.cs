// MIT License - Copyright (c) 2026 Alex Jenkins
// See LICENSE file for full terms

namespace Metrician.App
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string? scriptPath = ParseScriptPath(args);

            ApplicationConfiguration.Initialize();
            var ctx = new MultiFormApplicationContext();
            var controller = new WindowController(ctx, scriptPath);
            controller.Start();
            Application.Run(ctx);
        }

        private static string? ParseScriptPath(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "--script" || args[i] == "-s") && i + 1 < args.Length)
                    return args[i + 1];
            }
            return null;
        }
    }
}
