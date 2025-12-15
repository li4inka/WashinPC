using System;
using System.Collections.Generic;
using System.IO;
using WashinPC.Engines.Cleaner;

namespace WashinPC.Engines.Privacy
{
    public class PrivacyRule
    {
        public string Name { get; set; }
        public Func<IEnumerable<string>> FilesProvider { get; set; }
    }

    public class PrivacyEngine
    {
        public List<PrivacyRule> Rules = new List<PrivacyRule>();

        public PrivacyEngine()
        {
            InitRules();
        }

        public void InitRules()
        {
            Rules.Clear();
            // Chrome
            Rules.Add(new PrivacyRule {
                Name = "Google Chrome — History & Cookies",
                FilesProvider = () => new[] {
                    Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\History"),
                    Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\Cookies")
                }
            });
            // Edge (Chromium)
            Rules.Add(new PrivacyRule {
                Name = "Microsoft Edge — History & Cookies",
                FilesProvider = () => new[] {
                    Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\History"),
                    Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\Cookies")
                }
            });
            // Firefox (places.sqlite for history; cookies.sqlite for cookies)
            Rules.Add(new PrivacyRule {
                Name = "Mozilla Firefox — History & Cookies",
                FilesProvider = () => FirefoxPrivacyFiles()
            });
            // Internet Explorer
            Rules.Add(new PrivacyRule {
                Name = "Internet Explorer — History & Cookies",
                FilesProvider = () => new[] {
                    Environment.ExpandEnvironmentVariables("%USERPROFILE%\\AppData\\Local\\Microsoft\\Windows\\History"),
                    Environment.ExpandEnvironmentVariables("%USERPROFILE%\\AppData\\Local\\Microsoft\\Windows\\INetCache\\IE"),
                    Environment.ExpandEnvironmentVariables("%USERPROFILE%\\AppData\\Roaming\\Microsoft\\Windows\\Cookies"),
                    Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Local Settings\\History"),
                    Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Local Settings\\Temporary Internet Files"),
                    Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Cookies")
                }
            });
            // Opera
            Rules.Add(new PrivacyRule {
                Name = "Opera — History & Cookies",
                FilesProvider = () => new[] {
                    Environment.ExpandEnvironmentVariables("%APPDATA%\\Opera Software\\Opera Stable\\History"),
                    Environment.ExpandEnvironmentVariables("%APPDATA%\\Opera Software\\Opera Stable\\Cookies")
                }
            });
        }

        private IEnumerable<string> FirefoxPrivacyFiles()
        {
            var appData = Utils.GetKnownFolderPath_XP_Compatible("AppData");
            var profilesRoot = Path.Combine(appData, "Mozilla", "Firefox");
            var profilesIni = Path.Combine(profilesRoot, "profiles.ini");
            var list = new List<string>();
            try
            {
                if (File.Exists(profilesIni))
                {
                    foreach (var line in Utils.SplitLines(File.ReadAllText(profilesIni)))
                    {
                        if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                        {
                            var rel = line.Substring(5).Trim();
                            string prof = rel;
                            if (!Path.IsPathRooted(prof)) prof = Path.Combine(profilesRoot, rel);
                            list.Add(Path.Combine(prof, "places.sqlite"));
                            list.Add(Path.Combine(prof, "cookies.sqlite"));
                        }
                    }
                }
                else
                {
                    var profRoot = Path.Combine(profilesRoot, "Profiles");
                    if (Directory.Exists(profRoot))
                    {
                        foreach (var d in Directory.GetDirectories(profRoot))
                        {
                            list.Add(Path.Combine(d, "places.sqlite"));
                            list.Add(Path.Combine(d, "cookies.sqlite"));
                        }
                    }
                }
            } catch {}
            return list;
        }

        public List<GroupResult> Scan(out int totalFiles, out long totalBytes)
        {
            var results = new List<GroupResult>();
            totalFiles = 0;
            totalBytes = 0;
            foreach (var rule in Rules)
            {
                var gr = new GroupResult { GroupName = rule.Name };
                foreach (var p in rule.FilesProvider())
                {
                    string path = Utils.ExpandPath(p);
                    if (Directory.Exists(path))
                    {
                        string[] files = new string[0];
                        try { files = Directory.GetFiles(path, "*", SearchOption.AllDirectories); } catch {}
                        foreach (var f in files)
                        {
                            gr.Files.Add(f);
                            try { gr.TotalBytes += new FileInfo(f).Length; } catch {}
                        }
                    }
                    else if (File.Exists(path))
                    {
                        gr.Files.Add(path);
                        try { gr.TotalBytes += new FileInfo(path).Length; } catch {}
                    }
                }
                if (gr.Files.Count > 0)
                {
                    results.Add(gr);
                    totalFiles += gr.Files.Count;
                    totalBytes += gr.TotalBytes;
                }
            }
            return results;
        }

        public int CleanSelected(IEnumerable<string> files, IList<string> log)
        {
            int deleted = 0;
            foreach (var f in files)
            {
                if (Utils.SafeDeleteFile(f, log)) deleted++;
            }
            return deleted;
        }
    }
}