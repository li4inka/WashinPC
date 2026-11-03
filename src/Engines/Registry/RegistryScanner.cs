using System.Threading;
using WashinPC.Engines.Operations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using WashinPC.Core.Logging;
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

        public bool CanFix { get; set; }
        public Func<RegistryFixContext, RegistryFixResult> Fix { get; set; }

        public void RefreshText()
        {
            Category = Localizer.T(CategoryKey);
            Details = Localizer.T(DetailsKey, DetailsArgs) + (CanFix ? "" : " — " + Localizer.T("op.diagnostic"));
        }
    }

    /// <summary>
    /// Консервативный и безопасный сканер реестра (Win7..Win11)
    /// </summary>
    public sealed class RegistryScanner
    {
        private readonly List<RegistryIssue> _issues = new List<RegistryIssue>();

        private CancellationToken _token;
        public IList<RegistryIssue> ScanAll(CancellationToken token = default(CancellationToken), ISet<string> selected = null)
        {
            _issues.Clear();
            _token = token;

            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.applications")) ScanApplicationPaths();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.uninstall")) ScanUninstallEntries();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.shared_dlls")) ScanSharedDlls();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.unused_file_ext")) ScanUnusedFileExtensions();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.active_x")) ScanActiveXCom();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.type_libraries")) ScanTypeLibraries();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.applications")) ScanApplications();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.fonts")) ScanFonts();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.help_files")) ScanHelpFiles();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.installer_refs")) ScanInstallerReferences();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.obsolete_software")) ScanObsoleteSoftware();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.startup")) ScanStartupEntries();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.start_menu")) ScanStartMenuOrdering();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.mui_cache")) ScanMuiCache();
            token.ThrowIfCancellationRequested();
            if (selected == null || selected.Contains("registry.cat.sound_events")) ScanSoundEvents();
            if (selected != null && selected.Contains("registry.cat.services")) ScanWindowsServices();

            return _issues.AsReadOnly();
        }

        // ================= HELPERS =================

        private static RegistryKey OpenBase(RegistryHive hive, RegistryView view)
            => RegistryKey.OpenBaseKey(hive, view);

        private static void LogScanSectionFailure(
            string section,
            Exception ex,
            RegistryHive? hive = null,
            RegistryView? view = null)
        {
            string scope = string.Empty;
            if (hive.HasValue && view.HasValue)
                scope = $" hive={HiveToUi(hive.Value)} view={view.Value}";
            else if (view.HasValue)
                scope = $" view={view.Value}";

            AppLogger.Warn(
                "registry",
                "scan.section.fail",
                $"Registry scan section failed: {section}.{scope} exceptionType={ex.GetType().Name}");
        }

        private static string ExpandEnv(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            try { return Environment.ExpandEnvironmentVariables(s); }
            catch { return s; }
        }

        private static bool FileMissing(string path) => PathSafety.Probe(path) == PathState.Missing;
        private static bool DirMissing(string path) => PathSafety.Probe(path) == PathState.Missing;

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
            Func<bool, RegistryFixResult> legacyFix,
            string mutationKeyPath = null,
            Func<bool> isStillApplicable = null)
        {
            var canFix = legacyFix != null;
            var backupPath = mutationKeyPath ?? keyPathUi;
            var subKey = RegistryFixExecutor.ExtractSubKeyPath(hive, backupPath);
            var valueRepair = categoryKey == "registry.cat.shared_dlls" || categoryKey == "registry.cat.help_files" ||
                categoryKey == "registry.cat.fonts" || categoryKey == "registry.cat.installer_refs" ||
                categoryKey == "registry.cat.obsolete_software" || categoryKey == "registry.cat.mui_cache" ||
                categoryKey == "registry.cat.sound_events" ||
                (categoryKey == "registry.cat.startup" && detailsKey == "registry.details.startup_missing");
            Func<string> capture = () => valueRepair
                ? RegistryBackup.CaptureValues(hive, view, subKey, categoryKey == "registry.cat.obsolete_software"
                    ? new[] { "InstallLocation", "DisplayIcon" }
                    : new[] { valueName == "(Default)" ? "" : valueName })
                : RegistryBackup.Capture(hive, view, subKey);
            string snapshot = null;
            if (canFix)
            {
                try { snapshot = capture(); canFix = snapshot != null; }
                catch { canFix = false; }
            }
            Func<bool> validate = () =>
            {
                if (!canFix) return false;
                try
                {
                    if (snapshot != capture()) return false;
                    if (isStillApplicable != null) return isStillApplicable();
                    // These findings depend only on registry contents covered by the snapshot.
                    if (categoryKey == "registry.cat.unused_file_ext" || categoryKey == "registry.cat.start_menu") return true;
                    return detailsArgs != null && detailsArgs.Length == 1 && FileMissing(detailsArgs[0] as string);
                }
                catch { return false; }
            };
            var issue = new RegistryIssue
            {
                CanFix = canFix,
                CategoryKey = categoryKey,
                DetailsKey = detailsKey,
                DetailsArgs = detailsArgs,

                Hive = hive,
                View = view,
                KeyPath = keyPathUi,
                ValueName = valueName,
                Fix = ctx => canFix ? RegistryFixExecutor.ExecuteLegacyFix(ctx, hive, view, backupPath, legacyFix, revalidate: validate) : RegistryFixResult.Skipped
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;

                        foreach (var sk in key.GetSubKeyNames())
                        {
                            _token.ThrowIfCancellationRequested();
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
                                    dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteKey(RegistryHive.LocalMachine, view, $@"{sub}\{sk}"));
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanApplicationPaths", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                    try
                    {
                        using (var baseKey = OpenBase(hive, view))
                        using (var key = baseKey.OpenSubKey(sub))
                        {
                            if (key == null) continue;

                            foreach (var sk in key.GetSubKeyNames())
                            {
                            _token.ThrowIfCancellationRequested();
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
                                        dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteKey(hive, view, $@"{sub}\{sk}"));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogScanSectionFailure("ScanUninstallEntries", ex, hive, view);
                    }
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;

                        foreach (var valueName in key.GetValueNames())
                        {
                            _token.ThrowIfCancellationRequested();
                            var path = ExpandEnv(valueName);
                            if (!Path.IsPathRooted(path)) continue;
                            if (!FileMissing(path)) continue;

                            var count = key.GetValue(valueName);
                            if (!(count is int c) || c != 0)
                                continue;

                            AddIssue(
                                "registry.cat.shared_dlls",
                                RegistryHive.LocalMachine,
                                view,
                                $@"HKLM\{sub}",
                                valueName,
                                "registry.details.file_missing",
                                new object[] { path },
                                dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteValue(RegistryHive.LocalMachine, view, sub, valueName));
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanSharedDlls", ex, view: view);
                }
            }
        }

        // ============================================================
        // 4) Unused File Extensions
        // ============================================================

        private void ScanUnusedFileExtensions()
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.ClassesRoot, view))
                    {
                        // HKCR root: iterate only ".ext"
                        foreach (var name in baseKey.GetSubKeyNames())
                        {
                            _token.ThrowIfCancellationRequested();
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
                                    dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteKey(RegistryHive.ClassesRoot, view, name));
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanUnusedFileExtensions", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.ClassesRoot, view))
                    using (var clsidRoot = baseKey.OpenSubKey(sub))
                    {
                        if (clsidRoot == null) continue;

                        foreach (var guid in clsidRoot.GetSubKeyNames())
                        {
                            _token.ThrowIfCancellationRequested();
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
                                                dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.RepairCom(view, $@"{sub}\{guid}"));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanActiveXCom", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.ClassesRoot, view))
                    using (var root = baseKey.OpenSubKey(sub))
                    {
                        if (root == null) continue;

                        foreach (var guid in root.GetSubKeyNames())
                        {
                            _token.ThrowIfCancellationRequested();
                            using (var lib = root.OpenSubKey(guid))
                            {
                                if (lib == null) continue;

                                foreach (var ver in lib.GetSubKeyNames())
                                {
                            _token.ThrowIfCancellationRequested();
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
                                            dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteKey(RegistryHive.ClassesRoot, view, $@"{sub}\{guid}\{ver}"), mutationKeyPath: $@"HKCR\TypeLib\{guid}\{ver}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanTypeLibraries", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.ClassesRoot, view))
                    using (var root = baseKey.OpenSubKey(sub))
                    {
                        if (root == null) continue;

                        foreach (var appExe in root.GetSubKeyNames())
                        {
                            _token.ThrowIfCancellationRequested();
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
                                    dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteKey(RegistryHive.ClassesRoot, view, $@"{sub}\{appExe}"), mutationKeyPath: $@"HKCR\Applications\{appExe}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanApplications", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;

                        foreach (var valueName in key.GetValueNames())
                        {
                            _token.ThrowIfCancellationRequested();
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
                                dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteValue(RegistryHive.LocalMachine, view, sub, valueName));
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanFonts", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                    try
                    {
                        using (var baseKey = OpenBase(hive, view))
                        using (var key = baseKey.OpenSubKey(sub))
                        {
                            if (key == null) continue;

                            foreach (var sk in key.GetSubKeyNames())
                            {
                            _token.ThrowIfCancellationRequested();
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
                                        dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteValue(hive, view, $@"{sub}\{sk}", "HelpFile"));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogScanSectionFailure("ScanHelpFiles", ex, hive, view);
                    }
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                    using (var userData = baseKey.OpenSubKey(sub))
                    {
                        if (userData == null) continue;

                        foreach (var sid in userData.GetSubKeyNames())
                        {
                            _token.ThrowIfCancellationRequested();
                            var prodRootPath = $@"{sub}\{sid}\Products";
                            using (var prods = baseKey.OpenSubKey(prodRootPath))
                            {
                                if (prods == null) continue;

                                foreach (var prod in prods.GetSubKeyNames())
                                {
                            _token.ThrowIfCancellationRequested();
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
                                            dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteValue(RegistryHive.LocalMachine, view, ipPath, "LocalPackage"));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanInstallerReferences", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                    try
                    {
                        using (var baseKey = OpenBase(hive, view))
                        using (var key = baseKey.OpenSubKey(sub))
                        {
                            if (key == null) continue;

                            foreach (var sk in key.GetSubKeyNames())
                            {
                            _token.ThrowIfCancellationRequested();
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
                                        dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteValues(hive, view, $@"{sub}\{sk}", new[] { badLocation ? "InstallLocation" : null, badIcon ? "DisplayIcon" : null }), isStillApplicable: () =>
                                            (!badLocation || DirMissing(installLocation)) &&
                                            (!badIcon || FileMissing(FirstTokenAsPath(displayIcon))));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogScanSectionFailure("ScanObsoleteSoftware", ex, hive, view);
                    }
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
                            _token.ThrowIfCancellationRequested();
                foreach (var rk in runKeys)
                {
                            _token.ThrowIfCancellationRequested();
                    try
                    {
                        using (var baseKey = OpenBase(rk.Hive, view))
                        using (var key = baseKey.OpenSubKey(rk.Sub))
                        {
                            if (key == null) continue;

                            foreach (var valueName in key.GetValueNames())
                            {
                            _token.ThrowIfCancellationRequested();
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
                                    dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteValue(rk.Hive, view, rk.Sub, valueName));
                            }
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        LogScanSectionFailure("ScanStartupEntries", ex, view: view);
                    }
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
                            _token.ThrowIfCancellationRequested();
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
                            _token.ThrowIfCancellationRequested();
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
                            dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteKey(RegistryHive.CurrentUser, view, sub));
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanStartMenuOrdering", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.CurrentUser, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;

                        foreach (var valueName in key.GetValueNames())
                        {
                            _token.ThrowIfCancellationRequested();
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
                                dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.DeleteValue(RegistryHive.CurrentUser, view, sub, valueName));
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanMuiCache", ex, view: view);
                }
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
                            _token.ThrowIfCancellationRequested();
                try
                {
                    using (var baseKey = OpenBase(RegistryHive.CurrentUser, view))
                    using (var root = baseKey.OpenSubKey(sub))
                    {
                        if (root == null) continue;

                        // iterate: Apps\<app>\<event>\.Current
                        foreach (var app in root.GetSubKeyNames())
                        {
                            _token.ThrowIfCancellationRequested();
                            using (var appKey = root.OpenSubKey(app))
                            {
                                if (appKey == null) continue;

                                foreach (var ev in appKey.GetSubKeyNames())
                                {
                            _token.ThrowIfCancellationRequested();
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
                                            dryRun => dryRun ? RegistryFixResult.Skipped : RegistryMutations.SetValue(RegistryHive.CurrentUser, view, curPath, "", "", RegistryValueKind.String));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    LogScanSectionFailure("ScanSoundEvents", ex, view: view);
                }
            }
        }

        // ============================================================
        // 15) Windows Services (в рамках 15 категорий — включаем вместо лишней)
        // ============================================================

        internal IServiceManager ServiceManager { get; set; } = new WindowsServiceManager();
        private void ScanWindowsServices()
        {
            _issues.AddRange(ServiceRepairs.Scan(ServiceManager, _token));
        }
    }
}


