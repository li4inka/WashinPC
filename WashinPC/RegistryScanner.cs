using System;
using System.Collections.Generic;
using Microsoft.Win32;
using System.IO;

namespace WashinPC
{
    public class RegistryIssue
    {
        public string Category { get; set; }
        public string KeyPath { get; set; }
        public string ValueName { get; set; }
        public string Details { get; set; }
        public Action Fix { get; set; }
    }

    public class RegistryScanner
    {
        private List<RegistryIssue> _issues = new List<RegistryIssue>();

        public IList<RegistryIssue> ScanAll()
        {
            _issues.Clear();
            ScanMissingSharedDlls();
            ScanAppPaths();
            ScanUninstallEntries();
            ScanComInprocServers();
            return _issues.AsReadOnly();
        }

        private void AddIssue(string category, string keyPath, string valueName, string details, Action fix)
        {
            _issues.Add(new RegistryIssue
            {
                Category = category,
                KeyPath = keyPath,
                ValueName = valueName,
                Details = details,
                Fix = fix
            });
        }

        private void ScanMissingSharedDlls()
        {
            string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\SharedDLLs";
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;
                        foreach (var name in key.GetValueNames())
                        {
                            string path = name;
                            if (!File.Exists(path))
                            {
                                string kp = @"HKLM:\" + sub;
                                AddIssue("Missing Shared DLL", kp, name, "Файл не существует: " + path, () =>
                                {
                                    try { using (var w = baseKey.OpenSubKey(sub, true)) { if (w != null) w.DeleteValue(name, false); } } catch { }
                                });
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void ScanAppPaths()
        {
            string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (var key = baseKey.OpenSubKey(sub))
                    {
                        if (key == null) continue;
                        foreach (var sk in key.GetSubKeyNames())
                        {
                            using (var app = key.OpenSubKey(sk))
                            {
                                string exe = app.GetValue(null) as string;
                                if (!string.IsNullOrEmpty(exe) && !File.Exists(exe))
                                {
                                    string kp = @"HKLM:\" + sub + "\\" + sk;
                                    AddIssue("Application Paths", kp, "(Default)", "Путь не существует: " + exe, () =>
                                    {
                                        try { using (var w = key.OpenSubKey(sk, true)) { if (w != null) w.DeleteValue(null, false); } } catch { }
                                    });
                                }
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void ScanUninstallEntries()
        {
            string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            {
                foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
                {
                    try
                    {
                        using (var baseKey = RegistryKey.OpenBaseKey(hive, view))
                        using (var key = baseKey.OpenSubKey(sub))
                        {
                            if (key == null) continue;
                            foreach (var sk in key.GetSubKeyNames())
                            {
                                using (var app = key.OpenSubKey(sk))
                                {
                                    string display = app.GetValue("DisplayName") as string;
                                    string installLoc = app.GetValue("InstallLocation") as string;
                                    string uninstallStr = app.GetValue("UninstallString") as string;
                                    bool broken = false;
                                    if (!string.IsNullOrEmpty(installLoc) && !Directory.Exists(installLoc)) broken = true;
                                    if (!string.IsNullOrEmpty(uninstallStr))
                                    {
                                        string candidate = uninstallStr.Trim('"');
                                        if (candidate.Contains(".exe"))
                                        {
                                            int idx = candidate.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                                            candidate = candidate.Substring(0, idx + 4);
                                            if (!File.Exists(candidate)) broken = true;
                                        }
                                    }
                                    if (broken)
                                    {
                                        string kp = (hive == RegistryHive.LocalMachine ? "HKLM:\\" : "HKCU:\\") + sub + "\\" + sk;
                                        string dn = string.IsNullOrEmpty(display) ? sk : display;
                                        AddIssue(
                                            "Uninstall Entries",
                                            kp,
                                            "",
                                            "Битая запись деинсталляции: " + dn,
                                            () =>
                                            {
                                                try
                                                {
                                                    Utils.Log("Registry FIX (Uninstall): " + kp);
                                                    using (var w = key.OpenSubKey(sk, true))
                                                    {
                                                        if (w != null)
                                                            key.DeleteSubKeyTree(sk, false);
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Utils.Log("Registry FIX ERROR: " + ex);
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
        }

        private void ScanComInprocServers()
        {
            string sub = @"CLSID";
            try
            {
                using (var hkcr = Registry.ClassesRoot.OpenSubKey(sub))
                {
                    if (hkcr == null) return;
                    foreach (var cls in hkcr.GetSubKeyNames())
                    {
                        using (var clsid = hkcr.OpenSubKey(cls))
                        using (var inproc = clsid == null ? null : clsid.OpenSubKey("InprocServer32"))
                        {
                            if (inproc == null) continue;
                            string dll = inproc.GetValue(null) as string;
                            if (!string.IsNullOrEmpty(dll) && !File.Exists(dll))
                            {
                                string kp = @"HKCR:\CLSID\" + cls + "\\InprocServer32";
                                AddIssue("COM/ActiveX", kp, "(Default)", "Отсутствует DLL: " + dll, () =>
                                {
                                    try { using (var w = clsid.OpenSubKey("InprocServer32", true)) { if (w != null) w.DeleteValue(null, false); } } catch { }
                                });
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}