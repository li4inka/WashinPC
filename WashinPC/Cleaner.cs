using System;
using System.Collections.Generic;
using System.IO;

namespace WashinPC
{
    public class GroupResult
{
    public string GroupName;
    public List<string> Files = new List<string>();
    public long TotalBytes;
}

    public class CleaningRule
    {
        public string Name { get; set; }
        public Func<IEnumerable<string>> PathsProvider { get; set; }
        public bool IsDirectory { get; set; }
    }

    public class CleanerEngine
    {
        public List<CleaningRule> Rules = new List<CleaningRule>();
        public bool AggressiveMode { get; set; } = false;

        public CleanerEngine()
        {
            InitRules();
        }

        public void InitRules()
        {
            Rules.Clear();

            // System temp folders
            Rules.Add(new CleaningRule
            {
                Name = "Пользовательский TEMP",
                IsDirectory = true,
                PathsProvider = () => new[] { Utils.GetKnownFolderPath_XP_Compatible("Temp") }
            });

            Rules.Add(new CleaningRule
            {
                Name = "Windows Temp",
                IsDirectory = true,
                PathsProvider = () => new[] { Environment.ExpandEnvironmentVariables("%WINDIR%\\Temp") }
            });

            Rules.Add(new CleaningRule
            {
                Name = "Префетч (Prefetch)",
                IsDirectory = true,
                PathsProvider = () => new[] { Environment.ExpandEnvironmentVariables("%WINDIR%\\Prefetch") }
            });

            // Recent
            Rules.Add(new CleaningRule
            {
                Name = "Список последних документов (Recent)",
                IsDirectory = true,
                PathsProvider = () => new[] { Utils.GetKnownFolderPath_XP_Compatible("Recent") }
            });

            // Internet Explorer (legacy) cache locations
            Rules.Add(new CleaningRule
            {
                Name = "Internet Explorer кэш",
                IsDirectory = true,
                PathsProvider = () => new[] {
                    Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Microsoft\\Windows\\INetCache"),
                    Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Local Settings\\Temporary Internet Files")
                }
            });

            // Edge (Chromium)
            Rules.Add(new CleaningRule
            {
                Name = "Edge (Chromium) Cache",
                IsDirectory = true,
                PathsProvider = () => new[] {
                    Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\Cache")
                }
            });

            // Chrome
            Rules.Add(new CleaningRule
            {
                Name = "Google Chrome Cache",
                IsDirectory = true,
                PathsProvider = () => new[] {
                    Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\Cache")
                }
            });

            // Firefox
            Rules.Add(new CleaningRule
            {
                Name = "Mozilla Firefox cache2",
                IsDirectory = true,
                PathsProvider = () => FirefoxCachePaths()
            });

            // Opera
            Rules.Add(new CleaningRule
            {
                Name = "Opera Cache",
                IsDirectory = true,
                PathsProvider = () => new[] {
                    Environment.ExpandEnvironmentVariables("%APPDATA%\\Opera Software\\Opera Stable\\Cache"),
                    Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Opera Software\\Opera Stable\\Cache")
                }
            });

            if (AggressiveMode)
            {
                // Deep Prefetch cleanup
                Rules.Add(new CleaningRule
                {
                    Name = "Префетч (Глубокая очистка) / Prefetch Deep",
                    IsDirectory = true,
                    PathsProvider = () => new[] { Environment.ExpandEnvironmentVariables("%WINDIR%\\Prefetch") }
                });

                // Thumbnail cache (Vista+)
                Rules.Add(new CleaningRule
                {
                    Name = "Thumbnail/Explorer Cache (DB)",
                    IsDirectory = true,
                    PathsProvider = () => new[] {
                        Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\Microsoft\\Windows\\Explorer")
                    }
                });

                // Thumbs.db sweep (XP/general)
                Rules.Add(new CleaningRule
                {
                    Name = "Thumbs.db (глобальная очистка)",
                    IsDirectory = false,
                    PathsProvider = () =>
                    {
                        var list = new List<string>();
                        try
                        {
                            var root = Environment.ExpandEnvironmentVariables("%USERPROFILE%");
                            if (Directory.Exists(root))
                            {
                                foreach (var f in Directory.GetFiles(root, "thumbs.db", SearchOption.AllDirectories))
                                    list.Add(f);
                            }
                        } catch {}
                        return list;
                    }
                });
            }
        }

