using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using WashinPC.Engines.Operations;
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

        internal FileScanPlan LastPlan { get; private set; } = new FileScanPlan();

        internal FileScanPlan ScanPlan(CancellationToken token, OperationProgress progress = null)
        {
            var plan = new FileScanPlan();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in _rules)
            foreach (var root in rule.PathsProvider())
            {
                if (token.IsCancellationRequested) break;
                foreach (var path in PathSafety.Walk(root, plan, token))
                {
                    if (!seen.Add(path)) continue;
                    try
                    {
                        using (var file = SafeFile.Open(path, root, false))
                            plan.Files.Add(new FileCandidate { Path = path, Root = root, Group = rule.LocalizationKey, Fingerprint = file.Fingerprint, Bytes = file.Length });
                    }
                    catch { plan.Notices.Add(new ItemResult { Path = path, Status = ItemStatus.Skipped, Reason = "op.file_unavailable" }); }
                    progress?.Report(plan.Files.Count);
                }
            }
            plan.Cancelled = token.IsCancellationRequested;
            LastPlan = plan;
            return plan;
        }
        public List<GroupResult> ScanDetailed(out int totalFiles, out long totalBytes)
        {
            var plan = ScanPlan(CancellationToken.None);
            totalFiles = plan.Files.Count; totalBytes = plan.Files.Sum(x => x.Bytes);
            return plan.Files.GroupBy(x => x.Group).Select(g => new GroupResult { LocalizationKey = g.Key, Files = g.Select(x => x.Path).ToList(), TotalBytes = g.Sum(x => x.Bytes) }).ToList();
        }
        public int CleanSelectedGroups(IEnumerable<string> selectedGroupKeys)
            => FileOperations.Delete(LastPlan, selectedGroupKeys ?? Enumerable.Empty<string>(), CancellationToken.None).Applied;
        public void Clean(out int deleted) { deleted = CleanSelectedGroups(_rules.Select(x => x.LocalizationKey)); }
    }
}
