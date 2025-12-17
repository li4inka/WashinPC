using System;
using System.Collections.Generic;
using System.IO;
using WashinPC.Core.Utils;

namespace WashinPC.Engines.Cleaner
{
    public class GroupResult
    {
        public string LocalizationKey;
        public List<string> Files = new List<string>();
        public long TotalBytes;
    }

    public class CleaningRule
    {
        public string LocalizationKey;
        public Func<IEnumerable<string>> PathsProvider;
    }

    public class CleanerEngine
    {
        public bool AggressiveMode { get; set; }
        private readonly List<CleaningRule> _rules = new List<CleaningRule>();

        public CleanerEngine()
        {
            InitRules();
        }

        public void InitRules()
        {
            _rules.Clear();

            _rules.Add(new CleaningRule
            {
                LocalizationKey = "cleaner.user_temp",
                PathsProvider = () => new[]
                {
                    Utils.GetKnownFolderPath_XP_Compatible("Temp")
                }
            });

            _rules.Add(new CleaningRule
            {
                LocalizationKey = "cleaner.windows_temp",
                PathsProvider = () => new[]
                {
                    Environment.ExpandEnvironmentVariables("%WINDIR%\\Temp")
                }
            });

            _rules.Add(new CleaningRule
            {
                LocalizationKey = "cleaner.prefetch",
                PathsProvider = () => new[]
                {
                    Environment.ExpandEnvironmentVariables("%WINDIR%\\Prefetch")
                }
            });

            _rules.Add(new CleaningRule
            {
                LocalizationKey = "cleaner.recent",
                PathsProvider = () => new[]
                {
                    Utils.GetKnownFolderPath_XP_Compatible("Recent")
                }
            });
        }

        public List<GroupResult> ScanDetailed(out int totalFiles, out long totalBytes)
        {
            totalFiles = 0;
            totalBytes = 0;

            var result = new List<GroupResult>();

            foreach (var rule in _rules)
            {
                var gr = new GroupResult
                {
                    LocalizationKey = rule.LocalizationKey
                };

                foreach (var path in rule.PathsProvider())
                {
                    var p = Utils.ExpandPath(path);
                    if (!Directory.Exists(p)) continue;

                    string[] files;
                    try
                    {
                        files = Directory.GetFiles(p, "*", SearchOption.AllDirectories);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var f in files)
                    {
                        gr.Files.Add(f);
                        try { gr.TotalBytes += new FileInfo(f).Length; } catch { }
                    }
                }

                if (gr.Files.Count > 0)
                {
                    result.Add(gr);
                    totalFiles += gr.Files.Count;
                    totalBytes += gr.TotalBytes;
                }
            }

            return result;
        }

        public void Clean(out int deleted)
        {
            deleted = 0;
            // твоя существующая логика очистки (не трогаем)
        }
    }
}
