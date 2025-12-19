using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace WashinPC.Engines.ContextMenu
{
    public static class ContextMenuScanner
    {
        // Blocked list (CCleaner-like) — Shell Extensions (COM handlers)
        private const string BlockedKey = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

        public static List<ContextMenuEntry> Scan()
        {
            var list = new List<ContextMenuEntry>();

            // Build blocked sets for HKCU/HKLM (names are usually CLSIDs)
            var blockedHkcu = ReadBlockedSet(Microsoft.Win32.Registry.CurrentUser);
            var blockedHklm = ReadBlockedSet(Microsoft.Win32.Registry.LocalMachine);

            // Scan targets in HKCU/HKLM "Software\Classes"
            ScanScope(list, ContextMenuScope.CurrentUser, Microsoft.Win32.Registry.CurrentUser, "HKCU", blockedHkcu, blockedHklm);
            ScanScope(list, ContextMenuScope.LocalMachine, Microsoft.Win32.Registry.LocalMachine, "HKLM", blockedHkcu, blockedHklm);

            return list;
        }

        private static HashSet<string> ReadBlockedSet(RegistryKey root)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var key = root.OpenSubKey(BlockedKey, false))
                {
                    if (key == null) return set;

                    foreach (var name in key.GetValueNames())
                        set.Add(name);
                }
            }
            catch
            {
                // ignore — no rights / key missing
            }

            return set;
        }

        private static void ScanScope(
            List<ContextMenuEntry> list,
            ContextMenuScope scope,
            RegistryKey root,
            string source,
            HashSet<string> blockedHkcu,
            HashSet<string> blockedHklm)
        {
            // "Software\Classes" is where HKCR merges from
            const string classes = @"Software\Classes";

            // Targets
            ScanTargetShellEx(list, scope, root, source, classes + @"\*\shellex\ContextMenuHandlers", ContextMenuTarget.AnyFile, blockedHkcu, blockedHklm);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\*\shell", ContextMenuTarget.AnyFile);

            ScanTargetShellEx(list, scope, root, source, classes + @"\Directory\shellex\ContextMenuHandlers", ContextMenuTarget.Folder, blockedHkcu, blockedHklm);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\Directory\shell", ContextMenuTarget.Folder);

            ScanTargetShellEx(list, scope, root, source, classes + @"\Directory\Background\shellex\ContextMenuHandlers", ContextMenuTarget.DirectoryBackground, blockedHkcu, blockedHklm);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\Directory\Background\shell", ContextMenuTarget.DirectoryBackground);

            ScanTargetShellEx(list, scope, root, source, classes + @"\Drive\shellex\ContextMenuHandlers", ContextMenuTarget.Drive, blockedHkcu, blockedHklm);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\Drive\shell", ContextMenuTarget.Drive);
        }

        private static void ScanTargetShellEx(
            List<ContextMenuEntry> list,
            ContextMenuScope scope,
            RegistryKey root,
            string source,
            string handlersKeyPath,
            ContextMenuTarget target,
            HashSet<string> blockedHkcu,
            HashSet<string> blockedHklm)
        {
            try
            {
                using (var handlersKey = root.OpenSubKey(handlersKeyPath, false))
                {
                    if (handlersKey == null) return;

                    foreach (var sub in handlersKey.GetSubKeyNames())
                    {
                        using (var h = handlersKey.OpenSubKey(sub, false))
                        {
                            if (h == null) continue;

                            // Usually default value is CLSID, or the subkey name is a CLSID/name.
                            var clsid = (h.GetValue(null) as string);
                            if (string.IsNullOrWhiteSpace(clsid))
                                clsid = sub;

                            bool isBlocked = blockedHkcu.Contains(clsid) || blockedHklm.Contains(clsid);

                            list.Add(new ContextMenuEntry
                            {
                                Name = sub,
                                Target = target,
                                Kind = ContextMenuKind.ShellExHandler,
                                Scope = scope,
                                RegistryPath = clsid, // for Blocked list we use CLSID/value name
                                Source = source,
                                IsEnabled = !isBlocked
                            });
                        }
                    }
                }
            }
            catch
            {
                // ignore per-key failures
            }
        }

        private static void ScanTargetShellVerbs(
            List<ContextMenuEntry> list,
            ContextMenuScope scope,
            RegistryKey root,
            string source,
            string verbsKeyPath,
            ContextMenuTarget target)
        {
            // NOTE: Verbs disabling is non-trivial/unsafe (LegacyDisable/ProgrammaticAccessOnly etc.)
            // We list them for visibility, but we do NOT toggle them in Applier (only ShellEx Blocked list).
            try
            {
                using (var verbsKey = root.OpenSubKey(verbsKeyPath, false))
                {
                    if (verbsKey == null) return;

                    foreach (var verb in verbsKey.GetSubKeyNames())
                    {
                        using (var v = verbsKey.OpenSubKey(verb, false))
                        {
                            if (v == null) continue;

                            var display = (v.GetValue(null) as string);
                            if (string.IsNullOrWhiteSpace(display))
                                display = verb;

                            list.Add(new ContextMenuEntry
                            {
                                Name = display,
                                Target = target,
                                Kind = ContextMenuKind.ShellVerb,
                                Scope = scope,
                                RegistryPath = verbsKeyPath + @"\" + verb, // informational id
                                Source = source,
                                IsEnabled = true
                            });
                        }
                    }
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
