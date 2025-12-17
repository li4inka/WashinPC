using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public string KeyPath { get; set; }     // Для UI: HKLM\...\...
        public string ValueName { get; set; }
        public string Details { get; set; }

        /// <summary>Func(dryRun) => Fixed/Failed</summary>
        public Func<bool, RegistryFixResult> Fix { get; set; }
        public RegistryFixResult Result { get; set; } = RegistryFixResult.None;
    }

    /// <summary>
    /// Очень консервативный "чистильщик" реестра:
    /// - не трогаем HKCR/CLSID/COM/TypeLib/Interfaces
    /// - только безопасные категории: App Paths, Uninstall (только EXE-унсталлеры), SharedDLLs
    /// - перед удалением в Fix перепроверяем "битость"
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

        // ---------------- helpers ----------------

        private static RegistryKey OpenBase(RegistryHive hive, RegistryView view)
            => RegistryKey.OpenBaseKey(hive, view);

        private void AddIssue(
            string category,
            RegistryHive hive,
            RegistryView view,
            string keyPathUi,
            string valueName,
            string details,
            Func<bool, RegistryFixResult> fix)
        {
            _issues.Add(new RegistryIssue
            {
                Category = category,
                Hive = hive,
                View = view,
                KeyPath = keyPathUi,
                ValueName = valueName,
                Details = details,
                Fix = fix
            });
        }

        private static string ExpandEnv(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            try { return Environment.ExpandEnvironmentVariables(s); }
            catch { return s; }
        }

        private static string ExtractExePathFromCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return null;

            command = ExpandEnv(command).Trim();

            // Типичные форматы:
            // "C:\Path\uninstall.exe" /S
            // C:\Path\uninstall.exe /S
            // rundll32.exe ...
            // msiexec /x {GUID}
            // "C:\Path\something.exe"
            if (command.StartsWith("\""))
            {
                var end = command.IndexOf('"', 1);
                if (end > 1)
                {
                    var p = command.Substring(1, end - 1);
                    return p.Trim();
                }
            }

            // иначе берём первый токен до пробела
            var first = command.Split(new[] { ' ' }, 2)[0].Trim();
            if (string.IsNullOrWhiteSpace(first)) return null;
            return first;
        }

        private static bool IsMsiUninstall(string uninstallString, RegistryKey appKey)
        {
            // MSI-ключи часто имеют WindowsInstaller=1 и UninstallString=msiexec...
            try
            {
                var wi = appKey?.GetValue("WindowsInstaller");
                if (wi is int wiInt && wiInt == 1) return true;
                if (wi is string wiStr && wiStr == "1") return true;
            }
            catch { }

            if (string.IsNullOrWhiteSpace(uninstallString)) return false;

            var s = ExpandEnv(uninstallString).Trim().ToLowerInvariant();
            if (s.StartsWith("msiexec"))
                return true;

            // иногда: "C:\Windows\System32\msiexec.exe" /x {GUID}
            if (s.Contains("msiexec.exe"))
                return true;

            return false;
        }

        private static bool LooksLikeValidSystemUninstaller(string uninstallString)
        {
            if (string.IsNullOrWhiteSpace(uninstallString)) return false;

            var s = ExpandEnv(uninstallString).Trim().ToLowerInvariant();

            // rundll32.exe, control.exe appwiz.cpl и т.п. не считаем "битостью"
            if (s.StartsWith("rundll32") || s.Contains("rundll32.exe"))
                return true;

            if (s.StartsWith("control") || s.Contains("control.exe"))
                return true;

            return false;
        }

        // ---------------- Application Paths ----------------

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

                                // Иногда туда пишут команду с аргументами — извлечём exe
                                var exe = ExtractExePathFromCommand(raw);
                                if (string.IsNullOrWhiteSpace(exe)) continue;

                                exe = ExpandEnv(exe);

                                // Очень консервативно: помечаем только если путь абсолютный и файла нет
                                if (Path.IsPathRooted(exe) && !File.Exists(exe))
                                {
                                    var skLocal = sk;

                                    AddIssue(
                                        "Application Paths",
                                        RegistryHive.LocalMachine,
                                        view,
                                        $@"HKLM\{sub}\{skLocal}",
                                        "(Default)",
                                        "Путь не существует: " + exe,
                                        dryRun =>
                                        {
                                            try
                                            {
                                                // перепроверка в момент фикса
                                                if (!IsAppPathBroken(view, skLocal))
                                                    return RegistryFixResult.Fixed;

                                                if (!dryRun)
                                                {
                                                    using (var wbase = OpenBase(RegistryHive.LocalMachine, view))
                                                    using (var w = wbase.OpenSubKey(sub, writable: true))
                                                    {
                                                        if (w == null) return RegistryFixResult.Failed;
                                                        w.DeleteSubKeyTree(skLocal, throwOnMissingSubKey: false);
                                                    }
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
                }
                catch { }
            }
        }

        private bool IsAppPathBroken(RegistryView view, string subKeyName)
        {
            const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
            try
            {
                using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                using (var key = baseKey.OpenSubKey(sub))
                using (var app = key?.OpenSubKey(subKeyName))
                {
                    var raw = app?.GetValue(null) as string;
                    if (string.IsNullOrWhiteSpace(raw)) return false;

                    var exe = ExtractExePathFromCommand(raw);
                    if (string.IsNullOrWhiteSpace(exe)) return false;

                    exe = ExpandEnv(exe);
                    if (!Path.IsPathRooted(exe)) return false;

                    return !File.Exists(exe);
                }
            }
            catch
            {
                return false;
            }
        }

        // ---------------- Uninstall Entries ----------------

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
                                    if (app == null) continue;

                                    var displayName = app.GetValue("DisplayName") as string;
                                    var uninstallStr = app.GetValue("UninstallString") as string;
                                    var installLoc = app.GetValue("InstallLocation") as string;
                                    var displayIcon = app.GetValue("DisplayIcon") as string;

                                    // Если нет DisplayName — часто это служебные ключи, не трогаем
                                    if (string.IsNullOrWhiteSpace(displayName))
                                        continue;

                                    // MSI и "системные" команды не чистим вообще
                                    if (IsMsiUninstall(uninstallStr, app))
                                        continue;

                                    if (LooksLikeValidSystemUninstaller(uninstallStr))
                                        continue;

                                    // Консервативная логика:
                                    // считаем "битым" ТОЛЬКО если есть явный EXE-унсталлер и он отсутствует.
                                    var exe = ExtractExePathFromCommand(uninstallStr);
                                    exe = ExpandEnv(exe);

                                    bool hasExeCandidate = !string.IsNullOrWhiteSpace(exe)
                                                           && (exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                                                               || exe.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) >= 0);

                                    // Доп. попытка: DisplayIcon иногда содержит путь к uninstall.exe/приложению
                                    if (!hasExeCandidate && !string.IsNullOrWhiteSpace(displayIcon))
                                    {
                                        var di = ExtractExePathFromCommand(displayIcon);
                                        di = ExpandEnv(di);
                                        if (!string.IsNullOrWhiteSpace(di) && di.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            exe = di;
                                            hasExeCandidate = true;
                                        }
                                    }

                                    if (!hasExeCandidate)
                                        continue;

                                    // Нормализуем: если в строке "C:\...\unins000.exe,0"
                                    exe = exe?.Split(',').FirstOrDefault()?.Trim();
                                    if (string.IsNullOrWhiteSpace(exe)) continue;

                                    // Если путь не абсолютный — не трогаем
                                    if (!Path.IsPathRooted(exe))
                                        continue;

                                    bool exeMissing = !File.Exists(exe);

                                    // InstallLocation НЕ используем как “единственный” критерий (много ложных),
                                    // но можем добавить в details
                                    installLoc = ExpandEnv(installLoc);

                                    if (!exeMissing)
                                        continue;

                                    var hiveStr = hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU";
                                    var skLocal = sk;

                                    AddIssue(
                                        "Uninstall Entries",
                                        hive,
                                        view,
                                        $@"{hiveStr}\{sub}\{skLocal}",
                                        "",
                                        $"Битая запись деинсталляции: отсутствует {exe}",
                                        dryRun =>
                                        {
                                            try
                                            {
                                                // перепроверка в момент фикса
                                                if (!IsUninstallEntryBroken(hive, view, skLocal))
                                                    return RegistryFixResult.Fixed;

                                                if (!dryRun)
                                                {
                                                    using (var wbase = OpenBase(hive, view))
                                                    using (var w = wbase.OpenSubKey(sub, writable: true))
                                                    {
                                                        if (w == null) return RegistryFixResult.Failed;
                                                        w.DeleteSubKeyTree(skLocal, throwOnMissingSubKey: false);
                                                    }
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
        }

        private bool IsUninstallEntryBroken(RegistryHive hive, RegistryView view, string subKeyName)
        {
            const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            try
            {
                using (var baseKey = OpenBase(hive, view))
                using (var key = baseKey.OpenSubKey(sub))
                using (var app = key?.OpenSubKey(subKeyName))
                {
                    if (app == null) return false;

                    var displayName = app.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(displayName)) return false;

                    var uninstallStr = app.GetValue("UninstallString") as string;
                    if (IsMsiUninstall(uninstallStr, app)) return false;
                    if (LooksLikeValidSystemUninstaller(uninstallStr)) return false;

                    var exe = ExtractExePathFromCommand(uninstallStr);
                    exe = ExpandEnv(exe);
                    exe = exe?.Split(',').FirstOrDefault()?.Trim();

                    if (string.IsNullOrWhiteSpace(exe)) return false;
                    if (!Path.IsPathRooted(exe)) return false;

                    return !File.Exists(exe);
                }
            }
            catch
            {
                return false;
            }
        }

        // ---------------- Shared DLLs ----------------

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

                            var path = ExpandEnv(name);

                            // Консервативно: трогаем только абсолютные пути
                            if (!Path.IsPathRooted(path))
                                continue;

                            if (!File.Exists(path))
                            {
                                var valueNameLocal = name;

                                AddIssue(
                                    "Shared DLLs",
                                    RegistryHive.LocalMachine,
                                    view,
                                    $@"HKLM\{sub}",
                                    valueNameLocal,
                                    "Файл не существует: " + path,
                                    dryRun =>
                                    {
                                        try
                                        {
                                            // перепроверка в момент фикса
                                            if (!IsSharedDllMissing(view, valueNameLocal))
                                                return RegistryFixResult.Fixed;

                                            if (!dryRun)
                                            {
                                                using (var wbase = OpenBase(RegistryHive.LocalMachine, view))
                                                using (var w = wbase.OpenSubKey(sub, writable: true))
                                                {
                                                    if (w == null) return RegistryFixResult.Failed;
                                                    w.DeleteValue(valueNameLocal, throwOnMissingValue: false);
                                                }
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

        private bool IsSharedDllMissing(RegistryView view, string valueName)
        {
            const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs";
            try
            {
                using (var baseKey = OpenBase(RegistryHive.LocalMachine, view))
                using (var key = baseKey.OpenSubKey(sub))
                {
                    if (key == null) return false;

                    // valueName тут — имя значения, которое является путём
                    var path = ExpandEnv(valueName);
                    if (string.IsNullOrWhiteSpace(path)) return false;
                    if (!Path.IsPathRooted(path)) return false;

                    return !File.Exists(path);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
