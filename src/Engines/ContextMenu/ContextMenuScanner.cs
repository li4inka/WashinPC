using System;
using System.Collections.Generic;
using Microsoft.Win32;
using WinRegistry = Microsoft.Win32.Registry;

namespace WashinPC.Engines.ContextMenu
{
    public static class ContextMenuScanner
    {
        private const string BlockedKey = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

        public static List<ContextMenuEntry> Scan(System.Threading.CancellationToken token = default(System.Threading.CancellationToken))
        {
            var list = new List<ContextMenuEntry>();

            var blockedHkcu = ReadBlockedSet(WinRegistry.CurrentUser, token);
            var blockedHklm = ReadBlockedSet(WinRegistry.LocalMachine, token);

            ScanScope(list, ContextMenuScope.CurrentUser, WinRegistry.CurrentUser, "HKCU", blockedHkcu, blockedHklm, token);
            ScanScope(list, ContextMenuScope.LocalMachine, WinRegistry.LocalMachine, "HKLM", blockedHkcu, blockedHklm, token);

            var verified = new List<ContextMenuEntry>();
            foreach (var entry in list)
            {
                if (token.IsCancellationRequested) break;
                try { entry.Snapshot = ContextMenuApplier.Snapshot(entry); verified.Add(entry); }
                catch { Core.Logging.AppLogger.Warn("contextmenu", "scan.unavailable", "Entry unavailable; skipped."); }
            }
            return verified;
        }

        private static HashSet<string> ReadBlockedSet(RegistryKey root, System.Threading.CancellationToken token)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var key = root.OpenSubKey(BlockedKey, false))
                {
                    if (key == null)
                        return set;

                    foreach (var name in key.GetValueNames()) { if (token.IsCancellationRequested) break; set.Add(name); }
                }
            }
            catch
            {
            }

            return set;
        }

        private static void ScanScope(
            List<ContextMenuEntry> list,
            ContextMenuScope scope,
            RegistryKey root,
            string source,
            HashSet<string> blockedHkcu,
            HashSet<string> blockedHklm, System.Threading.CancellationToken token)
        {
            const string classes = @"Software\Classes";

            ScanTargetShellEx(list, scope, root, source, classes + @"\*\shellex\ContextMenuHandlers", ContextMenuTarget.AnyFile, blockedHkcu, blockedHklm, token);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\*\shell", ContextMenuTarget.AnyFile, token);

            ScanTargetShellEx(list, scope, root, source, classes + @"\lnkfile\shellex\ContextMenuHandlers", ContextMenuTarget.Shortcut, blockedHkcu, blockedHklm, token);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\lnkfile\shell", ContextMenuTarget.Shortcut, token);

            ScanTargetShellEx(list, scope, root, source, classes + @"\Directory\shellex\ContextMenuHandlers", ContextMenuTarget.Folder, blockedHkcu, blockedHklm, token);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\Directory\shell", ContextMenuTarget.Folder, token);

            ScanTargetShellEx(list, scope, root, source, classes + @"\Directory\Background\shellex\ContextMenuHandlers", ContextMenuTarget.DirectoryBackground, blockedHkcu, blockedHklm, token);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\Directory\Background\shell", ContextMenuTarget.DirectoryBackground, token);

            ScanTargetShellEx(list, scope, root, source, classes + @"\DesktopBackground\shellex\ContextMenuHandlers", ContextMenuTarget.DesktopBackground, blockedHkcu, blockedHklm, token);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\DesktopBackground\shell", ContextMenuTarget.DesktopBackground, token);

            ScanTargetShellEx(list, scope, root, source, classes + @"\Drive\shellex\ContextMenuHandlers", ContextMenuTarget.Drive, blockedHkcu, blockedHklm, token);
            ScanTargetShellVerbs(list, scope, root, source, classes + @"\Drive\shell", ContextMenuTarget.Drive, token);
        }

        private static void ScanTargetShellEx(
            List<ContextMenuEntry> list,
            ContextMenuScope scope,
            RegistryKey root,
            string source,
            string handlersKeyPath,
            ContextMenuTarget target,
            HashSet<string> blockedHkcu,
            HashSet<string> blockedHklm, System.Threading.CancellationToken token)
        {
            try
            {
                using (var handlersKey = root.OpenSubKey(handlersKeyPath, false))
                {
                    if (handlersKey == null)
                        return;

                    foreach (var sub in handlersKey.GetSubKeyNames())
                    {
                        if (token.IsCancellationRequested) break;
                        using (var handler = handlersKey.OpenSubKey(sub, false))
                        {
                            if (handler == null)
                                continue;

                            var clsid = handler.GetValue(null) as string;
                            if (string.IsNullOrWhiteSpace(clsid))
                                clsid = sub;

                            var isBlocked = blockedHkcu.Contains(clsid) || blockedHklm.Contains(clsid);
                            var relativePath = handlersKeyPath + @"\" + sub;

                            list.Add(new ContextMenuEntry
                            {
                                Name = sub,
                                Target = target,
                                Kind = ContextMenuKind.ShellExHandler,
                                Scope = scope,
                                RegistryPath = relativePath,
                                RelativeKeyPath = relativePath,
                                BlockedId = clsid,
                                Source = source,
                                IsEnabled = !isBlocked
                            });
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private static void ScanTargetShellVerbs(
            List<ContextMenuEntry> list,
            ContextMenuScope scope,
            RegistryKey root,
            string source,
            string verbsKeyPath,
            ContextMenuTarget target, System.Threading.CancellationToken token)
        {
            try
            {
                using (var verbsKey = root.OpenSubKey(verbsKeyPath, false))
                {
                    if (verbsKey == null)
                        return;

                    foreach (var verb in verbsKey.GetSubKeyNames())
                    {
                        if (token.IsCancellationRequested) break;
                        using (var v = verbsKey.OpenSubKey(verb, false))
                        {
                            if (v == null)
                                continue;

                            var display = v.GetValue(null) as string;
                            if (string.IsNullOrWhiteSpace(display))
                                display = verb;

                            var isDisabled = v.GetValue("LegacyDisable") != null ||
                                             v.GetValue("ProgrammaticAccessOnly") != null;

                            var relativePath = verbsKeyPath + @"\" + verb;

                            list.Add(new ContextMenuEntry
                            {
                                Name = display,
                                Target = target,
                                Kind = ContextMenuKind.ShellVerb,
                                Scope = scope,
                                RegistryPath = relativePath,
                                RelativeKeyPath = relativePath,
                                Source = source,
                                IsEnabled = !isDisabled
                            });
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}

