using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using WashinPC.Core.Logging;
using WashinPC.Engines.Operations;

namespace WashinPC.Engines.Privacy
{
    public class PrivacyGroupResult
    {
        public string LocalizationKey;
        public List<string> Files = new List<string>();
        public long TotalBytes;
    }
    public sealed class PrivacyEngine
    {
        internal FileScanPlan LastPlan { get; private set; } = new FileScanPlan();
        internal static bool DatabaseAvailable(string path, string process, Func<string, bool> running = null)
        {
            try
            {
                if (running != null ? running(process) : IsRunning(process)) return false;
                return new[] { "-wal", "-shm", "-journal" }.All(s => PathSafety.Probe(path + s) == PathState.Missing);
            }
            catch { return false; }
        }
        private static bool IsRunning(string name)
        {
            var processes = Process.GetProcessesByName(name);
            try { return processes.Length != 0; }
            finally { foreach (var process in processes) process.Dispose(); }
        }
        internal FileScanPlan ScanPlan(CancellationToken token, OperationProgress progress = null)
        {
            var plan = new FileScanPlan();
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            ScanBrowser(plan, Path.Combine(local, @"Google\Chrome\User Data"), "chrome", token, progress);
            ScanBrowser(plan, Path.Combine(local, @"Microsoft\Edge\User Data"), "msedge", token, progress);
            plan.Cancelled = token.IsCancellationRequested; LastPlan = plan;
            AppLogger.Info("privacy", "scan.done", "Candidates: " + plan.Files.Count + "; skipped: " + plan.Notices.Count);
            return plan;
        }
        internal static void ScanBrowser(FileScanPlan plan, string root, string process, CancellationToken token, OperationProgress progress = null, Func<string, bool> running = null)
        {
            if (PathSafety.Probe(root) == PathState.Missing) return;
            try
            {
                PathSafety.NoLinks(root);
                foreach (var profile in Directory.GetDirectories(root))
                {
                    if (token.IsCancellationRequested) break;
                    var name = Path.GetFileName(profile);
                    if (name != "Default" && !name.StartsWith("Profile ", StringComparison.Ordinal)) continue;
                    if (PathSafety.Probe(Path.Combine(profile, "Preferences")) != PathState.Present) continue;
                    foreach (var relative in new[] { "History", "Cookies", @"Network\Cookies" })
                    {
                        if (token.IsCancellationRequested) break;
                        var path = Path.Combine(profile, relative);
                        if (!File.Exists(path)) continue;
                        if (!DatabaseAvailable(path, process, running))
                        { plan.Notices.Add(new ItemResult { Path = path, Status = ItemStatus.Skipped, Reason = "op.browser_busy" }); continue; }
                        try
                        {
                            using (var file = SafeFile.Open(path, profile, false))
                            {
                                if (!file.IsSqlite()) throw new IOException();
                                plan.Files.Add(new FileCandidate { Path = path, Root = profile, Browser = process,
                                    Group = "privacy." + (process == "chrome" ? "chrome" : "edge") + (relative == "History" ? ".history" : ".cookies"),
                                    Bytes = file.Length, Fingerprint = file.Fingerprint });
                            }
                        }
                        catch (Exception ex)
                        {
                            plan.Notices.Add(new ItemResult { Path = path, Status = ItemStatus.Skipped, Reason = "op.file_unavailable" });
                            AppLogger.Warn("privacy", "scan.file.read.fail", "Database skipped. exceptionType=" + ex.GetType().Name);
                        }
                        progress?.Report(plan.Files.Count);
                    }
                }
            }
            catch { plan.Notices.Add(new ItemResult { Path = root, Status = ItemStatus.Skipped, Reason = "op.file_unavailable" }); }
        }
        public List<PrivacyGroupResult> Scan(out int totalFiles, out long totalBytes)
        {
            var plan = ScanPlan(CancellationToken.None);
            totalFiles = plan.Files.Count; totalBytes = plan.Files.Sum(x => x.Bytes);
            return plan.Files.GroupBy(x => x.Group).Select(g => new PrivacyGroupResult { LocalizationKey = g.Key, Files = g.Select(x => x.Path).ToList(), TotalBytes = g.Sum(x => x.Bytes) }).ToList();
        }
        public int CleanSelectedGroups(IEnumerable<string> groups)
            => FileOperations.Delete(LastPlan, groups ?? Enumerable.Empty<string>(), CancellationToken.None).Applied;
    }
}
