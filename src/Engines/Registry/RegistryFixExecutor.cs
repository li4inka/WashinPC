using System;
using System.IO;
using Microsoft.Win32;

namespace WashinPC.Engines.Registry
{
    internal static class RegistryFixExecutor
    {
        internal static RegistryFixResult ExecuteLegacyFix(
            RegistryFixContext context,
            RegistryHive hive,
            RegistryView view,
            string keyPathUi,
            Func<bool, RegistryFixResult> legacyFix,
            Func<RegistryHive, RegistryView, string, string> backupExporter = null,
            Func<bool> revalidate = null)
        {
            if (context == null || legacyFix == null)
                return RegistryFixResult.Failed;

            if (!context.AllowMutation)
                return RegistryFixResult.Skipped;

            if (revalidate != null && !revalidate()) return RegistryFixResult.Skipped;

            // Backup is mandatory, even for legacy callers that pass CreateBackup=false.
            {
                var exporter = backupExporter ?? RegistryBackup.ExportKey;
                var subKeyPath = ExtractSubKeyPath(hive, keyPathUi);
                var backup = exporter(hive, view, subKeyPath);

                if (!string.IsNullOrWhiteSpace(backup))
                {
                    context.Backups.Add(backup);
                    context.Logs.Add("Backup: " + backup);
                }
                else
                {
                    context.Logs.Add("Backup failed: " + keyPathUi);
                    return RegistryFixResult.Failed;
                }
            }

            if (revalidate != null && !revalidate()) return RegistryFixResult.Skipped;
            RegistryFixResult result;
            try { result = legacyFix(false); }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException)
            {
                using (var root = RegistryKey.OpenBaseKey(hive, view))
                using (var key = root.OpenSubKey(ExtractSubKeyPath(hive, keyPathUi)))
                    return key == null ? RegistryFixResult.Skipped : RegistryFixResult.Failed;
            }
            if (result == RegistryFixResult.None)
                return RegistryFixResult.Skipped;

            return result;
        }

        internal static string ExtractSubKeyPath(RegistryHive hive, string keyPathUi)
        {
            if (string.IsNullOrWhiteSpace(keyPathUi))
                return keyPathUi;

            var prefix = HiveToUi(hive) + "\\";
            if (keyPathUi.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return keyPathUi.Substring(prefix.Length);

            return keyPathUi.TrimStart('\\');
        }

        private static string HiveToUi(RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.LocalMachine: return "HKLM";
                case RegistryHive.CurrentUser: return "HKCU";
                case RegistryHive.ClassesRoot: return "HKCR";
                case RegistryHive.Users: return "HKU";
                default: return hive.ToString();
            }
        }
    }
}
