using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Engines.Registry;

namespace WashinPC.Tests.Registry
{
    [TestClass, DoNotParallelize]
    public class ServiceRepairTests
    {
        private string _directory;
        [TestInitialize] public void Init()
        { _directory = Path.Combine(Path.GetTempPath(), "WashinPC.Tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_directory); RegistryBackup.TestBackupRoot = Path.Combine(_directory, "backups"); }
        [TestCleanup] public void Cleanup() { RegistryBackup.TestBackupRoot = null; Directory.Delete(_directory, true); }
        private sealed class Manager : IServiceManager
        {
            internal ServiceConfiguration Config;
            internal int Writes;
            public IEnumerable<string> Names() => new[] { Config.Name };
            public ServiceConfiguration Read(string name) => new ServiceConfiguration { Name = Config.Name, DisplayName = Config.DisplayName, ImagePath = Config.ImagePath, Account = Config.Account, Dependencies = Config.Dependencies, ServiceType = Config.ServiceType, StartType = Config.StartType, ErrorControl = Config.ErrorControl, Stopped = Config.Stopped };
            public void SetStartType(string name, uint type) { Config.StartType = type; Writes++; }
        }
        private Manager Fixture() => new Manager { Config = new ServiceConfiguration { Name = "WashinPC.Tests-fixture", DisplayName = "Fixture", ImagePath = "\"" + Path.Combine(_directory, "missing.exe") + "\" --test", Account = "LocalSystem", Dependencies = "", ServiceType = 0x10, StartType = 3, ErrorControl = 1, Stopped = true } };

        [TestMethod] public void DisableAndRestoreUseManagerAndVerifiedBackup()
        {
            var manager = Fixture(); var issue = ServiceRepairs.Scan(manager, CancellationToken.None).Single();
            var context = new RegistryFixContext { AllowMutation = true };
            Assert.AreEqual(RegistryFixResult.Fixed, issue.Fix(context)); Assert.AreEqual(4u, manager.Config.StartType); Assert.AreEqual(1, manager.Writes);
            Assert.AreEqual(1, context.Backups.Count); Assert.AreEqual(3u, ServiceRepairs.ReadBackup(context.Backups[0]).StartType);
            Assert.AreEqual(RegistryFixResult.Skipped, issue.Fix(context));
            var entry = new BackupEntry { FilePath = context.Backups[0] };
            var preview = ServiceRepairs.Preview(entry, manager);
            var result = ServiceRepairs.Restore(preview, CancellationToken.None, manager);
            Assert.AreEqual(1, result.Applied); Assert.AreEqual(3u, manager.Config.StartType); Assert.AreEqual(2, manager.Writes); Assert.AreEqual(1, result.Backups.Count);
        }

        [TestMethod] public void RunningSharedDriverAndAmbiguousServicesAreExcluded()
        {
            var manager = Fixture(); manager.Config.Stopped = false; Assert.AreEqual(0, ServiceRepairs.Scan(manager, CancellationToken.None).Count());
            manager = Fixture(); manager.Config.ServiceType = 0x20; Assert.AreEqual(0, ServiceRepairs.Scan(manager, CancellationToken.None).Count());
            manager.Config.ServiceType = 1; Assert.AreEqual(0, ServiceRepairs.Scan(manager, CancellationToken.None).Count());
            manager = Fixture(); manager.Config.ImagePath = @"C:\Program Files\missing.exe --test"; Assert.AreEqual(0, ServiceRepairs.Scan(manager, CancellationToken.None).Count());
            Assert.IsNull(ServiceRepairs.Executable(@"C:\Windows\System32\svchost.exe -k group"));
        }

        [TestMethod] public void ChangedConfigRestoredExecutableAndBackupFailureBlockDisable()
        {
            var manager = Fixture(); var issue = ServiceRepairs.Scan(manager, CancellationToken.None).Single();
            manager.Config.Account = "changed";
            Assert.AreEqual(RegistryFixResult.Skipped, issue.Fix(new RegistryFixContext { AllowMutation = true })); Assert.AreEqual(0, manager.Writes);
            manager = Fixture(); issue = ServiceRepairs.Scan(manager, CancellationToken.None).Single(); File.WriteAllText(Path.Combine(_directory, "missing.exe"), "now present");
            Assert.AreEqual(RegistryFixResult.Skipped, issue.Fix(new RegistryFixContext { AllowMutation = true })); File.Delete(Path.Combine(_directory, "missing.exe"));
            var blocked = Path.Combine(_directory, "blocked"); File.WriteAllText(blocked, "fixture"); RegistryBackup.TestBackupRoot = blocked;
            Assert.ThrowsException<IOException>(() => issue.Fix(new RegistryFixContext { AllowMutation = true })); Assert.AreEqual(0, manager.Writes);
        }

        [TestMethod] public void ChangedServiceAndCancellationPreventRestoration()
        {
            var manager = Fixture(); var file = ServiceRepairs.SaveBackup(manager.Read(manager.Config.Name)); manager.Config.StartType = 4;
            var preview = ServiceRepairs.Preview(new BackupEntry { FilePath = file }, manager);
            Assert.IsTrue(ServiceRepairs.Restore(preview, new CancellationToken(true), manager).Cancelled);
            manager.Config.ImagePath = @"C:\changed.exe";
            Assert.ThrowsException<InvalidOperationException>(() => ServiceRepairs.Restore(preview, CancellationToken.None, manager)); Assert.AreEqual(0, manager.Writes);
        }

        private static int Sc(string arguments)
        {
            using (var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "sc.exe"), arguments)
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
            { var output = process.StandardOutput.ReadToEnd(); process.WaitForExit(); return process.ExitCode; }
        }
        [TestMethod] public void NativeManagerReadsExistingConfigurationWithoutChangingServices()
        {
            var manager = new WindowsServiceManager();
            var name = manager.Names().First();
            var config = manager.Read(name);
            Assert.AreEqual(name, config.Name); Assert.IsFalse(string.IsNullOrWhiteSpace(config.ImagePath)); Assert.IsTrue(config.ServiceType > 0);
        }
        [TestMethod] public void NativeManagerDisablesAndRestoresOnlyDisposableFixtureService()
        {
            var name = "WashinPC.Tests-" + Guid.NewGuid().ToString("N");
            var binary = Path.Combine(_directory, "missing.exe");
            var code = Sc("create " + name + " type= own start= demand binPath= \"\\\"" + binary + "\\\"\"");
            if (code == 5) Assert.Inconclusive("Creating a disposable service requires an elevated test process.");
            Assert.AreEqual(0, code);
            try
            {
                var manager = new WindowsServiceManager(); var original = manager.Read(name);
                Assert.IsTrue(original.Stopped); Assert.AreEqual(3u, original.StartType);
                var context = new RegistryFixContext { AllowMutation = true };
                Assert.AreEqual(RegistryFixResult.Fixed, ServiceRepairs.Disable(manager, original, binary, context));
                Assert.AreEqual(4u, manager.Read(name).StartType);
                var preview = ServiceRepairs.Preview(new BackupEntry { FilePath = context.Backups.Single() }, manager);
                Assert.AreEqual(1, ServiceRepairs.Restore(preview, CancellationToken.None, manager).Applied);
                Assert.AreEqual(3u, manager.Read(name).StartType);
            }
            finally { Assert.AreEqual(0, Sc("delete " + name)); }
        }
    }
}
