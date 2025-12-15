using System;
using System.IO;
using System.Reflection;

namespace WashinPC.Core
{
    public class AppInfo
    {
        public string Version { get; }
        public string Build { get; }
        public int Year { get; }

        public AppInfo()
        {
            var asm = Assembly.GetExecutingAssembly();

            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            Version = info?.InformationalVersion
                      ?? asm.GetName().Version?.ToString()
                      ?? "1.0.0";

            Build = GetBuildTime(asm).ToString("yyyy-MM-dd HH:mm");
            Year = DateTime.Now.Year;
        }

        private static DateTime GetBuildTime(Assembly asm)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(asm.Location) && File.Exists(asm.Location))
                    return File.GetLastWriteTime(asm.Location);
            }
            catch { }

            return DateTime.Now;
        }
    }
}
