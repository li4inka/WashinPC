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
    public sealed partial class PrivacyEngine
    {
        internal FileScanPlan LastPlan { get; private set; } = new FileScanPlan();
        internal static bool DatabaseAvailable(string path, string process, Func<string, bool> running = null)
        {
            try
            {
                if (running != null ? running(process) : BrowserRunning(process)) return false;
                return new[] { "-wal", "-shm", "-journal" }.All(s => PathSafety.Probe(path + s) == PathState.Missing);
            }
            catch { return false; }
        }
        internal static bool BrowserRunning(string name)
        {
            var processes = Process.GetProcessesByName(name);
            try { return processes.Length != 0; }
            finally { foreach (var process in processes) process.Dispose(); }
        }
        internal FileScanPlan ScanPlan(CancellationToken token, OperationProgress progress = null)
        {
            var plan = new FileScanPlan();
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ScanBrowser(plan, Path.Combine(local, @"Google\Chrome\User Data"), "chrome", token, progress);
            ScanBrowser(plan, Path.Combine(local, @"Microsoft\Edge\User Data"), "msedge", token, progress);
            ScanBrowser(plan, Path.Combine(local, @"BraveSoftware\Brave-Browser\User Data"), "brave", token, progress, browserId: "brave");
            ScanBrowser(plan, Path.Combine(local, @"Yandex\YandexBrowser\User Data"), "browser", token, progress, browserId: "yandex");
            ScanBrowser(plan, Path.Combine(roaming, @"Opera Software\Opera Stable"), "opera", token, progress, browserId: "opera", cacheRoot: Path.Combine(local, @"Opera Software\Opera Stable"));
            ScanBrowser(plan, Path.Combine(roaming, @"Opera Software\Opera GX Stable"), "opera", token, progress, browserId: "opera_gx", cacheRoot: Path.Combine(local, @"Opera Software\Opera GX Stable"));
            ScanFirefox(plan, Path.Combine(roaming, @"Mozilla\Firefox"), Path.Combine(local, @"Mozilla\Firefox"), token, progress);
            plan.Cancelled = token.IsCancellationRequested; LastPlan = plan;
            AppLogger.Info("privacy", "scan.done", "Candidates: " + plan.Files.Count + "; skipped: " + plan.Notices.Count);
            return plan;
        }
        internal static void ScanBrowser(FileScanPlan plan, string root, string process, CancellationToken token, OperationProgress progress = null, Func<string, bool> running = null, string browserId = null, string cacheRoot = null)
        {
            browserId = browserId ?? (process == "msedge" ? "edge" : process);
            if (!Directory.Exists(root)) return;
            try
            {
                PathSafety.NoLinks(root);
                var profiles = Directory.GetDirectories(root).Where(p => Path.GetFileName(p) == "Default" || Path.GetFileName(p).StartsWith("Profile ", StringComparison.Ordinal)).ToList();
                if (File.Exists(Path.Combine(root, "Preferences"))) profiles.Add(root);
                foreach (var profile in profiles)
                {
                    if (token.IsCancellationRequested) break;
                    if (PathSafety.Probe(Path.Combine(profile, "Preferences")) != PathState.Present) continue;
                    foreach (var relative in new[] { "History", "Cookies", @"Network\Cookies" })
                    {
                        if (token.IsCancellationRequested) break;
                        AddDatabase(plan, Path.Combine(profile, relative), profile, process, "privacy." + browserId + (relative == "History" ? ".history" : ".cookies"),
                            relative == "History" ? FileOperationKind.ChromiumHistory : FileOperationKind.ChromiumCookies, running, token);
                        progress?.Report(plan.Files.Count);
                    }
                    foreach (var relative in new[] { "Cache", "Code Cache", "GPUCache" })
                    {
                        ScanCache(plan, Path.Combine(profile, relative), process, "privacy." + browserId + ".cache", token, running);
                        if (cacheRoot != null)
                        {
                            var profileRelative = profile.Substring(root.TrimEnd('\\').Length).TrimStart('\\');
                            ScanCache(plan, Path.Combine(cacheRoot, profileRelative, relative), process, "privacy." + browserId + ".cache", token, running);
                        }
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
