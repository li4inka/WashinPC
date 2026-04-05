using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    if (!Directory.Exists(p))
                        continue;

                    string[] files;
                    try
                    {
                        files = Directory.GetFiles(p, "*", SearchOption.AllDirectories);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var file in files)
                    {
                        gr.Files.Add(file);
                        try
                        {
                            gr.TotalBytes += new FileInfo(file).Length;
                        }
                        catch
                        {
                            // Ignore file size errors.
                        }
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

        public int CleanSelectedGroups(IEnumerable<string> selectedGroupKeys)
        {
            var keySet = new HashSet<string>(
                selectedGroupKeys ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            if (keySet.Count == 0)
                return 0;

            int totalFiles;
            long totalBytes;
            var groups = ScanDetailed(out totalFiles, out totalBytes);

            var uniqueFiles = groups
                .Where(g => keySet.Contains(g.LocalizationKey))
                .SelectMany(g => g.Files)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            int deleted = 0;
            foreach (var file in uniqueFiles)
            {
                try
                {
                    if (!File.Exists(file))
                        continue;

                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    deleted++;
                }
                catch
                {
                    // Continue deleting other files.
                }
            }

            return deleted;
        }

        public void Clean(out int deleted)
        {
            deleted = CleanSelectedGroups(_rules.Select(r => r.LocalizationKey));
        }
    }
}
