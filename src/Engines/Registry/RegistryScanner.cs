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
        Skipped,
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

        public Func<RegistryFixContext, RegistryFixResult> Fix { get; set; }

        public void RefreshText()
        {
            Category = Localizer.T(CategoryKey);
            Details = Localizer.T(DetailsKey, DetailsArgs);
        }
    }

    /// <summary>
    /// Консервативный и безопасный сканер реестра (Win7..Win11)
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
            ScanUnusedFileExtensions();
            ScanActiveXCom();
            ScanTypeLibraries();
            ScanApplications();
            ScanFonts();
            ScanHelpFiles();
            ScanInstallerReferences();
            ScanObsoleteSoftware();
            ScanStartupEntries();
            ScanStartMenuOrdering();
            ScanMuiCache();
            ScanSoundEvents();
            // ScanWindowsServices() intentionally excluded by default
            // to keep behavior conservative across Win7-Win11.

            return _issues.AsReadOnly();
        }

        // ================= HELPERS =================

        private static RegistryKey OpenBase(RegistryHive hive, RegistryView view)
            => RegistryKey.OpenBaseKey(hive, view);

        private static string ExpandEnv(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            try { return Environment.ExpandEnvironmentVariables(s); }
            catch { return s; }
        }

        private static bool FileMissing(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return true;
            path = ExpandEnv(path).Trim().Trim('"');
            if (!Path.IsPathRooted(path)) return false; // относительные пути не трогаем
            try { return !File.Exists(path); }
            catch { return true; }
        }

        private static bool DirMissing(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return true;
            path = ExpandEnv(path).Trim().Trim('"');
            if (!Path.IsPathRooted(path)) return false;
            try { return !Directory.Exists(path); }
            catch { return true; }
        }

        private static string FirstTokenAsPath(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return null;

            var s = ExpandEnv(commandLine).Trim();
            if (s.StartsWith("\""))
            {
                int end = s.IndexOf('"', 1);
                if (end > 1) return s.Substring(1, end - 1);
            }

            // простой разбор
            var token = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return token?.Trim('"');
        }

        private void AddIssue(
            string categoryKey,
            RegistryHive hive,
            RegistryView view,
            string keyPathUi,
            string valueName,
            string detailsKey,
            object[] detailsArgs,
            Func<bool, RegistryFixResult> legacyFix)
        {
            var issue = new RegistryIssue
            {
                CategoryKey = categoryKey,
                DetailsKey = detailsKey,
                DetailsArgs = detailsArgs,

                Hive = hive,
                View = view,
                KeyPath = keyPathUi,
                ValueName = valueName,
                Fix = ctx => RegistryFixExecutor.ExecuteLegacyFix(ctx, hive, view, keyPathUi, legacyFix)
            };

            issue.RefreshText();
            _issues.Add(issue);
        }

        private static string HiveToUi(RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.LocalMachine: return "HKLM";
                case RegistryHive.CurrentUser: return "HKCU";
                case RegistryHive.ClassesRoot: return "HKCR";
                case RegistryHive.Users: return "HKU";
                default: return hive.ToString();
            }
        }

        // ============================================================
        // 1) Application Paths
        // ============================================================

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

                                var exe = ExpandEnv(raw.Trim().Trim('"'));
                                if (!Path.IsPathRooted(exe)) continue;
                                if (!FileMissing(exe)) continue;

                                AddIssue(
                                    "registry.cat.applications",
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
                                        catch { return RegistryFixResult.Failed; }
                                    });
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 2) Uninstall entries
        // ============================================================

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

                                    var exe = FirstTokenAsPath(uninstall);
                                    if (string.IsNullOrWhiteSpace(exe)) continue;
                                    if (!Path.IsPathRooted(exe)) continue;
                                    if (!FileMissing(exe)) continue;

                                    var hiveStr = HiveToUi(hive);

                                    AddIssue(
                                        "registry.cat.uninstall",
                                        hive,
                                        view,
                                        $@"{hiveStr}\{sub}\{sk}",
                                        "UninstallString",
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
                                            catch { return RegistryFixResult.Failed; }
                                        });
                                }
                            }
                        }
                    }
                    catch { }
                }
        }

        // ============================================================
        // 3) Shared DLLs
        // ============================================================

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
                            if (!FileMissing(path)) continue;

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
                                    catch { return RegistryFixResult.Failed; }
                                });
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 4) Unused File Extensions
        // ============================================================

        private void ScanUnusedFileExtensions()
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.ClassesRoot, view))
                    {
                        // HKCR root: iterate only ".ext"
                        foreach (var name in baseKey.GetSubKeyNames())
                        {
                            if (string.IsNullOrWhiteSpace(name) || !name.StartsWith(".")) continue;
                            if (name.Length < 2) continue;

                            using (var ext = baseKey.OpenSubKey(name))
                            {
                                if (ext == null) continue;

                                // If default ProgID exists -> used
                                var progId = ext.GetValue(null) as string;
                                if (!string.IsNullOrWhiteSpace(progId))
                                    continue;

                                // If OpenWithProgids exists -> used
                                using (var owp = ext.OpenSubKey("OpenWithProgids"))
                                {
                                    if (owp != null && owp.GetValueNames().Length > 0)
                                        continue;
                                }

                                // If OpenWithList exists -> used
                                using (var owl = ext.OpenSubKey("OpenWithList"))
                                {
                                    if (owl != null && owl.GetValueNames().Length > 0)
                                        continue;
                                }

                                AddIssue(
                                    "registry.cat.unused_file_ext",
                                    RegistryHive.ClassesRoot,
                                    view,
                                    $@"HKCR\{name}",
                                    "(Default)",
                                    "registry.details.unused_extension",
                                    new object[] { name },
                                    dryRun =>
                                    {
                                        try
                                        {
                                            if (!dryRun)
                                            {
                                                using (var wbase = OpenBase(RegistryHive.ClassesRoot, view))
                                                    wbase.DeleteSubKeyTree(name, false);
                                            }
                                            return RegistryFixResult.Fixed;
                                        }
                                        catch { return RegistryFixResult.Failed; }
                                    });
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 5) ActiveX / COM Issues (очень аккуратно)
        // ============================================================

        private void ScanActiveXCom()
        {
            const string sub = @"CLSID";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.ClassesRoot, view))
                    using (var clsidRoot = baseKey.OpenSubKey(sub))
                    {
                        if (clsidRoot == null) continue;

                        foreach (var guid in clsidRoot.GetSubKeyNames())
                        {
                            if (string.IsNullOrWhiteSpace(guid) || !guid.StartsWith("{") || !guid.EndsWith("}"))
                                continue;

                            using (var clsid = clsidRoot.OpenSubKey(guid))
                            {
                                if (clsid == null) continue;

                                // check InprocServer32
                                using (var inproc = clsid.OpenSubKey("InprocServer32"))
                                {
                                    var server = inproc?.GetValue(null) as string;
                                    if (!string.IsNullOrWhiteSpace(server))
                                    {
                                        server = ExpandEnv(server);
                                        if (Path.IsPathRooted(server) && FileMissing(server))
                                        {
                                            // Fix: удаляем CLSID ТОЛЬКО если ключ минимальный (только InprocServer32)
                                            AddIssue(
                                                "registry.cat.active_x",
                                                RegistryHive.ClassesRoot,
                                                view,
                                                $@"HKCR\CLSID\{guid}",
                                                "InprocServer32",
                                                "registry.details.com_server_missing",
                                                new object[] { server },
                                                dryRun =>
                                                {
                                                    try
                                                    {
                                                        if (!dryRun)
                                                        {
                                                            using (var wbase = OpenBase(RegistryHive.ClassesRoot, view))
                                                            using (var wroot = wbase.OpenSubKey(sub, true))
                                                            using (var wclsid = wroot?.OpenSubKey(guid))
                                                            {
                                                                if (wclsid == null) return RegistryFixResult.Failed;

                                                                var subkeys = wclsid.GetSubKeyNames();
                                                                // допускаем только InprocServer32 (и больше ничего)
                                                                if (subkeys.Length == 1 && subkeys[0].Equals("InprocServer32", StringComparison.OrdinalIgnoreCase))
                                                                {
                                                                    wroot.DeleteSubKeyTree(guid, false);
                                                                    return RegistryFixResult.Fixed;
                                                                }

                                                                // иначе: удаляем только InprocServer32 (без каскада)
                                                                using (var wip = wclsid.OpenSubKey("InprocServer32", true))
                                                                {
                                                                    wip?.SetValue(null, "", RegistryValueKind.String);
                                                                }
                                                            }
                                                        }
                                                        return RegistryFixResult.Fixed;
                                                    }
                                                    catch { return RegistryFixResult.Failed; }
                                                });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 6) Type Libraries (HKCR\TypeLib)
        // ============================================================

        private void ScanTypeLibraries()
        {
            const string sub = @"TypeLib";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.ClassesRoot, view))
                    using (var root = baseKey.OpenSubKey(sub))
                    {
                        if (root == null) continue;

                        foreach (var guid in root.GetSubKeyNames())
                        {
                            using (var lib = root.OpenSubKey(guid))
                            {
                                if (lib == null) continue;

                                foreach (var ver in lib.GetSubKeyNames())
                                {
                                    // Typical path: {GUID}\{ver}\0\win32 (default)
                                    var pathKey = $@"{guid}\{ver}\0\win32";
                                    using (var win32 = root.OpenSubKey(pathKey))
                                    {
                                        var p = win32?.GetValue(null) as string;
                                        if (string.IsNullOrWhiteSpace(p)) continue;

                                        p = ExpandEnv(p);
                                        if (!Path.IsPathRooted(p)) continue;
                                        if (!FileMissing(p)) continue;

                                        AddIssue(
                                            "registry.cat.type_libraries",
                                            RegistryHive.ClassesRoot,
                                            view,
                                            $@"HKCR\TypeLib\{guid}\{ver}\0\win32",
                                            "(Default)",
                                            "registry.details.typelib_missing",
                                            new object[] { p },
                                            dryRun =>
                                            {
                                                try
                                                {
                                                    if (!dryRun)
                                                    {
                                                        using (var wbase = OpenBase(RegistryHive.ClassesRoot, view))
                                                        using (var wroot = wbase.OpenSubKey(sub, true))
                                                        {
                                                            // удаляем только конкретную версию, не весь GUID
                                                            wroot?.DeleteSubKeyTree($@"{guid}\{ver}", false);
                                                        }
                                                    }
                                                    return RegistryFixResult.Fixed;
                                                }
                                                catch { return RegistryFixResult.Failed; }
                                            });
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 7) Applications (HKCR\Applications\*.exe\shell\open\command)
        // ============================================================

        private void ScanApplications()
        {
            const string sub = @"Applications";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.ClassesRoot, view))
                    using (var root = baseKey.OpenSubKey(sub))
                    {
                        if (root == null) continue;

                        foreach (var appExe in root.GetSubKeyNames())
                        {
                            if (string.IsNullOrWhiteSpace(appExe) || !appExe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                continue;

                            var cmdPath = $@"{sub}\{appExe}\shell\open\command";
                            using (var cmd = baseKey.OpenSubKey(cmdPath))
                            {
                                var raw = cmd?.GetValue(null) as string;
                                if (string.IsNullOrWhiteSpace(raw)) continue;

                                var exe = FirstTokenAsPath(raw);
                                if (string.IsNullOrWhiteSpace(exe)) continue;
                                if (!Path.IsPathRooted(exe)) continue;
                                if (!FileMissing(exe)) continue;

                                AddIssue(
                                    "registry.cat.applications",
                                    RegistryHive.ClassesRoot,
                                    view,
                                    $@"HKCR\{cmdPath}",
                                    "(Default)",
                                    "registry.details.app_command_missing",
                                    new object[] { exe },
                                    dryRun =>
                                    {
                                        try
                                        {
                                            if (!dryRun)
                                            {
                                                using (var wbase = OpenBase(RegistryHive.ClassesRoot, view))
                                                using (var wroot = wbase.OpenSubKey(sub, true))
                                                    wroot?.DeleteSubKeyTree(appExe, false);
                                            }
                                            return RegistryFixResult.Fixed;
                                        }
                                        catch { return RegistryFixResult.Failed; }
                                    });
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 8) Fonts
        // ============================================================

        private void ScanFonts()
        {
            const string sub = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";

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
                            var raw = key.GetValue(valueName) as string;
                            if (string.IsNullOrWhiteSpace(raw)) continue;

                            var fontFile = ExpandEnv(raw).Trim().Trim('"');

                            // Обычно это просто имя файла, лежит в %windir%\Fonts
                            string full = fontFile;
                            if (!Path.IsPathRooted(full))
                            {
                                var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                                full = Path.Combine(win, "Fonts", fontFile);
                            }

                            if (!FileMissing(full))
                                continue;

                            AddIssue(
                                "registry.cat.fonts",
                                RegistryHive.LocalMachine,
                                view,
                                $@"HKLM\{sub}",
                                valueName,
                                "registry.details.font_missing",
                                new object[] { full },
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
                                    catch { return RegistryFixResult.Failed; }
                                });
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 9) Help Files (по uninstall / HelpLink и HelpFile если есть)
        // ============================================================

        private void ScanHelpFiles()
        {
            // Реально встречается в uninstall-ключах:
            // HelpLink (URL) — не трогаем.
            // HelpFile / HelpTelephone / etc — трогаем только file-path.
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
                                    var helpFile = app?.GetValue("HelpFile") as string;
                                    if (string.IsNullOrWhiteSpace(helpFile))
                                        continue;

                                    helpFile = ExpandEnv(helpFile);
                                    if (!Path.IsPathRooted(helpFile)) continue;
                                    if (!FileMissing(helpFile)) continue;

                                    var hiveStr = HiveToUi(hive);

                                    AddIssue(
                                        "registry.cat.help_files",
                                        hive,
                                        view,
                                        $@"{hiveStr}\{sub}\{sk}",
                                        "HelpFile",
                                        "registry.details.help_missing",
                                        new object[] { helpFile },
                                        dryRun =>
                                        {
                                            try
                                            {
                                                if (!dryRun)
                                                {
                                                    using (var wbase = OpenBase(hive, view))
                                                    using (var w = wbase.OpenSubKey($@"{sub}\{sk}", true))
                                                        w?.DeleteValue("HelpFile", false);
                                                }
                                                return RegistryFixResult.Fixed;
                                            }
                                            catch { return RegistryFixResult.Failed; }
                                        });
                                }
                            }
                        }
                    }
                    catch { }
                }
        }

        // ============================================================
        // 10) Installer References (MSI cache refs) — осторожно
        // ============================================================

        private void ScanInstallerReferences()
        {
            // HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData\<SID>\Products\<Product>\InstallProperties
            const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                    using (var userData = baseKey.OpenSubKey(sub))
                    {
                        if (userData == null) continue;

                        foreach (var sid in userData.GetSubKeyNames())
                        {
                            var prodRootPath = $@"{sub}\{sid}\Products";
                            using (var prods = baseKey.OpenSubKey(prodRootPath))
                            {
                                if (prods == null) continue;

                                foreach (var prod in prods.GetSubKeyNames())
                                {
                                    var ipPath = $@"{prodRootPath}\{prod}\InstallProperties";
                                    using (var ip = baseKey.OpenSubKey(ipPath))
                                    {
                                        if (ip == null) continue;

                                        var localPackage = ip.GetValue("LocalPackage") as string; // *.msi in Installer cache
                                        if (string.IsNullOrWhiteSpace(localPackage))
                                            continue;

                                        localPackage = ExpandEnv(localPackage);
                                        if (!Path.IsPathRooted(localPackage)) continue;
                                        if (!FileMissing(localPackage)) continue;

                                        AddIssue(
                                            "registry.cat.installer_refs",
                                            RegistryHive.LocalMachine,
                                            view,
                                            $@"HKLM\{ipPath}",
                                            "LocalPackage",
                                            "registry.details.installer_package_missing",
                                            new object[] { localPackage },
                                            dryRun =>
                                            {
                                                // Fix: удаляем только LocalPackage value (не сносим продукт целиком)
                                                try
                                                {
                                                    if (!dryRun)
                                                    {
                                                        using (var wbase = OpenBase(RegistryHive.LocalMachine, view))
                                                        using (var wip = wbase.OpenSubKey(ipPath, true))
                                                            wip?.DeleteValue("LocalPackage", false);
                                                    }
                                                    return RegistryFixResult.Fixed;
                                                }
                                                catch { return RegistryFixResult.Failed; }
                                            });
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 11) Obsolete Software (битые InstallLocation / DisplayIcon)
        // ============================================================

        private void ScanObsoleteSoftware()
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
                                    if (string.IsNullOrWhiteSpace(name)) continue;

                                    var installLocation = app.GetValue("InstallLocation") as string;
                                    var displayIcon = app.GetValue("DisplayIcon") as string;

                                    bool badLocation = !string.IsNullOrWhiteSpace(installLocation) && DirMissing(installLocation);
                                    bool badIcon = false;
                                    if (!string.IsNullOrWhiteSpace(displayIcon))
                                    {
                                        var iconPath = FirstTokenAsPath(displayIcon);
                                        if (!string.IsNullOrWhiteSpace(iconPath) && Path.IsPathRooted(iconPath))
                                            badIcon = FileMissing(iconPath);
                                    }

                                    if (!badLocation && !badIcon) continue;

                                    var hiveStr = HiveToUi(hive);

                                    // Fix: удаляем только битые значения, не весь uninstall
                                    AddIssue(
                                        "registry.cat.obsolete_software",
                                        hive,
                                        view,
                                        $@"{hiveStr}\{sub}\{sk}",
                                        badLocation ? "InstallLocation" : "DisplayIcon",
                                        "registry.details.obsolete_reference",
                                        new object[] { badLocation ? installLocation : displayIcon },
                                        dryRun =>
                                        {
                                            try
                                            {
                                                if (!dryRun)
                                                {
                                                    using (var wbase = OpenBase(hive, view))
                                                    using (var w = wbase.OpenSubKey($@"{sub}\{sk}", true))
                                                    {
                                                        if (w == null) return RegistryFixResult.Failed;
                                                        if (badLocation) w.DeleteValue("InstallLocation", false);
                                                        if (badIcon) w.DeleteValue("DisplayIcon", false);
                                                    }
                                                }
                                                return RegistryFixResult.Fixed;
                                            }
                                            catch { return RegistryFixResult.Failed; }
                                        });
                                }
                            }
                        }
                    }
                    catch { }
                }
        }

        // ============================================================
        // 12) Startup (Run/RunOnce)
        // ============================================================

        private void ScanStartupEntries()
        {
            var runKeys = new[]
            {
                new { Hive = RegistryHive.CurrentUser, Sub = @"Software\Microsoft\Windows\CurrentVersion\Run" },
                new { Hive = RegistryHive.CurrentUser, Sub = @"Software\Microsoft\Windows\CurrentVersion\RunOnce" },
                new { Hive = RegistryHive.LocalMachine, Sub = @"Software\Microsoft\Windows\CurrentVersion\Run" },
                new { Hive = RegistryHive.LocalMachine, Sub = @"Software\Microsoft\Windows\CurrentVersion\RunOnce" },
            };

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                foreach (var rk in runKeys)
                {
                    try
                    {
                        using (var baseKey = OpenBase(rk.Hive, view))
                        using (var key = baseKey.OpenSubKey(rk.Sub))
                        {
                            if (key == null) continue;

                            foreach (var valueName in key.GetValueNames())
                            {
                                var cmd = key.GetValue(valueName) as string;
                                if (string.IsNullOrWhiteSpace(cmd)) continue;

                                var exe = FirstTokenAsPath(cmd);
                                if (string.IsNullOrWhiteSpace(exe)) continue;
                                if (!Path.IsPathRooted(exe)) continue;
                                if (!FileMissing(exe)) continue;

                                var hiveStr = HiveToUi(rk.Hive);

                                AddIssue(
                                    "registry.cat.startup",
                                    rk.Hive,
                                    view,
                                    $@"{hiveStr}\{rk.Sub}",
                                    valueName,
                                    "registry.details.startup_missing",
                                    new object[] { exe },
                                    dryRun =>
                                    {
                                        try
                                        {
                                            if (!dryRun)
                                            {
                                                using (var wbase = OpenBase(rk.Hive, view))
                                                using (var w = wbase.OpenSubKey(rk.Sub, true))
                                                    w?.DeleteValue(valueName, false);
                                            }
                                            return RegistryFixResult.Fixed;
                                        }
                                        catch { return RegistryFixResult.Failed; }
                                    });
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        // ============================================================
        // 13) Start Menu Ordering (сброс MenuOrder при повреждении/мусоре)
        // ============================================================

        private void ScanStartMenuOrdering()
        {
            // CCleaner чистит “MenuOrder”.
            // Без парсинга PIDL безопасная стратегия:
            // - считать проблемой наличие ключа с большим числом пустых/битых подпапок (после миграций)
            // - фикс: удалить MenuOrder целиком (Windows пересоздаст)
            const string sub = @"Software\Microsoft\Windows\CurrentVersion\Explorer\MenuOrder";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.CurrentUser, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;

                        // простое эвристическое условие: много подпапок и/или Order-значения пустые
                        int subCount = 0;
                        int emptyOrders = 0;

                        foreach (var sk in key.GetSubKeyNames())
                        {
                            subCount++;
                            using (var k2 = key.OpenSubKey(sk))
                            {
                                if (k2 == null) continue;
                                if (k2.GetValue("Order") == null && k2.GetValue("OrderEx") == null)
                                    emptyOrders++;
                            }
                        }

                        if (subCount == 0) continue;
                        if (emptyOrders == 0) continue;

                        AddIssue(
                            "registry.cat.start_menu",
                            RegistryHive.CurrentUser,
                            view,
                            $@"HKCU\{sub}",
                            "",
                            "registry.details.menuorder_reset",
                            new object[] { },
                            dryRun =>
                            {
                                try
                                {
                                    if (!dryRun)
                                    {
                                        using (var wbase = OpenBase(RegistryHive.CurrentUser, view))
                                            wbase.DeleteSubKeyTree(sub, false);
                                    }
                                    return RegistryFixResult.Fixed;
                                }
                                catch { return RegistryFixResult.Failed; }
                            });
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 14) MUI Cache (удаляем записи на несуществующие файлы)
        // ============================================================

        private void ScanMuiCache()
        {
            // Common location:
            // HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache
            const string sub = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.CurrentUser, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;

                        foreach (var valueName in key.GetValueNames())
                        {
                            // valueName often is path
                            var p = ExpandEnv(valueName);
                            if (!Path.IsPathRooted(p)) continue;
                            if (!FileMissing(p)) continue;

                            AddIssue(
                                "registry.cat.mui_cache",
                                RegistryHive.CurrentUser,
                                view,
                                $@"HKCU\{sub}",
                                valueName,
                                "registry.details.muicache_missing",
                                new object[] { p },
                                dryRun =>
                                {
                                    try
                                    {
                                        if (!dryRun)
                                        {
                                            using (var wbase = OpenBase(RegistryHive.CurrentUser, view))
                                            using (var w = wbase.OpenSubKey(sub, true))
                                                w?.DeleteValue(valueName, false);
                                        }
                                        return RegistryFixResult.Fixed;
                                    }
                                    catch { return RegistryFixResult.Failed; }
                                });
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 15) Sound Events (битые wav)
        // ============================================================

        private void ScanSoundEvents()
        {
            const string sub = @"AppEvents\Schemes\Apps";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.CurrentUser, view))
                    using (var root = baseKey.OpenSubKey(sub))
                    {
                        if (root == null) continue;

                        // iterate: Apps\<app>\<event>\.Current
                        foreach (var app in root.GetSubKeyNames())
                        {
                            using (var appKey = root.OpenSubKey(app))
                            {
                                if (appKey == null) continue;

                                foreach (var ev in appKey.GetSubKeyNames())
                                {
                                    var curPath = $@"{sub}\{app}\{ev}\.Current";
                                    using (var cur = baseKey.OpenSubKey(curPath))
                                    {
                                        var wav = cur?.GetValue(null) as string;
                                        if (string.IsNullOrWhiteSpace(wav)) continue;

                                        wav = ExpandEnv(wav);
                                        if (!Path.IsPathRooted(wav)) continue;
                                        if (!FileMissing(wav)) continue;

                                        AddIssue(
                                            "registry.cat.sound_events",
                                            RegistryHive.CurrentUser,
                                            view,
                                            $@"HKCU\{curPath}",
                                            "(Default)",
                                            "registry.details.sound_missing",
                                            new object[] { wav },
                                            dryRun =>
                                            {
                                                try
                                                {
                                                    if (!dryRun)
                                                    {
                                                        using (var wbase = OpenBase(RegistryHive.CurrentUser, view))
                                                        using (var w = wbase.OpenSubKey(curPath, true))
                                                            w?.SetValue(null, "", RegistryValueKind.String);
                                                    }
                                                    return RegistryFixResult.Fixed;
                                                }
                                                catch { return RegistryFixResult.Failed; }
                                            });
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        // ============================================================
        // 15) Windows Services (в рамках 15 категорий — включаем вместо лишней)
        // ============================================================

        private void ScanWindowsServices()
        {
            // Для безопасности:
            // - НЕ удаляем сервис
            // - Фикс: если ImagePath указывает на отсутствующий exe/dll, ставим Start=4 (Disabled)
            // Это обратимо и минимально разрушительно.
            const string sub = @"SYSTEM\CurrentControlSet\Services";

            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                    using (var root = baseKey.OpenSubKey(sub))
                    {
                        if (root == null) continue;

                        foreach (var svc in root.GetSubKeyNames())
                        {
                            using (var sk = root.OpenSubKey(svc))
                            {
                                if (sk == null) continue;

                                var imagePath = sk.GetValue("ImagePath") as string;
                                if (string.IsNullOrWhiteSpace(imagePath))
                                    continue;

                                // svchost.exe — системный хост, не трогаем
                                var exe = FirstTokenAsPath(imagePath);
                                if (string.IsNullOrWhiteSpace(exe)) continue;

                                exe = ExpandEnv(exe);

                                if (!Path.IsPathRooted(exe))
                                    continue;

                                if (!FileMissing(exe))
                                    continue;

                                AddIssue(
                                    "registry.cat.startup",
                                    RegistryHive.LocalMachine,
                                    view,
                                    $@"HKLM\{sub}\{svc}",
                                    "ImagePath",
                                    "registry.details.service_image_missing",
                                    new object[] { exe },
                                    dryRun =>
                                    {
                                        try
                                        {
                                            if (!dryRun)
                                            {
                                                using (var wbase = OpenBase(RegistryHive.LocalMachine, view))
                                                using (var w = wbase.OpenSubKey($@"{sub}\{svc}", true))
                                                {
                                                    if (w == null) return RegistryFixResult.Failed;
                                                    w.SetValue("Start", 4, RegistryValueKind.DWord); // Disabled
                                                }
                                            }
                                            return RegistryFixResult.Fixed;
                                        }
                                        catch { return RegistryFixResult.Failed; }
                                    });
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }
}


