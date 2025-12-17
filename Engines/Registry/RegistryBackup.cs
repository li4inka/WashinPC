using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace WashinPC.Engines.Registry
{
    internal static class RegistryBackup
    {
        /// <summary>
        /// Папка с бэкапами реестра (не Desktop, чтобы не зависеть от политики/OneDrive).
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
        /// Экспортирует конкретный ключ реестра в .reg (точечный бэкап).
        /// Возвращает путь к файлу бэкапа или null, если экспорт не удался.
        /// </summary>
        public static string ExportKey(RegistryHive hive, RegistryView view, string subKeyPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subKeyPath))
                    return null;

                // reg.exe export "HKLM\...\path" "file.reg" /y [/reg:32|/reg:64]
                string hiveStr =
                    hive == RegistryHive.LocalMachine ? "HKLM" :
                    hive == RegistryHive.CurrentUser ? "HKCU" :
                    null;

                if (hiveStr == null)
                    return null;

                // Нормализуем ведущие слэши
                subKeyPath = subKeyPath.Trim().TrimStart('\\');

                var backupDir = GetBackupRoot();
                var safeName = MakeSafeFileName($"{hiveStr}_{ViewToString(view)}_{subKeyPath}");
                if (safeName.Length > 140) safeName = safeName.Substring(0, 140);

                var file = Path.Combine(
                    backupDir,
                    $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{safeName}.reg");

                var keyFull = $"{hiveStr}\\{subKeyPath}";
                var args = $"export \"{keyFull}\" \"{file}\" /y {RegViewArg(view)}";

                var (ok, stdout, stderr, exitCode) = RunRegExe(args);

                Utils.Log($"RegistryBackup export {keyFull} -> {file}, exit={exitCode}");
                if (!string.IsNullOrWhiteSpace(stdout)) Utils.Log("RegistryBackup stdout: " + stdout);
                if (!string.IsNullOrWhiteSpace(stderr)) Utils.Log("RegistryBackup stderr: " + stderr);

                if (!ok || exitCode != 0 || !File.Exists(file))
                    return null;

                return file;
            }
            catch (Exception ex)
            {
                Utils.Log("RegistryBackup ExportKey ERROR: " + ex);
                return null;
            }
        }

        private static string ViewToString(RegistryView view)
            => view == RegistryView.Registry32 ? "x86" : "x64";

        private static string RegViewArg(RegistryView view)
        {
            // На 32-бит ОС /reg:64 может не поддерживаться корректно — поэтому пробуем, но падение ловим выше.
            return view == RegistryView.Registry32 ? "/reg:32" : "/reg:64";
        }

        private static (bool ok, string stdout, string stderr, int exitCode) RunRegExe(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                return (true, stdout, stderr, p.ExitCode);
            }
        }

        private static string MakeSafeFileName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');

            // Ещё подчистим "особые" разделители
            s = s.Replace('\\', '_').Replace('/', '_').Replace(':', '_').Replace('*', '_')
                 .Replace('?', '_').Replace('"', '_').Replace('<', '_').Replace('>', '_')
                 .Replace('|', '_');

            return s;
        }
    }
}
