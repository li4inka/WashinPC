using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Engines.Cleaner;
using WashinPC.Engines.Privacy;
using WashinPC.Engines.Operations;

namespace WashinPC.Tests.Operations
{
    [TestClass]
    public class FileSafetyTests
    {
        private string root;
        [TestInitialize] public void Init() { root = Path.Combine(Path.GetTempPath(), "WashinPC.Tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); }
        [TestCleanup] public void Cleanup() { Directory.Delete(root, true); }
        private string File(string name = "one.tmp", string text = "original") { var path = Path.Combine(root, name); System.IO.File.WriteAllText(path, text); return path; }
        private CleanerEngine Engine()
        {
            var engine = new CleanerEngine();
            var rules = (List<CleaningRule>)typeof(CleanerEngine).GetField("_rules", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(engine);
            rules.Clear(); rules.Add(new CleaningRule { LocalizationKey = "test", PathsProvider = () => new[] { root } }); return engine;
        }
        [TestMethod] public void NewFilesAfterScanAreNotDeleted()
        {
            var old = File(); var plan = Engine().ScanPlan(CancellationToken.None); var added = File("new.tmp");
            var result = FileOperations.Delete(plan, new[] { "test" }, CancellationToken.None);
            Assert.AreEqual(1, result.Applied); Assert.AreEqual(8L, result.DeletedBytes);
            Assert.IsFalse(System.IO.File.Exists(old)); Assert.IsTrue(System.IO.File.Exists(added));
        }
        [TestMethod] public void ChangedFileIsPreserved()
        {
            var path = File(); var plan = Engine().ScanPlan(CancellationToken.None); System.IO.File.WriteAllText(path, "new content");
            var result = FileOperations.Delete(plan, new[] { "test" }, CancellationToken.None);
            Assert.AreEqual(0, result.Applied); Assert.AreEqual("new content", System.IO.File.ReadAllText(path));
        }
        [TestMethod] public void PartialFailureCountsOnlyDeletedBytes()
        {
            var a = File(); File("two.tmp"); var plan = Engine().ScanPlan(CancellationToken.None);
            using (var held = new FileStream(a, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                var result = FileOperations.Delete(plan, new[] { "test" }, CancellationToken.None);
                Assert.AreEqual(1, result.Applied); Assert.AreEqual(1, result.Failed); Assert.AreEqual(8L, result.DeletedBytes);
            }
            Assert.AreEqual(1, FileOperations.Delete(plan, new[] { "test" }, CancellationToken.None).Applied);
        }
        [TestMethod] public void CancellationPreservesPendingFilesAndAllowsRetry()
        {
            var path = File(); var plan = Engine().ScanPlan(CancellationToken.None);
            var result = FileOperations.Delete(plan, new[] { "test" }, new CancellationToken(true));
            Assert.IsTrue(result.Cancelled); Assert.IsTrue(System.IO.File.Exists(path));
            Assert.AreEqual(1, FileOperations.Delete(plan, new[] { "test" }, CancellationToken.None).Applied);
        }
        [TestMethod] public void PathBoundariesAndUnknownPathsFailClosed()
        {
            Assert.IsFalse(PathSafety.Under(root + "Other\\file", root));
            Assert.IsFalse(PathSafety.Under(root, root));
            Assert.AreEqual(PathState.Missing, PathSafety.Probe(Path.Combine(root, "missing")));
            Assert.AreEqual(PathState.Unknown, PathSafety.Probe(Path.Combine(root, "unknown", "file")));
            Assert.AreEqual(PathState.Unknown, PathSafety.Probe(@"\\server\share\file"));
            Assert.AreEqual(PathState.Unknown, PathSafety.Probe("relative.exe"));
        }
        [TestMethod] public void AncestorCannotBeRenamedDuringDeletion()
        {
            var child = Path.Combine(root, "child"); Directory.CreateDirectory(child);
            using (var lease = new DirectoryLease(child)) Assert.ThrowsException<IOException>(() => Directory.Move(child, child + "-moved"));
        }
        [TestMethod] public void OpenFileCannotBeChangedOrReplaced()
        {
            var path = File(); using (var file = SafeFile.Open(path, root, true))
            {
                Assert.ThrowsException<IOException>(() => System.IO.File.WriteAllText(path, "other"));
                Assert.ThrowsException<IOException>(() => System.IO.File.Delete(path));
            }
        }
        [TestMethod] public void HardlinkedFilesAreNotCandidates()
        {
            var path = File(); Assert.IsTrue(CreateHardLink(Path.Combine(root, "link.tmp"), path, IntPtr.Zero));
            Assert.AreEqual(0, Engine().ScanPlan(CancellationToken.None).Files.Count);
        }
        [TestMethod] public void NamedStreamsArePreserved()
        {
            var path = File();
            using (var handle = SafeFile.CreateFile(path + ":private", 0x40000000, 0, IntPtr.Zero, 2, 0, IntPtr.Zero)) Assert.IsFalse(handle.IsInvalid);
            Assert.AreEqual(0, Engine().ScanPlan(CancellationToken.None).Files.Count);
        }
        [TestMethod] public void BrowserProfilesAndSidecarsAreHandledConservatively()
        {
            var profile = Path.Combine(root, "Profile 2"); Directory.CreateDirectory(profile);
            System.IO.File.WriteAllText(Path.Combine(profile, "Preferences"), "{}");
            var history = Path.Combine(profile, "History"); WashinPC.Tests.Privacy.BrowserFixtures.ChromiumHistory(history);
            var plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, root, "chrome", CancellationToken.None, running: p => false);
            Assert.AreEqual(1, plan.Files.Count); Assert.AreEqual("privacy.chrome.history", plan.Files[0].Group);
            System.IO.File.WriteAllText(history + "-wal", "recovery");
            plan = new FileScanPlan(); PrivacyEngine.ScanBrowser(plan, root, "chrome", CancellationToken.None, running: p => false);
            Assert.AreEqual(0, plan.Files.Count); Assert.AreEqual("op.browser_busy", plan.Notices.Single().Reason);
            System.IO.File.Delete(history + "-wal");
            Assert.IsFalse(PrivacyEngine.DatabaseAvailable(history, "chrome", p => true));
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool CreateHardLink(string link, string existing, IntPtr security);
    }
}
