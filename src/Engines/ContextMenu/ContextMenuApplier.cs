using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using WashinPC.Engines.Registry;
using WashinPC.Engines.Operations;

namespace WashinPC.Engines.ContextMenu
{
    internal enum MenuAction { Disable, Enable, Delete }
    public static class ContextMenuApplier
    {
        private const string BlockedKey = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
        private static RegistryHive Hive(ContextMenuEntry entry) => entry.Scope == ContextMenuScope.CurrentUser ? RegistryHive.CurrentUser : RegistryHive.LocalMachine;
        private static void Validate(ContextMenuEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.RelativeKeyPath)) throw new IOException();
            var parents = new[] { "*", "lnkfile", "Directory", @"Directory\Background", @"DesktopBackground", "Drive" };
            var kind = entry.Kind == ContextMenuKind.ShellVerb ? @"\shell\" : @"\shellex\ContextMenuHandlers\";
            if (!parents.Any(p => {
                var prefix = @"Software\Classes\" + p + kind;
                return entry.RelativeKeyPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && entry.RelativeKeyPath.Length > prefix.Length && entry.RelativeKeyPath.Substring(prefix.Length).IndexOf('\\') < 0;
            })) throw new IOException("Unsupported context menu path.");
            if (entry.Kind == ContextMenuKind.ShellExHandler && !Guid.TryParse(entry.BlockedId, out var unused)) throw new IOException("Ambiguous handler identity.");
        }
        private static string BlockValue(RegistryHive hive, RegistryView view, string name)
        {
            using (var root = RegistryKey.OpenBaseKey(hive, view))
            using (var key = root.OpenSubKey(BlockedKey))
            {
                if (key == null || !key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase)) return "absent";
                return RegistryBackup.Encode(key.GetValueKind(name), key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames));
            }
        }
        internal static string Snapshot(ContextMenuEntry entry)
        {
            Validate(entry);
            var value = RegistryBackup.Capture(Hive(entry), entry.View, entry.RelativeKeyPath);
            if (entry.Kind == ContextMenuKind.ShellExHandler)
                value += BlockValue(RegistryHive.CurrentUser, entry.View, entry.BlockedId) + "\n" + BlockValue(RegistryHive.LocalMachine, entry.View, entry.BlockedId);
            return value;
        }
        internal static OperationResult Apply(IEnumerable<ContextMenuEntry> entries, MenuAction action, CancellationToken token)
        {
            var result = new OperationResult();
            foreach (var entry in entries)
            {
                var item = new ItemResult { Path = entry.RelativeKeyPath }; result.Items.Add(item);
                if (token.IsCancellationRequested) { result.Cancelled = true; item.Status = ItemStatus.Cancelled; item.Reason = "op.cancelled"; continue; }
                try
                {
                    Validate(entry);
                    if (entry.Snapshot == null || entry.Snapshot != Snapshot(entry)) throw new InvalidOperationException("op.changed");
                    if (entry.Kind == ContextMenuKind.ShellExHandler && action == MenuAction.Enable && BlockValue(RegistryHive.LocalMachine, entry.View, entry.BlockedId) != "absent")
                        throw new InvalidOperationException("op.machine_blocked");
                    string backup;
                    if (entry.Kind == ContextMenuKind.ShellExHandler && action != MenuAction.Delete)
                        backup = RegistryBackup.ExportValue(RegistryHive.CurrentUser, entry.View, BlockedKey, entry.BlockedId);
                    else backup = RegistryBackup.ExportKey(Hive(entry), entry.View, entry.RelativeKeyPath);
                    if (string.IsNullOrEmpty(backup)) throw new InvalidOperationException("op.backup_failed");
                    result.Backups.Add(backup);
                    // Export missing toggle values as deletion directives, so manual import can undo newly added values.
                    if (entry.Kind == ContextMenuKind.ShellVerb && action != MenuAction.Delete)
                    {
                        result.Backups.Add(RegistryBackup.ExportValue(Hive(entry), entry.View, entry.RelativeKeyPath, "LegacyDisable"));
                        result.Backups.Add(RegistryBackup.ExportValue(Hive(entry), entry.View, entry.RelativeKeyPath, "ProgrammaticAccessOnly"));
                    }
                    if (entry.Snapshot != Snapshot(entry)) throw new InvalidOperationException("op.changed");
                    if (entry.Kind == ContextMenuKind.ShellExHandler && action != MenuAction.Delete)
                    {
                        using (var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, entry.View))
                        using (var key = root.CreateSubKey(BlockedKey))
                        { if (action == MenuAction.Disable) key.SetValue(entry.BlockedId, "", RegistryValueKind.String); else key.DeleteValue(entry.BlockedId, false); }
                    }
                    else
                    {
                        using (var root = RegistryKey.OpenBaseKey(Hive(entry), entry.View))
                        {
                            if (action == MenuAction.Delete) root.DeleteSubKeyTree(entry.RelativeKeyPath, true);
                            else using (var key = root.OpenSubKey(entry.RelativeKeyPath, true))
                            {
                                if (key == null) throw new IOException();
                                if (action == MenuAction.Disable) key.SetValue("LegacyDisable", "", RegistryValueKind.String);
                                else { key.DeleteValue("LegacyDisable", false); key.DeleteValue("ProgrammaticAccessOnly", false); }
                            }
                        }
                    }
                    item.Status = ItemStatus.Applied; item.Reason = "op.applied";
                }
                catch (InvalidOperationException ex) { item.Status = ItemStatus.Skipped; item.Reason = ex.Message; }
                catch { item.Status = ItemStatus.Failed; item.Reason = "op.failed"; }
            }
            return result;
        }
        public static int ApplyDisable(IEnumerable<ContextMenuEntry> entries) => Apply(entries, MenuAction.Disable, CancellationToken.None).Applied;
        public static int ApplyEnable(IEnumerable<ContextMenuEntry> entries) => Apply(entries, MenuAction.Enable, CancellationToken.None).Applied;
        public static int ApplyDelete(IEnumerable<ContextMenuEntry> entries) => Apply(entries, MenuAction.Delete, CancellationToken.None).Applied;
    }
}
