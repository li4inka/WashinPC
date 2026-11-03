using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Engines.Registry;
using WashinPC.Engines.ContextMenu;
using WashinPC.Engines.Operations;

namespace WashinPC.Tests.Operations
{
    [TestClass]
    [DoNotParallelize]
    public class RegistrySafetyTests
    {
        [TestMethod] public void HelpFileRepairRevalidatesAndPreservesOtherValues()
        {
            var id = "WashinPC.Tests-" + Guid.NewGuid().ToString("N");
            var path = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + id;
            var directory = Path.Combine(Path.GetTempPath(), id); Directory.CreateDirectory(directory);
            var missing = Path.Combine(directory, "missing.chm");
            RegistryBackup.TestBackupRoot = Path.Combine(directory, "backups");
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(path))
                { key.SetValue("HelpFile", missing); key.SetValue("Marker", "preserve"); }
                var scanner = new RegistryScanner();
                var issue = scanner.ScanAll(selected: new System.Collections.Generic.HashSet<string> { "registry.cat.help_files" })
                    .First(x => x.Hive == RegistryHive.CurrentUser && x.KeyPath.EndsWith(id));
                Assert.IsTrue(issue.CanFix);
                File.WriteAllText(missing, "now present");
                var context = new RegistryFixContext { AllowMutation = true };
                Assert.AreEqual(RegistryFixResult.Skipped, issue.Fix(context)); Assert.AreEqual(0, context.Backups.Count);
                File.Delete(missing);
                Assert.AreEqual(RegistryFixResult.Fixed, issue.Fix(context)); Assert.AreEqual(1, context.Backups.Count);
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path))
                { Assert.IsNull(key.GetValue("HelpFile")); Assert.AreEqual("preserve", key.GetValue("Marker")); }
                Assert.AreEqual(RegistryFixResult.Skipped, issue.Fix(context));
            }
            finally
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(path, false);
                RegistryBackup.TestBackupRoot = null; Directory.Delete(directory, true);
            }
        }
        [TestMethod] public void BackupFailurePreventsMutationEvenWhenCheckboxDisabled()
        {
            bool called = false;
            var result = RegistryFixExecutor.ExecuteLegacyFix(new RegistryFixContext { AllowMutation = true, CreateBackup = false }, RegistryHive.CurrentUser, RegistryView.Default,
                @"HKCU\Software\fixture", dry => { called = true; return RegistryFixResult.Fixed; }, (h, v, p) => null);
            Assert.AreEqual(RegistryFixResult.Failed, result); Assert.IsFalse(called);
        }
        [TestMethod] public void RevalidationAfterBackupPreventsStaleMutation()
        {
            int checks = 0; bool called = false;
            var result = RegistryFixExecutor.ExecuteLegacyFix(new RegistryFixContext { AllowMutation = true }, RegistryHive.CurrentUser, RegistryView.Default,
                @"HKCU\Software\fixture", dry => { called = true; return RegistryFixResult.Fixed; }, (h, v, p) => "backup.reg", () => ++checks == 1);
            Assert.AreEqual(RegistryFixResult.Skipped, result); Assert.IsFalse(called);
        }
        [TestMethod] public void BackupPreservesTypesAndContextMenuCanBeReenabled()
        {
            var name = "WashinPC.Tests-" + Guid.NewGuid().ToString("N");
            var path = @"Software\Classes\*\shell\" + name;
            var directory = Path.Combine(Path.GetTempPath(), name);
            RegistryBackup.TestBackupRoot = directory;
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(path))
                {
                    key.SetValue("text", "Пример", RegistryValueKind.String);
                    key.SetValue("expand", "%TEMP%", RegistryValueKind.ExpandString);
                    key.SetValue("multi", new[] { "one", "two" }, RegistryValueKind.MultiString);
                    key.SetValue("qword", long.MaxValue, RegistryValueKind.QWord);
                    key.SetValue("binary", Enumerable.Range(0, 257).Select(x => (byte)x).ToArray(), RegistryValueKind.Binary);
                }
                var before = RegistryBackup.Capture(RegistryHive.CurrentUser, RegistryView.Default, path);
                var entry = new ContextMenuEntry { RelativeKeyPath = path, Kind = ContextMenuKind.ShellVerb, Scope = ContextMenuScope.CurrentUser };
                entry.Snapshot = ContextMenuApplier.Snapshot(entry);
                Directory.CreateDirectory(directory);
                var blockedDestination = Path.Combine(directory, "not-a-directory"); File.WriteAllText(blockedDestination, "fixture");
                RegistryBackup.TestBackupRoot = blockedDestination;
                Assert.AreEqual(0, ContextMenuApplier.Apply(new[] { entry }, MenuAction.Disable, CancellationToken.None).Applied);
                Assert.AreEqual(before, RegistryBackup.Capture(RegistryHive.CurrentUser, RegistryView.Default, path));
                RegistryBackup.TestBackupRoot = directory;
                var result = ContextMenuApplier.Apply(new[] { entry }, MenuAction.Disable, CancellationToken.None);
                Assert.AreEqual(1, result.Applied); Assert.AreEqual(3, result.Backups.Count);
                Assert.AreEqual(before, File.ReadAllText(result.Backups[0]));
                StringAssert.Contains(File.ReadAllText(result.Backups[1]), "\"LegacyDisable\"=-");
                StringAssert.Contains(before, "hex(7):"); StringAssert.Contains(before, "hex(b):"); StringAssert.Contains(before, "hex(2):");
                // Import only our isolated fixture, proving that the exported types round-trip through Windows.
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(path, true);
                using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "reg.exe"),
                    "import \"" + result.Backups[0] + "\" /reg:" + (Environment.Is64BitProcess ? "64" : "32"))
                    { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
                {
                    Assert.IsTrue(process.WaitForExit(10000)); Assert.AreEqual(0, process.ExitCode, process.StandardError.ReadToEnd());
                }
                Assert.AreEqual(before, RegistryBackup.Capture(RegistryHive.CurrentUser, RegistryView.Default, path));
                // Recreate the disabled state before testing Enable.
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path, true)) key.SetValue("LegacyDisable", "");
                entry.Snapshot = ContextMenuApplier.Snapshot(entry);
                Assert.AreEqual(1, ContextMenuApplier.Apply(new[] { entry }, MenuAction.Enable, CancellationToken.None).Applied);
                Assert.AreEqual(before, RegistryBackup.Capture(RegistryHive.CurrentUser, RegistryView.Default, path));
                // A change after scanning must preserve the key.
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path, true)) key.SetValue("new", "value");
                Assert.AreEqual(0, ContextMenuApplier.Apply(new[] { entry }, MenuAction.Delete, CancellationToken.None).Applied);
            }
            finally
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(path, false);
                RegistryBackup.TestBackupRoot = null;
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
