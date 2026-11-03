using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace WashinPC.Engines.Registry
{
    internal static class RegistryBackup
    {
        internal static string TestBackupRoot;
        public static string GetBackupRoot()
        {
            var dir = TestBackupRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WashinPC_Backups", "Registry");
            Directory.CreateDirectory(dir); return dir;
        }
        internal static string HiveName(RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.CurrentUser: return "HKEY_CURRENT_USER";
                case RegistryHive.LocalMachine: return "HKEY_LOCAL_MACHINE";
                case RegistryHive.ClassesRoot: return "HKEY_CLASSES_ROOT";
                case RegistryHive.Users: return "HKEY_USERS";
                default: throw new ArgumentException("Unsupported hive.");
            }
        }
        internal static string Capture(RegistryHive hive, RegistryView view, string path)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(path, false))
            {
                if (key == null) throw new IOException("Registry object is unavailable.");
                var text = new StringBuilder("Windows Registry Editor Version 5.00\r\n\r\n");
                AppendKey(text, key, HiveName(hive) + "\\" + path, 0);
                return text.ToString();
            }
        }
        private static void AppendKey(StringBuilder text, RegistryKey key, string path, int depth)
        {
            if (depth > 64 || path.IndexOfAny(new[] {'\r','\n',']'}) >= 0) throw new IOException("Unsupported registry key.");
            text.Append('[').Append(path).Append("]\r\n");
            foreach (var name in key.GetValueNames().OrderBy(x => x, StringComparer.Ordinal))
            {
                text.Append(ValueName(name)).Append('=').Append(Encode(key.GetValueKind(name), key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames))).Append("\r\n");
            }
            text.Append("\r\n");
            foreach (var name in key.GetSubKeyNames().OrderBy(x => x, StringComparer.Ordinal))
                using (var child = key.OpenSubKey(name, false))
                { if (child == null) throw new IOException(); AppendKey(text, child, path + "\\" + name, depth + 1); }
        }
        internal static string ValueName(string name)
        {
            if (name.IndexOfAny(new[] {'\r','\n','\0'}) >= 0) throw new IOException("Unsupported value name.");
            return name.Length == 0 ? "@" : "\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
        internal static string Encode(RegistryValueKind kind, object value)
        {
            byte[] bytes;
            switch (kind)
            {
                case RegistryValueKind.DWord: return "dword:" + unchecked((uint)(int)value).ToString("x8");
                case RegistryValueKind.QWord: bytes = BitConverter.GetBytes((long)value); break;
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString: bytes = Encoding.Unicode.GetBytes((string)value + "\0"); break;
                case RegistryValueKind.MultiString: bytes = Encoding.Unicode.GetBytes(string.Join("\0", (string[])value) + "\0\0"); break;
                case RegistryValueKind.Binary:
                case RegistryValueKind.None: bytes = (byte[])value; break;
                default: throw new IOException("Unsupported registry type.");
            }
            var type = kind == RegistryValueKind.None ? 0 : (int)kind;
            var encoded = new StringBuilder("hex(" + type.ToString("x") + "):");
            for (var i = 0; i < bytes.Length; i++)
            {
                if (i > 0) encoded.Append(i % 24 == 0 ? ",\\\r\n  " : ",");
                encoded.Append(bytes[i].ToString("x2"));
            }
            return encoded.ToString();
        }
        internal static string CaptureValues(RegistryHive hive, RegistryView view, string path, params string[] names)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(path, false))
            {
                if (key == null) throw new IOException("Registry object is unavailable.");
                var result = new StringBuilder();
                var existing = key.GetValueNames();
                foreach (var name in names)
                {
                    var value = existing.Contains(name, StringComparer.OrdinalIgnoreCase)
                        ? Encode(key.GetValueKind(name), key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames))
                        : "-";
                    result.Append(ValueName(name)).Append('=').Append(value).AppendLine();
                }
                return result.ToString();
            }
        }

        internal static string ExportValue(RegistryHive hive, RegistryView view, string path, string name)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(path, false))
            {
                var exists = key != null && key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase);
                var value = exists ? Encode(key.GetValueKind(name), key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames)) : "-";
                return Save(hive, view, path, "Windows Registry Editor Version 5.00\r\n\r\n[" + HiveName(hive) + "\\" + path + "]\r\n" + ValueName(name) + "=" + value + "\r\n");
            }
        }
        public static string ExportKey(RegistryHive hive, RegistryView view, string path)
        {
            try { return Save(hive, view, path, Capture(hive, view, path)); }
            catch { Core.Logging.AppLogger.Warn("registry", "backup.failed", "Backup failed; mutation is blocked."); return null; }
        }
        internal static string Save(RegistryHive hive, RegistryView view, string path, string text)
        {
            var actualView = view == RegistryView.Default ? (Environment.Is64BitProcess ? RegistryView.Registry64 : RegistryView.Registry32) : view;
            var file = Path.Combine(GetBackupRoot(), DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".reg");
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var preamble = Encoding.Unicode.GetPreamble(); var data = Encoding.Unicode.GetBytes(text);
                stream.Write(preamble, 0, preamble.Length); stream.Write(data, 0, data.Length); stream.Flush(true);
            }
            if (File.ReadAllText(file, Encoding.Unicode) != text) throw new IOException("Backup verification failed.");
            BackupCatalog.WriteMetadata(file, actualView, BackupCatalog.Parse(text).Select(x => RegistryBackup.HiveName(x.Hive) + "\\" + x.Path).Distinct().ToArray());
            var architecture = actualView == RegistryView.Registry64 ? "64" : "32";
            File.WriteAllText(file + ".txt", "WashinPC registry backup / Резервная копия реестра\r\n" + HiveName(hive) + "\\" + path + "\r\nView / Представление: " + architecture +
                "\r\nInspect current values before import; importing may overwrite newer changes.\r\nПеред импортом сравните значения: импорт может перезаписать новые изменения.\r\nRun in the original Windows account / Запускайте в исходной учётной записи:\r\nreg.exe import \"" + file + "\" /reg:" + architecture + "\r\n", Encoding.UTF8);
            Core.Logging.AppLogger.Info("registry", "backup.created", "Verified registry backup created.");
            return file;
        }
    }
}
