using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using WashinPC.Localization;

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
        // Keys for localization (for instant language switch)
        public string CategoryKey { get; set; }
        public string DetailsKey { get; set; }
        public object[] DetailsArgs { get; set; }

        public RegistryHive Hive { get; set; }
        public RegistryView View { get; set; }

        public string KeyPath { get; set; }
        public string ValueName { get; set; }

        // For compatibility (current language snapshot)
        public string Category { get; set; }
        public string Details { get; set; }

        public Func<bool, RegistryFixResult> Fix { get; set; }

        public void RefreshText()
        {
            Category = Localizer.T(CategoryKey);
            Details = Localizer.T(DetailsKey, DetailsArgs);
        }
    }

    /// <summary>
    /// Консервативный и безопасный сканер реестра
    /// </summary>
    public sealed class RegistryScanner
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

        // ================= SAFETY =================

        private static bool IsForbiddenKey(string keyPathUi)
        {
            if (string.IsNullOrWhiteSpace(keyPathUi))
                return true;

            var k = keyPathUi.ToUpperInvariant();

            return
                k.Contains(@"\SYSTEM\") ||
                k.Contains(@"\SERVICES\") ||
                k.Contains(@"\DRIVERS\") ||
                k.Contains(@"\POLICIES\") ||
                k.Contains(@"\CLASSES\") ||
                k.Contains(@"\CLSID\") ||
                k.Contains(@"\INTERFACE\") ||
                k.Contains(@"\TYPELIB\");
        }

        private static RegistryKey OpenBase(RegistryHive hive, RegistryView view)
            => RegistryKey.OpenBaseKey(hive, view);

        private static string ExpandEnv(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            try { return Environment.ExpandEnvironmentVariables(s); }
            catch { return s; }
        }

        private void AddIssue(
            string categoryKey,
            RegistryHive hive,
            RegistryView view,
            string keyPathUi,
            string valueName,
            string detailsKey,
            object[] detailsArgs,
            Func<bool, RegistryFixResult> fix)
        {
            if (IsForbiddenKey(keyPathUi))
                return;

            var issue = new RegistryIssue
            {
                CategoryKey = categoryKey,
                DetailsKey = detailsKey,
                DetailsArgs = detailsArgs,

                Hive = hive,
                View = view,
                KeyPath = keyPathUi,
                ValueName = valueName,
                Fix = fix
            };

            issue.RefreshText(); // fill Category/Details in current language
            _issues.Add(issue);
        }

        // ================= APP PATHS =================

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
                                var raw = app?.GetValue(null) as string;
                                if (string.IsNullOrWhiteSpace(raw)) continue;

                                var exe = ExpandEnv(raw.Trim('"'));
                                if (!Path.IsPathRooted(exe)) continue;
                                if (File.Exists(exe)) continue;

                                AddIssue(
                                    "registry.cat.app_paths",
                                    RegistryHive.LocalMachine,
                                    view,
                                    $@"HKLM\{sub}\{sk}",
                                    "(Default)",
                                    "registry.details.path_missing",
                                    new object[] { exe },
                                    dryRun =>
                                    {
                                        try
                                        {
                                            if (!dryRun)
                                            {
                                                using (var wbase = OpenBase(RegistryHive.LocalMachine, view))
                                                using (var w = wbase.OpenSubKey(sub, true))
                                                    w?.DeleteSubKeyTree(sk, false);
                                            }
                                            return RegistryFixResult.Fixed;
                                        }
                                        catch
                                        {
                                            return RegistryFixResult.Failed;
                                        }
                                    });
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ================= UNINSTALL =================

        private void ScanUninstallEntries()
        {
            const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
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
                                    var name = app?.GetValue("DisplayName") as string;
                                    var uninstall = app?.GetValue("UninstallString") as string;
                                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(uninstall))
                                        continue;

                                    uninstall = ExpandEnv(uninstall);

                                    // MSI uninstallers we do NOT touch
                                    if (uninstall.IndexOf("msiexec", StringComparison.OrdinalIgnoreCase) >= 0)
                                        continue;

                                    var exe = uninstall.Trim('"').Split(' ').FirstOrDefault();
                                    if (!Path.IsPathRooted(exe)) continue;
                                    if (File.Exists(exe)) continue;

                                    var hiveStr = hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";

                                    AddIssue(
                                        "registry.cat.uninstall",
                                        hive,
                                        view,
                                        $@"{hiveStr}\{sub}\{sk}",
                                        "",
                                        "registry.details.uninstall_missing",
                                        new object[] { exe },
                                        dryRun =>
                                        {
                                            try
                                            {
                                                if (!dryRun)
                                                {
                                                    using (var wbase = OpenBase(hive, view))
                                                    using (var w = wbase.OpenSubKey(sub, true))
                                                        w?.DeleteSubKeyTree(sk, false);
                                                }
                                                return RegistryFixResult.Fixed;
                                            }
                                            catch
                                            {
                                                return RegistryFixResult.Failed;
                                            }
                                        });
                                }
                            }
                        }
                    }
                    catch { }
                }
        }

        // ================= SHARED DLLS =================

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

                        foreach (var valueName in key.GetValueNames())
                        {
                            var path = ExpandEnv(valueName);
                            if (!Path.IsPathRooted(path)) continue;
                            if (File.Exists(path)) continue;

                            var count = key.GetValue(valueName);
                            if (count is int c && c > 1)
                                continue;

                            AddIssue(
                                "registry.cat.shared_dlls",
                                RegistryHive.LocalMachine,
                                view,
                                $@"HKLM\{sub}",
                                valueName,
                                "registry.details.file_missing",
                                new object[] { path },
                                dryRun =>
                                {
                                    try
                                    {
                                        if (!dryRun)
                                        {
                                            using (var wbase = OpenBase(RegistryHive.LocalMachine, view))
                                            using (var w = wbase.OpenSubKey(sub, true))
                                                w?.DeleteValue(valueName, false);
                                        }
                                        return RegistryFixResult.Fixed;
                                    }
                                    catch
                                    {
                                        return RegistryFixResult.Failed;
                                    }
                                });
                        }
                    }
                }
                catch { }
            }
        }
    }
}
