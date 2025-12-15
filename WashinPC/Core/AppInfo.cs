using System.Reflection;

namespace WashinPC.Core
{
    public static class AppInfo
    {
        public static string Name => "WashinPC";

        public static string Version =>
            Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        public static string Channel => "Open Beta";

        public static string FullTitle => $"{Name} {Version} ({Channel})";
    }
}
