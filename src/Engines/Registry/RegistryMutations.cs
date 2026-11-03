using System;
using System.Linq;
using Microsoft.Win32;

namespace WashinPC.Engines.Registry
{
    internal static class RegistryMutations
    {
        internal static RegistryFixResult DeleteKey(RegistryHive hive, RegistryView view, string path)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            {
                using (var key = root.OpenSubKey(path)) if (key == null) return RegistryFixResult.Skipped;
                root.DeleteSubKeyTree(path, true);
                using (var key = root.OpenSubKey(path)) return key == null ? RegistryFixResult.Fixed : RegistryFixResult.Failed;
            }
        }

        internal static RegistryFixResult DeleteValue(RegistryHive hive, RegistryView view, string path, string name)
            => DeleteValues(hive, view, path, new[] { name });

        internal static RegistryFixResult DeleteValues(RegistryHive hive, RegistryView view, string path, string[] names)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(path, true))
            {
                if (key == null) return RegistryFixResult.Skipped;
                var targets = names.Where(x => x != null).ToArray();
                if (targets.Length == 0 || targets.Any(x => !key.GetValueNames().Contains(x, StringComparer.OrdinalIgnoreCase))) return RegistryFixResult.Skipped;
                foreach (var name in targets) key.DeleteValue(name, true);
                return targets.All(x => !key.GetValueNames().Contains(x, StringComparer.OrdinalIgnoreCase)) ? RegistryFixResult.Fixed : RegistryFixResult.Failed;
            }
        }

        internal static RegistryFixResult SetValue(RegistryHive hive, RegistryView view, string path, string name, object value, RegistryValueKind kind)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(path, true))
            {
                if (key == null || !key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase)) return RegistryFixResult.Skipped;
                if (key.GetValueKind(name) == kind && RegistryBackup.Encode(kind, key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames)) == RegistryBackup.Encode(kind, value)) return RegistryFixResult.Skipped;
                key.SetValue(name, value, kind);
                return key.GetValueKind(name) == kind && RegistryBackup.Encode(kind, key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames)) == RegistryBackup.Encode(kind, value)
                    ? RegistryFixResult.Fixed : RegistryFixResult.Failed;
            }
        }

        internal static RegistryFixResult RepairCom(RegistryView view, string path)
        {
            using (var root = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, view))
            using (var key = root.OpenSubKey(path))
            {
                if (key == null) return RegistryFixResult.Skipped;
                var children = key.GetSubKeyNames();
                if (children.Length == 1 && children[0].Equals("InprocServer32", StringComparison.OrdinalIgnoreCase))
                    return DeleteKey(RegistryHive.ClassesRoot, view, path);
            }
            return SetValue(RegistryHive.ClassesRoot, view, path + @"\InprocServer32", "", "", RegistryValueKind.String);
        }
    }
}
