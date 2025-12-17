using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace WashinPC.Engines.Registry
{
    internal static class RegistryBackup
    {
        /// <summary>
        /// Папка с бэкапами реестра
        /// </summary>
        public static string GetBackupRoot()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "WashinPC_Backups",
                "Registry");

            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Экспортирует конкретный ключ реестра в .reg (точечный бэкап)
        /// </summary>
        public static string ExportKey(RegistryHive hive, RegistryView view, string subKeyPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subKeyPath))
                    return null;

                string hiveStr =
                    hive == RegistryHive.LocalMachine ? "HKLM" :
                    hive == RegistryHive.CurrentUser ? "HKCU" :
                    null;

                if (hiveStr == null)
                    return null;

                subKeyPath = subKeyPath.Trim().TrimStart('\\');

                var backupDir = GetBackupRoot();
                var safeName = MakeSafeFileName($"{hiveStr}_{ViewToString(view)}_{subKeyPath}");
                if (safeName.Length > 140)
                    safeName = safeName.Substring(0, 140);

                var file = Path.Combine(
                    backupDir,
                    $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{safeName}.reg");

                var fullKey = $"{hiveStr}\\{subKeyPath}";
                var args = $"export \"{fullKey}\" \"{file}\" /y {RegViewArg(view)}";

                var psi = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var p = Process.Start(psi))
                {
                    p.WaitForExit();
                    if (p.ExitCode != 0 || !File.Exists(file))
                        return null;
                }

                return file;
            }
            catch
            {
                return null;
            }
        }

        private static string ViewToString(RegistryView view)
            => view == RegistryView.Registry32 ? "x86" : "x64";

        private static string RegViewArg(RegistryView view)
            => view == RegistryView.Registry32 ? "/reg:32" : "/reg:64";

        private static string MakeSafeFileName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');

            return s
                .Replace('\\', '_')
                .Replace('/', '_')
                .Replace(':', '_')
                .Replace('*', '_')
                .Replace('?', '_')
                .Replace('"', '_')
                .Replace('<', '_')
                .Replace('>', '_')
                .Replace('|', '_');
        }
    }
}
