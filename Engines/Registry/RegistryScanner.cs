using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace WashinPC.Engines.Registry
{
    public enum RegistryFixResult
    {
        None,
        Fixed,
        Failed
    }

    public class RegistryIssue
    {
        public string Category { get; set; }
        public RegistryHive Hive { get; set; }
        public RegistryView View { get; set; }

        public string KeyPath { get; set; }
        public string ValueName { get; set; }
        public string Details { get; set; }

        /// <summary>
        /// Возвращает Fixed или Failed.
        /// Никаких Skipped — CCleaner так не делает.
        /// </summary>
        public Func<bool, RegistryFixResult> Fix { get; set; }

        public RegistryFixResult Result { get; set; } = RegistryFixResult.None;
    }

    /// <summary>
    /// SAFE registry cleaner (CCleaner-like):
    /// - только реально исправляемые проблемы
    /// - без COM / CLSID / HKCR
    /// - после Fix повторный Scan = 0 проблем
    /// </summary>
    public class RegistryScanner
    {
        private readonly List<RegistryIssue> _issues = new List<RegistryIssue>();

        public IList<RegistryIssue> ScanAll()
        {
            _issues.Clear();

            ScanApplicationPaths();
            ScanUninstallEntries();
            ScanSharedDlls();

            return _issues.AsReadOnly();
        }

        // ───────────────────────── helpers ─────────────────────────

        private static RegistryKey OpenBase(RegistryHive hive, RegistryView view)
        {
            return RegistryKey.OpenBaseKey(hive, view);
        }

        private void AddIssue(
            string category,
            RegistryHive hive,
            RegistryView view,
            string keyPath,
            string valueName,
            string details,
            Func<bool, RegistryFixResult> fix)
        {
            _issues.Add(new RegistryIssue
            {
                Category = category,
                Hive = hive,
                View = view,
                KeyPath = keyPath,
                ValueName = valueName,
                Details = details,
                Fix = fix
            });
        }

        // ───────────────────────── Application Paths ─────────────────────────
        // CCleaner: показывает ТОЛЬКО если реально можно удалить subkey

        private void ScanApplicationPaths()
        {
            const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;

                        foreach (var sk in key.GetSubKeyNames())
                        {
                            using (var app = key.OpenSubKey(sk))
                            {
                                var exe = app?.GetValue(null) as string;
                                if (string.IsNullOrWhiteSpace(exe)) continue;

                                if (!File.Exists(exe))
                                {
                                    AddIssue(
                                        "Application Paths",
                                        RegistryHive.LocalMachine,
                                        view,
                                        $@"HKLM\{sub}\{sk}",
                                        "(Default)",
                                        "Путь не существует: " + exe,
                                        dryRun =>
                                        {
                                            try
                                            {
                                                if (!dryRun)
                                                {
                                                    using (var wbase = OpenBase(RegistryHive.LocalMachine, view))
                                                    using (var w = wbase.OpenSubKey(sub, true))
                                                    {
                                                        if (w == null) return RegistryFixResult.Failed;
                                                        w.DeleteSubKeyTree(sk, false);
                                                    }
                                                }
                                                return RegistryFixResult.Fixed;
                                            }
                                            catch
                                            {
                                                return RegistryFixResult.Failed;
                                            }
                                        }
                                    );
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ───────────────────────── Uninstall Entries ─────────────────────────
        // CCleaner: битые деинсталляторы

        private void ScanUninstallEntries()
        {
            const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            {
                foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using (var baseKey = OpenBase(hive, view))
                        using (var key = baseKey.OpenSubKey(sub))
                        {
                            if (key == null) continue;

                            foreach (var sk in key.GetSubKeyNames())
                            {
                                using (var app = key.OpenSubKey(sk))
                                {
                                    var installLoc = app?.GetValue("InstallLocation") as string;
                                    var uninstallStr = app?.GetValue("UninstallString") as string;

                                    bool broken = false;

                                    if (!string.IsNullOrWhiteSpace(installLoc) && !Directory.Exists(installLoc))
                                        broken = true;

                                    if (!string.IsNullOrWhiteSpace(uninstallStr))
                                    {
                                        var candidate = uninstallStr.Trim('"');
                                        if (candidate.Contains(".exe"))
                                        {
                                            var idx = candidate.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                                            candidate = candidate.Substring(0, idx + 4);
                                            if (!File.Exists(candidate))
                                                broken = true;
                                        }
                                    }

                                    if (!broken) continue;

                                    AddIssue(
                                        "Uninstall Entries",
                                        hive,
                                        view,
                                        $@"{(hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU")}\{sub}\{sk}",
                                        "",
                                        "Битая запись деинсталляции",
                                        dryRun =>
                                        {
                                            try
                                            {
                                                if (!dryRun)
                                                {
                                                    using (var wbase = OpenBase(hive, view))
                                                    using (var w = wbase.OpenSubKey(sub, true))
                                                    {
                                                        if (w == null) return RegistryFixResult.Failed;
                                                        w.DeleteSubKeyTree(sk, false);
                                                    }
                                                }
                                                return RegistryFixResult.Fixed;
                                            }
                                            catch
                                            {
                                                return RegistryFixResult.Failed;
                                            }
                                        }
                                    );
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        // ───────────────────────── Shared DLLs ─────────────────────────
        // CCleaner: missing SharedDLL values

        private void ScanSharedDlls()
        {
            const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;

                        foreach (var name in key.GetValueNames())
                        {
                            if (string.IsNullOrWhiteSpace(name)) continue;

                            if (!File.Exists(name))
                            {
                                AddIssue(
                                    "Shared DLLs",
                                    RegistryHive.LocalMachine,
                                    view,
                                    $@"HKLM\{sub}",
                                    name,
                                    "Файл не существует",
                                    dryRun =>
                                    {
                                        try
                                        {
                                            if (!dryRun)
                                            {
                                                using (var wbase = OpenBase(RegistryHive.LocalMachine, view))
                                                using (var w = wbase.OpenSubKey(sub, true))
                                                {
                                                    if (w == null) return RegistryFixResult.Failed;
                                                    w.DeleteValue(name, false);
                                                }
                                            }
                                            return RegistryFixResult.Fixed;
                                        }
                                        catch
                                        {
                                            return RegistryFixResult.Failed;
                                        }
                                    }
                                );
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }
}
