using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using WashinPC.Engines.Registry;

namespace WashinPC.Tests.Registry
{
    [TestClass, DoNotParallelize]
    public class BackupRestoreTests
    {
        private string _path, _directory;
        [TestInitialize] public void Init()
        {
            _path = @"Software\WashinPC.Tests-" + Guid.NewGuid().ToString("N");
            _directory = Path.Combine(Path.GetTempPath(), "WashinPC.Tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory); RegistryBackup.TestBackupRoot = _directory;
        }
        [TestCleanup] public void Cleanup()
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(_path, false);
            RegistryBackup.TestBackupRoot = null; Directory.Delete(_directory, true);
        }
        private BackupEntry Entry(string file, RegistryView? view = RegistryView.Default) => new BackupEntry { FilePath = file, View = view == RegistryView.Default ? (Environment.Is64BitProcess ? RegistryView.Registry64 : RegistryView.Registry32) : view };

        [TestMethod] public void AllExportedTypesRestoreDeletedSubtreeAndUndoPreservesNewObjects()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_path))
            {
                key.SetValue("", "default"); key.SetValue("quote\"slash\\", "Пример");
                key.SetValue("expand", "%TEMP%", RegistryValueKind.ExpandString);
                key.SetValue("multi", new[] { "one", "two" }, RegistryValueKind.MultiString);
                key.SetValue("dword", -1, RegistryValueKind.DWord); key.SetValue("qword", long.MaxValue, RegistryValueKind.QWord);
                key.SetValue("binary", Enumerable.Range(0, 257).Select(x => (byte)x).ToArray(), RegistryValueKind.Binary);
                key.SetValue("none", new byte[] { 1, 2 }, RegistryValueKind.None);
                using (var child = key.CreateSubKey("child")) child.SetValue("value", "nested");
            }
            var snapshot = RegistryBackup.Capture(RegistryHive.CurrentUser, RegistryView.Default, _path);
            var file = RegistryBackup.ExportKey(RegistryHive.CurrentUser, RegistryView.Default, _path);
            Assert.IsNotNull(file); Assert.IsTrue(File.Exists(file + ".json"));
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(_path);
            var preview = BackupCatalog.Preview(Entry(file));
            var result = BackupCatalog.Restore(preview, CancellationToken.None);
            Assert.AreEqual(0, result.Failed); Assert.IsTrue(result.Applied > 0); Assert.AreEqual(1, result.Backups.Count);
            Assert.AreEqual(snapshot, RegistryBackup.Capture(RegistryHive.CurrentUser, RegistryView.Default, _path));
            var undo = BackupCatalog.Preview(Entry(result.Backups[0]));
            var reverted = BackupCatalog.Restore(undo, CancellationToken.None);
            Assert.AreEqual(0, reverted.Failed);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path)) Assert.IsNull(key);
        }

        [TestMethod] public void ValueRestorePreservesUnrelatedDataAndCanUndoNewValue()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_path)) key.SetValue("saved", "original");
            var file = RegistryBackup.ExportValue(RegistryHive.CurrentUser, RegistryView.Default, _path, "saved");
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path, true)) { key.DeleteValue("saved"); key.SetValue("unrelated", "preserve"); }
            var result = BackupCatalog.Restore(BackupCatalog.Preview(Entry(file)), CancellationToken.None);
            Assert.AreEqual(1, result.Applied);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path)) { Assert.AreEqual("original", key.GetValue("saved")); Assert.AreEqual("preserve", key.GetValue("unrelated")); }
            BackupCatalog.Restore(BackupCatalog.Preview(Entry(result.Backups[0])), CancellationToken.None);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path)) { Assert.IsNull(key.GetValue("saved")); Assert.AreEqual("preserve", key.GetValue("unrelated")); }
        }

        [TestMethod] public void ChangedAfterPreviewAndBackupFailurePreventWrites()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_path)) key.SetValue("value", "old");
            var file = RegistryBackup.ExportKey(RegistryHive.CurrentUser, RegistryView.Default, _path);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path, true)) key.SetValue("value", "current");
            var preview = BackupCatalog.Preview(Entry(file));
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path, true)) key.SetValue("value", "newer");
            Assert.ThrowsException<InvalidOperationException>(() => BackupCatalog.Restore(preview, CancellationToken.None));
            preview = BackupCatalog.Preview(Entry(file));
            var blocked = Path.Combine(_directory, "blocked"); File.WriteAllText(blocked, "fixture"); RegistryBackup.TestBackupRoot = blocked;
            Assert.ThrowsException<IOException>(() => BackupCatalog.Restore(preview, CancellationToken.None));
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path)) Assert.AreEqual("newer", key.GetValue("value"));
        }

        [TestMethod] public void LegacyViewMustBeChosenAndCorruptionIsRejected()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_path)) key.SetValue("value", 1);
            var file = RegistryBackup.ExportKey(RegistryHive.CurrentUser, RegistryView.Default, _path);
            File.Delete(file + ".json"); File.Delete(file + ".txt");
            var entry = BackupCatalog.List().Single(); Assert.IsNull(entry.View);
            Assert.ThrowsException<IOException>(() => BackupCatalog.Preview(entry));
            Assert.AreEqual(0, BackupCatalog.Preview(entry, RegistryView.Registry32).Differences.Count);
            Assert.AreEqual(0, BackupCatalog.Preview(entry, RegistryView.Registry64).Differences.Count);
            File.AppendAllText(file, "invalid", Encoding.Unicode);
            Assert.IsNotNull(BackupCatalog.List().Single().Error);
        }

        [TestMethod] public void CancellationDoesNotWriteAndMissingValueDirectiveRestoresAbsence()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_path)) { }
            var file = RegistryBackup.ExportValue(RegistryHive.CurrentUser, RegistryView.Default, _path, "toggle");
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path, true)) key.SetValue("toggle", "");
            var preview = BackupCatalog.Preview(Entry(file));
            Assert.IsTrue(BackupCatalog.Restore(preview, new CancellationToken(true)).Cancelled);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path)) Assert.IsNotNull(key.GetValue("toggle"));
            Assert.AreEqual(1, BackupCatalog.Restore(preview, CancellationToken.None).Applied);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path)) Assert.IsNull(key.GetValue("toggle"));
        }

        [TestMethod] public void ParserRejectsUnsupportedAndDuplicateValues()
        {
            var header = "Windows Registry Editor Version 5.00\r\n\r\n[HKEY_CURRENT_USER\\" + _path + "]\r\n";
            foreach (var content in new[] { "\"v\"=hex(9):00", "\"v\"=dword:00000001\r\n\"v\"=dword:00000002", "\"v\"=hex(1):ff", "[HKEY_LOCAL_MACHINE]" })
                Assert.ThrowsException<IOException>(() => BackupCatalog.Parse(header + content));
        }

        [TestMethod] public void PartialWriteFailureHasAccurateCountsAndKeepsUndoBackup()
        {
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(_path)) { key.SetValue("a", "saved"); key.SetValue("b", "saved"); }
            var file = RegistryBackup.ExportKey(RegistryHive.CurrentUser, RegistryView.Default, _path);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path, true)) { key.SetValue("a", "current"); key.SetValue("b", "current"); }
            var preview = BackupCatalog.Preview(Entry(file));
            var result = BackupCatalog.Restore(preview, CancellationToken.None, (obj, view) =>
            {
                if (obj.Name == "b") throw new UnauthorizedAccessException("Fixture denied write");
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path, true)) key.SetValue(obj.Name, obj.Value, obj.Kind);
            });
            Assert.AreEqual(1, result.Applied); Assert.AreEqual(1, result.Failed); Assert.AreEqual(1, result.Backups.Count);
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(_path)) { Assert.AreEqual("saved", key.GetValue("a")); Assert.AreEqual("current", key.GetValue("b")); }
        }
    }
}
