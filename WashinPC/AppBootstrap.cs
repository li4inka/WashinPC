using System;
using System.IO;

namespace WashinPC
{
    internal static class AppBootstrap
    {
        public static readonly string AppDataDir =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WashinPC"
            );

        public static readonly string LogFile =
            Path.Combine(AppDataDir, "washinpc.log");

        public static void Initialize()
        {
            Directory.CreateDirectory(AppDataDir);

            File.AppendAllText(
                LogFile,
                $"START: {DateTime.Now:u} WashinPC launched\r\n"
            );

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    File.AppendAllText(
                        LogFile,
                        $"UNHANDLED: {DateTime.Now:u} {e.ExceptionObject}\r\n"
                    );
                }
                catch { }
            };
        }
    }
}