        private IEnumerable<string> FirefoxCachePaths()
        {
            var appData = Utils.GetKnownFolderPath_XP_Compatible("AppData");
            var profilesIni = Path.Combine(appData, "Mozilla", "Firefox", "profiles.ini");
            if (File.Exists(profilesIni))
            {
                foreach (var line in Utils.SplitLines(File.ReadAllText(profilesIni)))
                {
                    if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                    {
                        var rel = line.Substring(5).Trim();
                        string prof = rel;
                        if (!Path.IsPathRooted(prof))
                        {
                            prof = Path.Combine(appData, "Mozilla", "Firefox", rel);
                        }
                        yield return Path.Combine(prof, "cache2");
                    }
                }
            }
            else
            {
                var profRoot = Path.Combine(appData, "Mozilla", "Firefox", "Profiles");
                if (Directory.Exists(profRoot))
                {
                    foreach (var d in Directory.GetDirectories(profRoot))
                        yield return Path.Combine(d, "cache2");
                }
            }
        }

        private IEnumerable<string> EnumerateRuleFiles(CleaningRule rule)
{
    foreach (var p in rule.PathsProvider())
    {
        var path = Utils.ExpandPath(p);
        if (rule.IsDirectory)
        {
            if (Directory.Exists(path))
            {
                string[] files = new string[0];
                try { files = Directory.GetFiles(path, "*", SearchOption.AllDirectories); } catch { }
                foreach (var f in files) yield return f;
            }
        }
        else
        {
            foreach (var f in Utils.ExpandWildcards(path))
                if (File.Exists(f)) yield return f;
            if (File.Exists(path)) yield return path;
        }
    }
}

public List<GroupResult> ScanDetailed(out int totalFiles, out long totalBytes)
{
    var results = new List<GroupResult>();
    totalFiles = 0;
    totalBytes = 0;
    foreach (var rule in Rules)
    {
        var gr = new GroupResult { GroupName = rule.Name };
        foreach (var f in EnumerateRuleFiles(rule))
        {
            gr.Files.Add(f);
            try { var fi = new FileInfo(f); gr.TotalBytes += fi.Length; } catch { }
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

        public List<string> Scan(out int candidates)
        {
            var log = new List<string>();
            int c = 0;
            foreach (var rule in Rules)
            {
                try
                {
                    foreach (var p in rule.PathsProvider())
                    {
                        var path = Utils.ExpandPath(p);
                        if (rule.IsDirectory)
                        {
                            if (Directory.Exists(path))
                            {
                                int files = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Length;
                                int dirs = Directory.GetDirectories(path, "*", SearchOption.AllDirectories).Length;
                                log.Add(rule.Name + " => найдено: файлов " + files + ", папок " + dirs + " в " + path);
                                c += files + dirs;
                            }
                        }
                        else
                        {
                            foreach (var f in Utils.ExpandWildcards(path))
                            {
                                if (File.Exists(f)) { log.Add(rule.Name + " файл: " + f); c++; }
                            }
                            // Also allow direct file enumeration from PathsProvider (thumbs.db sweep)
                            if (File.Exists(path) == false && Directory.Exists(path) == false && !path.Contains("*") && !path.Contains("?"))
                            {
                                // skip
                            }
                        }
                    }
                }
                catch (Exception ex) { log.Add(rule.Name + " ошибка сканирования: " + ex.Message); }
            }
            candidates = c;
            return log;
        }

        public List<string> Clean(out int deleted)
        {
            var log = new List<string>();
            int count = 0;
            foreach (var rule in Rules)
            {
                try
                {
                    foreach (var p in rule.PathsProvider())
                    {
                        var path = Utils.ExpandPath(p);
                        if (rule.IsDirectory)
                        {
                            count += Utils.SafeDeleteDirectoryContents(path, log);
                        }
                        else
                        {
                            bool any = false;
                            foreach (var f in Utils.ExpandWildcards(path))
                            {
                                any = true;
                                if (Utils.SafeDeleteFile(f, log)) count++;
                            }
                            if (!any && File.Exists(path))
                            {
                                if (Utils.SafeDeleteFile(path, log)) count++;
                            }
                        }
                    }
                }
                catch (Exception ex) { log.Add(rule.Name + " ошибка очистки: " + ex.Message); }
            }
            deleted = count;
            return log;
        }
    }
}