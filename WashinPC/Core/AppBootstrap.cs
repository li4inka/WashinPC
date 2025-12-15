using System;
using System.IO;

namespace WashinPC.Core
{
    public static class AppBootstrap
    {
        public static string AppDir { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WashinPC");

        public static string LogFile => Path.Combine(AppDir, "app.log");

        public static void Initialize()
        {
            Directory.CreateDirectory(AppDir);
            Log("Application start");
        }

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    LogFile,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}\r\n"
                );
            }
            catch
            {
                // never crash on logging
            }
        }
    }
}
