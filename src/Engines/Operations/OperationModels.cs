using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WashinPC.Engines.Operations
{
    internal enum ItemStatus { Applied, Skipped, Failed, Cancelled }
    internal sealed class ItemResult
    {
        internal string Path, Reason;
        internal ItemStatus Status;
        internal long Bytes;
    }
    internal sealed class OperationResult
    {
        internal readonly List<ItemResult> Items = new List<ItemResult>();
        internal readonly List<string> Backups = new List<string>();
        internal bool Cancelled;
        internal int Applied => Items.Count(x => x.Status == ItemStatus.Applied);
        internal int Failed => Items.Count(x => x.Status == ItemStatus.Failed);
        internal int Skipped => Items.Count - Applied - Failed;
        internal long DeletedBytes => Items.Where(x => x.Status == ItemStatus.Applied).Sum(x => x.Bytes);
    }
    internal sealed class FileCandidate
    {
        internal string Path, Root, Group, Fingerprint, Browser;
        internal long Bytes;
    }
    internal sealed class FileScanPlan
    {
        internal readonly List<FileCandidate> Files = new List<FileCandidate>();
        internal readonly List<ItemResult> Notices = new List<ItemResult>();
        internal bool Cancelled;
    }
    internal sealed class OperationProgress
    {
        private readonly Action<int> _report;
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        internal OperationProgress(Action<int> report) { _report = report; }
        internal void Report(int count)
        {
            if (_clock.ElapsedMilliseconds < 250) return;
            _clock.Restart(); _report?.Invoke(count);
        }
    }
    internal static class FileOperations
    {
        internal static OperationResult Delete(FileScanPlan plan, IEnumerable<string> selected, CancellationToken token, OperationProgress progress = null)
        {
            var result = new OperationResult();
            var groups = new HashSet<string>(selected, StringComparer.Ordinal);
            foreach (var candidate in plan.Files.Where(x => groups.Contains(x.Group)).ToArray())
            {
                var item = new ItemResult { Path = candidate.Path }; result.Items.Add(item);
                if (token.IsCancellationRequested) { result.Cancelled = true; item.Status = ItemStatus.Cancelled; item.Reason = "op.cancelled"; continue; }
                try
                {
                    if (candidate.Browser != null && !Privacy.PrivacyEngine.DatabaseAvailable(candidate.Path, candidate.Browser))
                        throw new InvalidOperationException("op.browser_busy");
                    using (var lease = new DirectoryLease(System.IO.Path.GetDirectoryName(candidate.Path)))
                    using (var file = SafeFile.Open(candidate.Path, candidate.Root, true))
                    {
                        if (file.Fingerprint != candidate.Fingerprint) throw new InvalidOperationException("op.changed");
                        if (candidate.Browser != null && !Privacy.PrivacyEngine.DatabaseAvailable(candidate.Path, candidate.Browser))
                            throw new InvalidOperationException("op.browser_busy");
                        file.Delete();
                    }
                    item.Status = ItemStatus.Applied; item.Bytes = candidate.Bytes; item.Reason = "op.applied";
                    plan.Files.Remove(candidate);
                }
                catch (InvalidOperationException ex) { item.Status = ItemStatus.Skipped; item.Reason = ex.Message; }
                catch (Exception) { item.Status = ItemStatus.Failed; item.Reason = "op.file_unavailable"; }
                progress?.Report(result.Items.Count);
            }
            return result;
        }
    }
}
