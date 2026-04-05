using System.Collections.Generic;
using Microsoft.Win32;
using WinRegistry = Microsoft.Win32.Registry;

namespace WashinPC.Engines.ContextMenu
{
    public static class ContextMenuApplier
    {
        private const string BlockedKey = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

        public static int ApplyDisable(IEnumerable<ContextMenuEntry> entries)
        {
            int changed = 0;

            foreach (var e in entries)
            {
                if (e == null)
                    continue;

                try
                {
                    if (e.Kind == ContextMenuKind.ShellExHandler)
                    {
                        var blockedId = !string.IsNullOrWhiteSpace(e.BlockedId)
                            ? e.BlockedId
                            : e.RegistryPath;

                        if (string.IsNullOrWhiteSpace(blockedId))
                            continue;

                        using (var key = WinRegistry.CurrentUser.CreateSubKey(BlockedKey))
                        {
                            if (key == null)
                                continue;

                            key.SetValue(blockedId, "");
                            changed++;
                        }
                    }
                    else if (e.Kind == ContextMenuKind.ShellVerb)
                    {
                        if (string.IsNullOrWhiteSpace(e.RelativeKeyPath))
                            continue;

                        var root = GetRoot(e.Scope);
                        using (var key = root.CreateSubKey(e.RelativeKeyPath))
                        {
                            if (key == null)
                                continue;

                            key.SetValue("LegacyDisable", "", RegistryValueKind.String);
                            changed++;
                        }
                    }
                }
                catch
                {
                }
            }

            return changed;
        }

        public static int ApplyEnable(IEnumerable<ContextMenuEntry> entries)
        {
            int changed = 0;

            foreach (var e in entries)
            {
                if (e == null)
                    continue;

                try
                {
                    if (e.Kind == ContextMenuKind.ShellExHandler)
                    {
                        var blockedId = !string.IsNullOrWhiteSpace(e.BlockedId)
                            ? e.BlockedId
                            : e.RegistryPath;

                        if (string.IsNullOrWhiteSpace(blockedId))
                            continue;

                        using (var key = WinRegistry.CurrentUser.OpenSubKey(BlockedKey, true))
                        {
                            if (key == null)
                                continue;

                            key.DeleteValue(blockedId, false);
                            changed++;
                        }
                    }
                    else if (e.Kind == ContextMenuKind.ShellVerb)
                    {
                        if (string.IsNullOrWhiteSpace(e.RelativeKeyPath))
                            continue;

                        var root = GetRoot(e.Scope);
                        using (var key = root.OpenSubKey(e.RelativeKeyPath, true))
                        {
                            if (key == null)
                                continue;

                            key.DeleteValue("LegacyDisable", false);
                            key.DeleteValue("ProgrammaticAccessOnly", false);
                            changed++;
                        }
                    }
                }
                catch
                {
                }
            }

            return changed;
        }

        public static int ApplyDelete(IEnumerable<ContextMenuEntry> entries)
        {
            int changed = 0;

            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.RelativeKeyPath))
                    continue;

                try
                {
                    var root = GetRoot(e.Scope);
                    root.DeleteSubKeyTree(e.RelativeKeyPath, false);

                    // Cleanup blocked list residue when removing ShellEx handler.
                    if (e.Kind == ContextMenuKind.ShellExHandler && !string.IsNullOrWhiteSpace(e.BlockedId))
                    {
                        using (var key = WinRegistry.CurrentUser.OpenSubKey(BlockedKey, true))
                            key?.DeleteValue(e.BlockedId, false);
                    }

                    changed++;
                }
                catch
                {
                }
            }

            return changed;
        }

        private static RegistryKey GetRoot(ContextMenuScope scope)
        {
            return scope == ContextMenuScope.CurrentUser
                ? WinRegistry.CurrentUser
                : WinRegistry.LocalMachine;
        }
    }
}

