using System;
using System.IO;
using System.Reflection;

namespace WashinPC
{
    public class AppInfo
    {
        public string AppVersion { get; }
        public string BuildDate { get; }
        public string Copyright { get; }

        public AppInfo()
        {
            var asm = Assembly.GetExecutingAssembly();

            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var fullVersion = info?.InformationalVersion ?? asm.GetName().Version.ToString();

            AppVersion = "Версия: " + FormatVersion(fullVersion);

            BuildDate = "Сборка: " + GetBuildTime(asm).ToString("yyyy-MM-dd HH:mm");

            Copyright = "© 2025 Kulich Software";
        }

        private static DateTime GetBuildTime(Assembly asm)
        {
            try
            {
                var path = asm.Location;
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return File.GetLastWriteTime(path);
            }
            catch { }

            return DateTime.Now;
        }

        private static string FormatVersion(string v)
        {
            var parts = v.Split('+');
            if (parts.Length == 2)
                return $"{parts[0]} + {parts[1].TrimStart('g')}";
            return v;
        }
    }
}
