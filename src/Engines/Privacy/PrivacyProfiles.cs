using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using WashinPC.Engines.Operations;

namespace WashinPC.Engines.Privacy
{
    public sealed partial class PrivacyEngine
    {
        internal static bool CandidateAvailable(FileCandidate file) => file.Browser == null ||
            (file.IsDatabase ? DatabaseAvailable(file.Path, file.Browser) : !BrowserRunning(file.Browser));

        internal static void ScanFirefox(FileScanPlan plan, string roamingRoot, string localRoot, CancellationToken token, OperationProgress progress = null, Func<string, bool> running = null)
        {
            if (!Directory.Exists(roamingRoot)) return;
            try
            {
                PathSafety.NoLinks(roamingRoot);
                var profiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var standard = Path.Combine(roamingRoot, "Profiles");
                if (Directory.Exists(standard)) foreach (var profile in Directory.GetDirectories(standard)) profiles.Add(Path.GetFullPath(profile));
                var ini = Path.Combine(roamingRoot, "profiles.ini");
                if (File.Exists(ini))
                {
                    string path = null; bool relative = true; bool profileSection = false;
                    Action add = () =>
                    {
                        if (!profileSection || string.IsNullOrEmpty(path)) return;
                        var full = Path.GetFullPath(relative ? Path.Combine(roamingRoot, path.Replace('/', '\\')) : path);
                        if (PathSafety.Under(full, roamingRoot)) profiles.Add(full);
                    };
                    foreach (var raw in File.ReadAllLines(ini).Concat(new[] { "[End]" }))
                    {
                        var line = raw.Trim();
                        if (line.StartsWith("[", StringComparison.Ordinal)) { add(); profileSection = line.StartsWith("[Profile", StringComparison.OrdinalIgnoreCase); path = null; relative = true; }
                        else if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase)) path = line.Substring(5);
                        else if (line.StartsWith("IsRelative=", StringComparison.OrdinalIgnoreCase)) relative = line.Substring(11) != "0";
                    }
                }
                foreach (var profile in profiles)
                {
                    if (token.IsCancellationRequested) break;
                    if (!File.Exists(Path.Combine(profile, "prefs.js"))) continue;
                    AddDatabase(plan, Path.Combine(profile, "places.sqlite"), profile, "firefox", "privacy.firefox.history", FileOperationKind.FirefoxHistory, running, token);
                    AddDatabase(plan, Path.Combine(profile, "cookies.sqlite"), profile, "firefox", "privacy.firefox.cookies", FileOperationKind.FirefoxCookies, running, token);
                    var relative = profile.Substring(roamingRoot.TrimEnd('\\').Length).TrimStart('\\');
                    ScanCache(plan, Path.Combine(localRoot, relative, "cache2"), "firefox", "privacy.firefox.cache", token, running);
                    ScanCache(plan, Path.Combine(profile, "cache2"), "firefox", "privacy.firefox.cache", token, running);
                    progress?.Report(plan.Files.Count);
                }
            }
            catch { Notice(plan, roamingRoot, "op.file_unavailable"); }
        }

        private static void AddDatabase(FileScanPlan plan, string path, string root, string process, string group, FileOperationKind kind, Func<string, bool> running, CancellationToken token)
        {
            if (token.IsCancellationRequested || !File.Exists(path) || plan.Files.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) return;
            if (!DatabaseAvailable(path, process, running)) { Notice(plan, path, "op.browser_busy"); return; }
            try
            {
                var records = BrowserDatabase.Count(path, kind);
                using (var file = SafeFile.Open(path, root, false))
                {
                    if (!file.IsSqlite()) throw new IOException();
                    if (records > 0) plan.Files.Add(new FileCandidate { Path = path, Root = root, Browser = process, Group = group, Kind = kind, IsDatabase = true,
                        Bytes = 0, Records = records, Fingerprint = file.Fingerprint });
                }
            }
            catch (InvalidOperationException) { Notice(plan, path, "privacy.unsupported_schema"); }
            catch { Notice(plan, path, "op.file_unavailable"); }
        }

        private static void ScanCache(FileScanPlan plan, string root, string process, string group, CancellationToken token, Func<string, bool> running)
        {
            if (!Directory.Exists(root)) return;
            if (running != null ? running(process) : BrowserRunning(process)) { Notice(plan, root, "op.browser_busy"); return; }
            foreach (var path in PathSafety.Walk(root, plan, token))
            {
                if (plan.Files.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase))) continue;
                try
                {
                    using (var file = SafeFile.Open(path, root, false))
                        plan.Files.Add(new FileCandidate { Path = path, Root = root, Browser = process, Group = group, Bytes = file.Length, Fingerprint = file.Fingerprint });
                }
                catch { Notice(plan, path, "op.file_unavailable"); }
            }
        }
        private static void Notice(FileScanPlan plan, string path, string reason) => plan.Notices.Add(new ItemResult { Path = path, Status = ItemStatus.Skipped, Reason = reason });
    }
}
