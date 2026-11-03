using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using WashinPC.Engines.Operations;
using WashinPC.Localization;

namespace WashinPC.Engines.Registry
{
    [DataContract]
    internal sealed class BackupMetadata
    {
        [DataMember] public int Version = 1;
        [DataMember] public string User;
        [DataMember] public string CreatedUtc;
        [DataMember] public int View;
        [DataMember] public string[] Targets;
        [DataMember] public string Sha256;
    }

    internal static class ArtifactJson
    {
        internal static T Read<T>(string path)
        {
            using (var stream = File.OpenRead(path)) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }
        internal static void Write<T>(string path, T value)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value); stream.Flush(true); }
        }
        internal static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }

    internal sealed class BackupEntry
    {
        public string FilePath { get; set; }
        public DateTime Date { get; set; }
        public string Targets { get; set; }
        public string ViewLabel => IsService ? "—" : View.HasValue ? (View == RegistryView.Registry64 ? "x64" : "x86") : "?";
        public RegistryView? View { get; set; }
        public string Error { get; set; }
        public bool IsService => FilePath.EndsWith(".service.json", StringComparison.OrdinalIgnoreCase);
        public string Name => Path.GetFileName(FilePath);
    }

    internal sealed class RegistryRestoreObject
    {
        internal RegistryHive Hive;
        internal string Path, Name;
        internal bool IsKey, Delete;
        internal RegistryValueKind Kind;
        internal object Value;
        internal string Identity => RegistryBackup.HiveName(Hive) + "\\" + Path + (IsKey ? "" : " :: " + Name);
        internal string Target => IsKey ? (Delete ? "backup.absent" : "backup.key") : Delete ? "backup.absent" : RegistryBackup.Encode(Kind, Value);
    }

    internal sealed class RestoreDifference
    {
        public string Object { get; set; }
        public string Before { get; set; }
        public string After { get; set; }
        internal string Snapshot;
        internal RegistryRestoreObject Mutation;
    }

    internal sealed class RestorePreview
    {
        internal BackupEntry Entry;
        internal RegistryView View;
        internal string Hash;
        internal ServiceConfiguration Service;
        internal ServiceConfiguration CurrentService;
        public List<RestoreDifference> Differences { get; } = new List<RestoreDifference>();
    }

    internal static class BackupCatalog
    {
        internal static void WriteMetadata(string file, RegistryView view, string[] targets)
        {
            ArtifactJson.Write(file + ".json", new BackupMetadata
            {
                User = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value,
                CreatedUtc = DateTime.UtcNow.ToString("o"), View = (int)view, Targets = targets, Sha256 = ArtifactJson.Hash(file)
            });
        }

        internal static List<BackupEntry> List()
        {
            var result = new List<BackupEntry>();
            foreach (var file in Directory.GetFiles(RegistryBackup.GetBackupRoot()).Where(x => x.EndsWith(".reg", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".service.json", StringComparison.OrdinalIgnoreCase)))
            {
                var entry = new BackupEntry { FilePath = file, Date = File.GetLastWriteTime(file) };
                try
                {
                    if (entry.IsService)
                    {
                        var config = ServiceRepairs.ReadBackup(file);
                        entry.Targets = config.Name; entry.View = RegistryView.Default;
                    }
                    else
                    {
                        var objects = Parse(File.ReadAllText(file));
                        entry.Targets = string.Join("; ", objects.Select(x => RegistryBackup.HiveName(x.Hive) + "\\" + x.Path).Distinct());
                        if (File.Exists(file + ".json"))
                        {
                            var metadata = ArtifactJson.Read<BackupMetadata>(file + ".json");
                            ValidateMetadata(file, metadata);
                            entry.View = (RegistryView)metadata.View;
                        }
                        else if (File.Exists(file + ".txt"))
                        {
                            var matches = Regex.Matches(File.ReadAllText(file + ".txt"), @"/reg:(32|64)\b");
                            if (matches.Count == 1) entry.View = matches[0].Groups[1].Value == "64" ? RegistryView.Registry64 : RegistryView.Registry32;
                        }
                    }
                }
                catch { entry.Error = Localizer.T("backup.invalid"); }
                result.Add(entry);
            }
            return result.OrderByDescending(x => x.Date).ToList();
        }

        private static void ValidateMetadata(string file, BackupMetadata metadata)
        {
            if (metadata.Version != 1 || metadata.Sha256 != ArtifactJson.Hash(file) ||
                (metadata.View != (int)RegistryView.Registry32 && metadata.View != (int)RegistryView.Registry64)) throw new IOException("Invalid backup metadata.");
            if (metadata.User != System.Security.Principal.WindowsIdentity.GetCurrent().User.Value) throw new IOException("Backup belongs to another account.");
        }

        internal static RestorePreview Preview(BackupEntry entry, RegistryView? overrideView = null)
        {
            if (entry.Error != null) throw new IOException(entry.Error);
            if (entry.IsService) return ServiceRepairs.Preview(entry);
            var view = entry.View ?? overrideView;
            if (!view.HasValue || (view != RegistryView.Registry32 && view != RegistryView.Registry64)) throw new IOException("Select registry view.");
            if (File.Exists(entry.FilePath + ".json")) ValidateMetadata(entry.FilePath, ArtifactJson.Read<BackupMetadata>(entry.FilePath + ".json"));
            var preview = new RestorePreview { Entry = entry, View = view.Value, Hash = ArtifactJson.Hash(entry.FilePath) };
            foreach (var obj in Parse(File.ReadAllText(entry.FilePath)))
            {
                var current = Current(obj, view.Value);
                var target = obj.Target;
                if (current == target || (obj.IsKey && !obj.Delete && current != "backup.absent")) continue;
                preview.Differences.Add(new RestoreDifference
                {
                    Object = obj.Identity, Before = DisplayValue(obj, current), After = DisplayValue(obj, target), Snapshot = current, Mutation = obj
                });
            }
            return preview;
        }

        private static string Display(string value) => value.StartsWith("backup.", StringComparison.Ordinal) ? Localizer.T(value) : value;
        private static string DisplayValue(RegistryRestoreObject obj, string value)
        {
            if (value.StartsWith("backup.", StringComparison.Ordinal)) return Display(value);
            if (obj.IsKey) return value;
            try
            {
                var parsed = Parse("Windows Registry Editor Version 5.00\n\n[HKEY_CURRENT_USER\\preview]\n@=" + value + "\n").Last();
                if (parsed.Value is string text) return text;
                if (parsed.Value is string[] strings) return string.Join("; ", strings);
                if (parsed.Value is byte[] bytes) return BitConverter.ToString(bytes);
                return Convert.ToString(parsed.Value, System.Globalization.CultureInfo.CurrentCulture);
            }
            catch { return value; }
        }

        internal static string Current(RegistryRestoreObject obj, RegistryView view)
        {
            using (var root = RegistryKey.OpenBaseKey(obj.Hive, view))
            using (var key = root.OpenSubKey(obj.Path))
            {
                if (key == null) return "backup.absent";
                if (obj.IsKey) return obj.Delete ? RegistryBackup.Capture(obj.Hive, view, obj.Path) : "backup.key";
                if (!key.GetValueNames().Contains(obj.Name, StringComparer.OrdinalIgnoreCase)) return "backup.absent";
                return RegistryBackup.Encode(key.GetValueKind(obj.Name), key.GetValue(obj.Name, null, RegistryValueOptions.DoNotExpandEnvironmentNames));
            }
        }

        internal static OperationResult Restore(RestorePreview preview, CancellationToken token, Action<RegistryRestoreObject, RegistryView> writer = null)
        {
            if (preview.Service != null) return ServiceRepairs.Restore(preview, token);
            var result = new OperationResult();
            if (token.IsCancellationRequested) { result.Cancelled = true; return result; }
            if (ArtifactJson.Hash(preview.Entry.FilePath) != preview.Hash) throw new InvalidOperationException("op.changed");
            foreach (var diff in preview.Differences)
                if (Current(diff.Mutation, preview.View) != diff.Snapshot) throw new InvalidOperationException("op.changed");
            // All undo data is saved before the first write. Missing keys have deletion directives.
            var undo = new StringBuilder("Windows Registry Editor Version 5.00\r\n\r\n");
            var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var diff in preview.Differences)
            {
                var obj = diff.Mutation;
                var full = RegistryBackup.HiveName(obj.Hive) + "\\" + obj.Path;
                if (missing.Any(x => full.Equals(x, StringComparison.OrdinalIgnoreCase) || full.StartsWith(x + "\\", StringComparison.OrdinalIgnoreCase))) continue;
                if (obj.IsKey)
                {
                    if (diff.Snapshot == "backup.absent") { if (missing.Add(full)) undo.Append("[-").Append(full).Append("]\r\n\r\n"); }
                    else if (obj.Delete) undo.Append(diff.Snapshot.Substring("Windows Registry Editor Version 5.00\r\n\r\n".Length));
                }
                else if (!missing.Any(x => full.Equals(x, StringComparison.OrdinalIgnoreCase) || full.StartsWith(x + "\\", StringComparison.OrdinalIgnoreCase)))
                    undo.Append('[').Append(full).Append("]\r\n").Append(RegistryBackup.ValueName(obj.Name)).Append('=').Append(diff.Snapshot == "backup.absent" ? "-" : diff.Snapshot).Append("\r\n\r\n");
            }
            if (preview.Differences.Count == 0) return result;
            result.Backups.Add(RegistryBackup.Save(preview.Differences[0].Mutation.Hive, preview.View, preview.Differences[0].Mutation.Path, undo.ToString()));
            foreach (var diff in preview.Differences)
                if (Current(diff.Mutation, preview.View) != diff.Snapshot) throw new InvalidOperationException("op.changed");
            foreach (var diff in preview.Differences)
            {
                var item = new ItemResult { Path = diff.Object }; result.Items.Add(item);
                if (token.IsCancellationRequested) { result.Cancelled = true; item.Status = ItemStatus.Cancelled; item.Reason = "op.cancelled"; continue; }
                try
                {
                    if (Current(diff.Mutation, preview.View) != diff.Snapshot) throw new InvalidOperationException("op.changed");
                    (writer ?? Apply)(diff.Mutation, preview.View);
                    if (Current(diff.Mutation, preview.View) != diff.Mutation.Target) throw new IOException("Restore verification failed.");
                    item.Status = ItemStatus.Applied; item.Reason = "op.applied";
                }
                catch (InvalidOperationException) { item.Status = ItemStatus.Skipped; item.Reason = "op.changed"; }
                catch { item.Status = ItemStatus.Failed; item.Reason = "op.failed"; }
            }
            return result;
        }

        private static void Apply(RegistryRestoreObject obj, RegistryView view)
        {
            using (var root = RegistryKey.OpenBaseKey(obj.Hive, view))
            {
                if (obj.IsKey && obj.Delete) { root.DeleteSubKeyTree(obj.Path, true); return; }
                using (var key = root.CreateSubKey(obj.Path))
                {
                    if (obj.IsKey) return;
                    if (obj.Delete) key.DeleteValue(obj.Name, true);
                    else key.SetValue(obj.Name, obj.Value, obj.Kind);
                }
            }
        }

        internal static List<RegistryRestoreObject> Parse(string text)
        {
            if (text.Length > 16 * 1024 * 1024) throw new IOException("Backup too large.");
            text = text.TrimStart('\ufeff').Replace("\r\n", "\n");
            if (!text.StartsWith("Windows Registry Editor Version 5.00\n", StringComparison.Ordinal)) throw new IOException("Unsupported backup.");
            text = Regex.Replace(text, @",\\\n[ \t]*", ",");
            var result = new List<RegistryRestoreObject>();
            RegistryRestoreObject section = null;
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in text.Split('\n').Skip(1))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    var full = line.Substring(1, line.Length - 2); var delete = full.StartsWith("-", StringComparison.Ordinal); if (delete) full = full.Substring(1);
                    var split = full.IndexOf('\\'); if (split < 1) throw new IOException();
                    RegistryHive hive;
                    switch (full.Substring(0, split))
                    {
                        case "HKEY_CURRENT_USER": hive = RegistryHive.CurrentUser; break;
                        case "HKEY_LOCAL_MACHINE": hive = RegistryHive.LocalMachine; break;
                        case "HKEY_CLASSES_ROOT": hive = RegistryHive.ClassesRoot; break;
                        case "HKEY_USERS": hive = RegistryHive.Users; break;
                        default: throw new IOException();
                    }
                    var path = full.Substring(split + 1);
                    if (string.IsNullOrWhiteSpace(path) || path.Split('\\').Any(x => x.Length == 0) || path.IndexOfAny(new[] { '[', ']', '\0', '\r', '\n' }) >= 0) throw new IOException();
                    section = new RegistryRestoreObject { Hive = hive, Path = path, IsKey = true, Delete = delete };
                    if (identities.Add(section.Identity)) result.Add(section);
                    else if (result.Any(x => x.IsKey && x.Identity == section.Identity && x.Delete != delete)) throw new IOException();
                    continue;
                }
                if (section == null || section.Delete) throw new IOException();
                var match = Regex.Match(line, "^(@|\"(?:[^\"\\\\]|\\\\[\\\\\"])*\")=(.+)$");
                if (!match.Success) throw new IOException("Unsupported registry value.");
                var name = match.Groups[1].Value;
                name = name == "@" ? "" : Regex.Replace(name.Substring(1, name.Length - 2), @"\\([\\" + "\"" + @"])", "$1");
                var obj = new RegistryRestoreObject { Hive = section.Hive, Path = section.Path, Name = name };
                if (!identities.Add(obj.Identity)) throw new IOException("Duplicate value.");
                var encoded = match.Groups[2].Value;
                if (encoded == "-") obj.Delete = true;
                else if (Regex.IsMatch(encoded, "^dword:[0-9a-fA-F]{8}$")) { obj.Kind = RegistryValueKind.DWord; obj.Value = unchecked((int)Convert.ToUInt32(encoded.Substring(6), 16)); }
                else
                {
                    var hex = Regex.Match(encoded, @"^hex\(([01237b])\):((?:[0-9a-fA-F]{2}(?:,[0-9a-fA-F]{2})*)?)$");
                    if (!hex.Success) throw new IOException("Unsupported registry type.");
                    obj.Kind = (RegistryValueKind)Convert.ToInt32(hex.Groups[1].Value, 16);
                    var bytes = hex.Groups[2].Value.Length == 0 ? new byte[0] : hex.Groups[2].Value.Split(',').Select(x => Convert.ToByte(x, 16)).ToArray();
                    if (obj.Kind == RegistryValueKind.QWord) { if (bytes.Length != 8) throw new IOException(); obj.Value = BitConverter.ToInt64(bytes, 0); }
                    else if (obj.Kind == RegistryValueKind.String || obj.Kind == RegistryValueKind.ExpandString || obj.Kind == RegistryValueKind.MultiString)
                    {
                        if (bytes.Length < 2 || bytes.Length % 2 != 0) throw new IOException();
                        var value = new UnicodeEncoding(false, false, true).GetString(bytes);
                        if (!value.EndsWith("\0", StringComparison.Ordinal)) throw new IOException();
                        if (obj.Kind == RegistryValueKind.MultiString)
                        { if (!value.EndsWith("\0\0", StringComparison.Ordinal)) throw new IOException(); var body = value.Substring(0, value.Length - 2); obj.Value = body.Length == 0 ? new string[0] : body.Split('\0'); }
                        else obj.Value = value.Substring(0, value.Length - 1);
                    }
                    else { if ((int)obj.Kind == 0) obj.Kind = RegistryValueKind.None; obj.Value = bytes; }
                }
                result.Add(obj);
            }
            if (result.Count == 0) throw new IOException("Empty backup.");
            return result;
        }
    }
}
