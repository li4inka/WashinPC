using System;
using System.IO;
using System.Linq;

namespace WashinPC.Core.Logging
{
    public enum AppLogLevel
    {
        Info,
        Warn,
        Error
    }

    public static class AppLogger
    {
        private static readonly object Sync = new object();
        private const long MaxLogFileBytes = 2 * 1024 * 1024;
        private const int MaxLogFiles = 5;
        private static readonly string LogDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WashinPC", "logs");
        private static readonly string LogFile = Path.Combine(LogDir, "washinpc.log");

        public static string CurrentLogFile => LogFile;

        public static void Info(string subsystem, string eventId, string message, string operationId = null)
            => Write(AppLogLevel.Info, subsystem, eventId, message, operationId);

        public static void Warn(string subsystem, string eventId, string message, string operationId = null)
            => Write(AppLogLevel.Warn, subsystem, eventId, message, operationId);

        public static void Error(string subsystem, string eventId, string message, Exception ex = null, string operationId = null)
        {
            var full = ex == null ? message : (message + " | " + ex.GetType().Name + ": " + ex.Message);
            Write(AppLogLevel.Error, subsystem, eventId, full, operationId);
        }

        public static void Write(
            AppLogLevel level,
            string subsystem,
            string eventId,
            string message,
            string operationId = null,
            bool containsSensitiveData = false)
        {
            try
            {
                var effectiveMessage = message ?? string.Empty;
                if (!Core.AppSettings.VerboseDiagnostics)
                    effectiveMessage = MaskSensitive(effectiveMessage);

                lock (Sync)
                {
                    Directory.CreateDirectory(LogDir);
                    RotateIfNeeded();

                    var line = string.Format(
                        "{0:yyyy-MM-ddTHH:mm:ss.fffZ} | {1} | {2} | {3} | {4} | {5}",
                        DateTime.UtcNow,
                        level.ToString().ToUpperInvariant(),
                        string.IsNullOrWhiteSpace(subsystem) ? "general" : subsystem,
                        string.IsNullOrWhiteSpace(eventId) ? "event" : eventId,
                        string.IsNullOrWhiteSpace(operationId) ? "-" : operationId,
                        effectiveMessage.Replace("\r", " ").Replace("\n", " "));

                    File.AppendAllText(LogFile, line + Environment.NewLine);
                }
            }
            catch
            {
                // Never crash application due to logging failure.
            }
        }

        private static string MaskSensitive(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
                text = text.Replace(userProfile, "%USERPROFILE%");

            return text;
        }

        private static void RotateIfNeeded()
        {
            if (!File.Exists(LogFile))
                return;

            var fileInfo = new FileInfo(LogFile);
            if (fileInfo.Length < MaxLogFileBytes)
                return;

            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var archive = Path.Combine(LogDir, "washinpc_" + stamp + ".log");
            File.Move(LogFile, archive);

            var archives = Directory.GetFiles(LogDir, "washinpc_*.log")
                .Select(path => new FileInfo(path))
                .OrderByDescending(info => info.CreationTimeUtc)
                .ToList();

            for (int i = MaxLogFiles; i < archives.Count; i++)
            {
                try
                {
                    archives[i].Delete();
                }
                catch
                {
                    // Keep best-effort cleanup.
                }
            }
        }
    }
}
