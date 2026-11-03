using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using WashinPC.Engines.Registry;

namespace WashinPC.Tests.Registry
{
    [TestClass]
    public class RegistryFixExecutorTests
    {
        [TestMethod]
        public void MissingObjectsAreSkippedAndSuccessfulWritesAreVerified()
        {
            var path = @"Software\WashinPC.Tests-" + System.Guid.NewGuid().ToString("N");
            try
            {
                Assert.AreEqual(RegistryFixResult.Skipped, RegistryMutations.DeleteKey(RegistryHive.CurrentUser, RegistryView.Default, path));
                Assert.AreEqual(RegistryFixResult.Skipped, RegistryMutations.DeleteValue(RegistryHive.CurrentUser, RegistryView.Default, path, "value"));
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(path)) key.SetValue("value", "original");
                Assert.AreEqual(RegistryFixResult.Skipped, RegistryMutations.DeleteValue(RegistryHive.CurrentUser, RegistryView.Default, path, "absent"));
                Assert.AreEqual(RegistryFixResult.Skipped, RegistryMutations.SetValue(RegistryHive.CurrentUser, RegistryView.Default, path, "value", "original", RegistryValueKind.String));
                Assert.AreEqual(RegistryFixResult.Fixed, RegistryMutations.SetValue(RegistryHive.CurrentUser, RegistryView.Default, path, "value", "updated", RegistryValueKind.String));
                Assert.AreEqual(RegistryFixResult.Fixed, RegistryMutations.DeleteValue(RegistryHive.CurrentUser, RegistryView.Default, path, "value"));
                Assert.AreEqual(RegistryFixResult.Skipped, RegistryMutations.DeleteValue(RegistryHive.CurrentUser, RegistryView.Default, path, "value"));
                Assert.AreEqual(RegistryFixResult.Fixed, RegistryMutations.DeleteKey(RegistryHive.CurrentUser, RegistryView.Default, path));
                Assert.AreEqual(RegistryFixResult.Skipped, RegistryMutations.DeleteKey(RegistryHive.CurrentUser, RegistryView.Default, path));
            }
            finally { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(path, false); }
        }

        [TestMethod]
        public void ObjectDisappearingInsideWriterIsNotReportedAsFixed()
        {
            var path = @"Software\WashinPC.Tests-" + System.Guid.NewGuid().ToString("N");
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(path)) { }
            try
            {
                var result = RegistryFixExecutor.ExecuteLegacyFix(new RegistryFixContext { AllowMutation = true }, RegistryHive.CurrentUser, RegistryView.Default,
                    path, dry => { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(path, true); throw new System.IO.IOException("Gone during write"); }, (h, v, p) => "fixture.reg");
                Assert.AreEqual(RegistryFixResult.Skipped, result);
            }
            finally { Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(path, false); }
        }
        [TestMethod]
        public void ExecuteLegacyFix_ReturnsSkipped_WhenMutationDisabled()
        {
            var ctx = new RegistryFixContext
            {
                AllowMutation = false,
                CreateBackup = true
            };

            var called = false;
            var result = RegistryFixExecutor.ExecuteLegacyFix(
                ctx,
                RegistryHive.CurrentUser,
                RegistryView.Registry64,
                @"HKCU\Software\Test",
                dryRun =>
                {
                    called = true;
                    return dryRun ? RegistryFixResult.Skipped : RegistryFixResult.Fixed;
                });

            Assert.AreEqual(RegistryFixResult.Skipped, result);
            Assert.IsFalse(called, "Legacy fix delegate should not be called when mutation is disabled.");
            Assert.AreEqual(0, ctx.Backups.Count);
        }

        [TestMethod]
        public void ExecuteLegacyFix_CreatesBackup_AndTreatsNoneAsSkipped()
        {
            var ctx = new RegistryFixContext
            {
                AllowMutation = true,
                CreateBackup = true
            };

            var gotDryRun = true;
            var result = RegistryFixExecutor.ExecuteLegacyFix(
                ctx,
                RegistryHive.CurrentUser,
                RegistryView.Registry64,
                @"HKCU\Software\Test",
                dryRun =>
                {
                    gotDryRun = dryRun;
                    return RegistryFixResult.None;
                },
                (hive, view, subKey) =>
                {
                    Assert.AreEqual(RegistryHive.CurrentUser, hive);
                    Assert.AreEqual(RegistryView.Registry64, view);
                    Assert.AreEqual(@"Software\Test", subKey);
                    return @"C:\backup.reg";
                });

            Assert.IsFalse(gotDryRun, "Executor should run fix in mutation mode with dryRun=false.");
            Assert.AreEqual(RegistryFixResult.Skipped, result);
            Assert.AreEqual(1, ctx.Backups.Count);
            Assert.AreEqual(@"C:\backup.reg", ctx.Backups[0]);
            StringAssert.Contains(ctx.Logs[0], "Backup:");
        }

        [TestMethod]
        public void ExecuteLegacyFix_ReturnsFailed_WhenContextOrFixIsNull()
        {
            var ctx = new RegistryFixContext { AllowMutation = true };

            var noContext = RegistryFixExecutor.ExecuteLegacyFix(
                null,
                RegistryHive.CurrentUser,
                RegistryView.Registry64,
                @"HKCU\Software\Test",
                dryRun => RegistryFixResult.Fixed);

            var noFix = RegistryFixExecutor.ExecuteLegacyFix(
                ctx,
                RegistryHive.CurrentUser,
                RegistryView.Registry64,
                @"HKCU\Software\Test",
                null);

            Assert.AreEqual(RegistryFixResult.Failed, noContext);
            Assert.AreEqual(RegistryFixResult.Failed, noFix);
        }

        [TestMethod]
        public void ExtractSubKeyPath_StripsHivePrefix()
        {
            var hkcu = RegistryFixExecutor.ExtractSubKeyPath(
                RegistryHive.CurrentUser,
                @"HKCU\Software\Classes\*\shell");
            var hklm = RegistryFixExecutor.ExtractSubKeyPath(
                RegistryHive.LocalMachine,
                @"HKLM\Software\Test");
            var noPrefix = RegistryFixExecutor.ExtractSubKeyPath(
                RegistryHive.LocalMachine,
                @"Software\NoPrefix");

            Assert.AreEqual(@"Software\Classes\*\shell", hkcu);
            Assert.AreEqual(@"Software\Test", hklm);
            Assert.AreEqual(@"Software\NoPrefix", noPrefix);
        }
    }
}
