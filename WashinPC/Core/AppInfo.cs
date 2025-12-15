using System;
using System.Reflection;

namespace WashinPC
{
    public class AppInfo
    {
        public string AppVersion { get; }
        public string BuildInfo { get; }
        public string Copyright { get; }

        public AppInfo()
        {
            var asm = Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version;

            AppVersion = $"Версия: {version}";

#if DEBUG
            var buildType = "DEBUG";
#else
            var buildType = "RELEASE";
#endif

            BuildInfo =
                $"Сборка: {DateTime.Now:yyyy-MM-dd HH:mm} ({buildType})";

            Copyright =
                "© 2025 Kulich Software";
        }
    }
}
