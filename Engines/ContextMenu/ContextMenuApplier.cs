using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace WashinPC.Engines.ContextMenu
{
    public static class ContextMenuApplier
    {
        private const string BlockedKey = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

        public static int ApplyDisable(IEnumerable<ContextMenuEntry> entries)
        {
            int changed = 0;

            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(BlockedKey))
            {
                if (key == null) return 0;

                foreach (var e in entries)
                {
                    if (e == null) continue;
                    if (e.Kind != ContextMenuKind.ShellExHandler) continue; // only safe path
                    if (string.IsNullOrWhiteSpace(e.RegistryPath)) continue;

                    try
                    {
                        key.SetValue(e.RegistryPath, "");
                        changed++;
                    }
                    catch { }
                }
            }

            return changed;
        }

        public static int ApplyEnable(IEnumerable<ContextMenuEntry> entries)
        {
            int changed = 0;

            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(BlockedKey, true))
            {
                if (key == null) return 0;

                foreach (var e in entries)
                {
                    if (e == null) continue;
                    if (e.Kind != ContextMenuKind.ShellExHandler) continue;
                    if (string.IsNullOrWhiteSpace(e.RegistryPath)) continue;

                    try
                    {
                        key.DeleteValue(e.RegistryPath, false);
                        changed++;
                    }
                    catch { }
                }
            }

            return changed;
        }
    }
}
