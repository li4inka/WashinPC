using System;
using System.IO;
using WashinPC.Core.Logging;

namespace WashinPC.Core
{
    public static class AppBootstrap
    {
        public static string AppDir { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WashinPC");

        public static string LogFile => AppLogger.CurrentLogFile;

        public static void Initialize()
        {
            Directory.CreateDirectory(AppDir);
            Log("Application start");
        }

        public static void Log(string message)
        {
            AppLogger.Info("bootstrap", "app.log", message);
        }
    }
}
