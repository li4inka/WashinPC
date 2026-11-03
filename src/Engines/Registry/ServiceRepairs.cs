using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.ServiceProcess;
using System.Threading;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using WashinPC.Engines.Operations;
using WashinPC.Localization;

namespace WashinPC.Engines.Registry
{
    [DataContract]
    internal sealed class ServiceConfiguration
    {
        [DataMember] public string Name;
        [DataMember] public string DisplayName;
        [DataMember] public string ImagePath;
        [DataMember] public string Account;
        [DataMember] public string Dependencies;
        [DataMember] public uint ServiceType;
        [DataMember] public uint StartType;
        [DataMember] public uint ErrorControl;
        [DataMember] public bool Stopped;
        internal string Snapshot => string.Join("\n", Name, DisplayName, ImagePath, Account, Dependencies, ServiceType, StartType, ErrorControl, Stopped);
        internal string Identity => string.Join("\n", Name, DisplayName, ImagePath, Account, Dependencies, ServiceType, ErrorControl);
    }

    internal interface IServiceManager
    {
        IEnumerable<string> Names();
        ServiceConfiguration Read(string name);
        void SetStartType(string name, uint startType);
    }

    internal sealed class WindowsServiceManager : IServiceManager
    {
        public IEnumerable<string> Names()
        {
            var services = ServiceController.GetServices();
            try { return services.Select(x => x.ServiceName).ToArray(); }
            finally { foreach (var service in services) service.Dispose(); }
        }
        public ServiceConfiguration Read(string name)
        {
            using (var manager = OpenSCManager(null, null, 1))
            using (var service = OpenService(manager, name, 1))
            {
                if (manager.IsInvalid || service.IsInvalid) throw new Win32Exception();
                uint needed; QueryServiceConfig(service, IntPtr.Zero, 0, out needed);
                if (needed == 0 || needed > 1024 * 1024) throw new Win32Exception();
                var buffer = Marshal.AllocHGlobal((int)needed);
                try
                {
                    if (!QueryServiceConfig(service, buffer, needed, out needed)) throw new Win32Exception();
                    var config = (NativeConfig)Marshal.PtrToStructure(buffer, typeof(NativeConfig));
                    var dependencies = new List<string>();
                    var pointer = config.Dependencies;
                    if (pointer != IntPtr.Zero)
                        for (var value = Marshal.PtrToStringUni(pointer); !string.IsNullOrEmpty(value); value = Marshal.PtrToStringUni(pointer))
                        { dependencies.Add(value); pointer = IntPtr.Add(pointer, (value.Length + 1) * 2); }
                    using (var controller = new ServiceController(name))
                        return new ServiceConfiguration { Name = name, DisplayName = Marshal.PtrToStringUni(config.DisplayName), ImagePath = Marshal.PtrToStringUni(config.BinaryPath),
                            Account = Marshal.PtrToStringUni(config.Account), Dependencies = string.Join("\0", dependencies), ServiceType = config.Type, StartType = config.Start,
                            ErrorControl = config.Error, Stopped = controller.Status == ServiceControllerStatus.Stopped };
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
        }
        public void SetStartType(string name, uint startType)
        {
            using (var manager = OpenSCManager(null, null, 1))
            using (var service = OpenService(manager, name, 2))
                if (manager.IsInvalid || service.IsInvalid || !ChangeServiceConfig(service, uint.MaxValue, startType, uint.MaxValue, null, null, IntPtr.Zero, null, null, null, null)) throw new Win32Exception();
        }
        [StructLayout(LayoutKind.Sequential)] private struct NativeConfig
        { internal uint Type, Start, Error; internal IntPtr BinaryPath, Group; internal uint Tag; internal IntPtr Dependencies, Account, DisplayName; }
        internal sealed class ServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
        { public ServiceHandle() : base(true) { } protected override bool ReleaseHandle() => CloseServiceHandle(handle); }
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ServiceHandle OpenSCManager(string machine, string database, uint access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern ServiceHandle OpenService(ServiceHandle manager, string name, uint access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryServiceConfig(ServiceHandle service, IntPtr config, uint size, out uint needed);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool ChangeServiceConfig(ServiceHandle service, uint type, uint start, uint error, string path, string group, IntPtr tag, string dependencies, string account, string password, string display);
        [DllImport("advapi32.dll")] private static extern bool CloseServiceHandle(IntPtr handle);
    }

    internal static class ServiceRepairs
    {
        internal static string Executable(string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return null;
            command = Environment.ExpandEnvironmentVariables(command.Trim());
            if (command.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase)) command = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + command.Substring(11);
            string path;
            if (command.StartsWith("\"", StringComparison.Ordinal))
            {
                var end = command.IndexOf('"', 1); if (end < 2) return null;
                path = command.Substring(1, end - 1);
            }
            else
            {
                var end = command.IndexOfAny(new[] { ' ', '\t' });
                path = end < 0 ? command : command.Substring(0, end);
            }
            if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(path).Equals("svchost.exe", StringComparison.OrdinalIgnoreCase)) return null;
            try { return PathSafety.Canonical(path); } catch { return null; }
        }

        internal static IEnumerable<RegistryIssue> Scan(IServiceManager manager, CancellationToken token)
        {
            foreach (var name in manager.Names())
            {
                token.ThrowIfCancellationRequested();
                ServiceConfiguration config;
                try { config = manager.Read(name); } catch { continue; }
                if (!config.Stopped || config.ServiceType != 0x10 || (config.StartType != 2 && config.StartType != 3)) continue;
                var executable = Executable(config.ImagePath);
                if (executable == null || PathSafety.Probe(executable) != PathState.Missing) continue;
                var issue = new RegistryIssue { CategoryKey = "registry.cat.services", DetailsKey = "registry.details.service_image_missing", DetailsArgs = new object[] { executable },
                    Hive = RegistryHive.LocalMachine, View = RegistryView.Default, KeyPath = @"HKLM\SYSTEM\CurrentControlSet\Services\" + name, ValueName = "Start", CanFix = true,
                    Fix = context => Disable(manager, config, executable, context) };
                issue.RefreshText(); yield return issue;
            }
        }

        internal static string SaveBackup(ServiceConfiguration config)
        {
            var file = Path.Combine(RegistryBackup.GetBackupRoot(), DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".service.json");
            ArtifactJson.Write(file, config);
            if (ArtifactJson.Read<ServiceConfiguration>(file).Snapshot != config.Snapshot) throw new IOException("Backup verification failed.");
            BackupCatalog.WriteMetadata(file, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32, new[] { config.Name });
            return file;
        }

        internal static ServiceConfiguration ReadBackup(string file)
        {
            var metadata = ArtifactJson.Read<BackupMetadata>(file + ".json");
            if (metadata.Version != 1 || metadata.Sha256 != ArtifactJson.Hash(file) || metadata.User != System.Security.Principal.WindowsIdentity.GetCurrent().User.Value) throw new IOException("Invalid service backup.");
            var config = ArtifactJson.Read<ServiceConfiguration>(file);
            if (string.IsNullOrWhiteSpace(config.Name) || config.Name.IndexOfAny(new[] { '\\', '/', '\0', '\r', '\n' }) >= 0 || config.ServiceType != 0x10 || config.StartType < 2 || config.StartType > 4) throw new IOException("Unsupported service configuration.");
            return config;
        }

        internal static RegistryFixResult Disable(IServiceManager manager, ServiceConfiguration original, string executable, RegistryFixContext context)
        {
            if (context == null || !context.AllowMutation) return RegistryFixResult.Skipped;
            if (manager.Read(original.Name).Snapshot != original.Snapshot || PathSafety.Probe(executable) != PathState.Missing) return RegistryFixResult.Skipped;
            context.Backups.Add(SaveBackup(original));
            if (manager.Read(original.Name).Snapshot != original.Snapshot || PathSafety.Probe(executable) != PathState.Missing) return RegistryFixResult.Skipped;
            manager.SetStartType(original.Name, 4);
            var after = manager.Read(original.Name);
            return after.StartType == 4 && after.Identity == original.Identity && after.Stopped ? RegistryFixResult.Fixed : RegistryFixResult.Failed;
        }

        internal static RestorePreview Preview(BackupEntry entry, IServiceManager manager = null)
        {
            manager = manager ?? new WindowsServiceManager();
            var target = ReadBackup(entry.FilePath); var current = manager.Read(target.Name);
            if (!current.Stopped || target.Identity != current.Identity) throw new InvalidOperationException("op.changed");
            var result = new RestorePreview { Entry = entry, Service = target, CurrentService = current, Hash = ArtifactJson.Hash(entry.FilePath) };
            if (target.StartType != current.StartType)
                result.Differences.Add(new RestoreDifference { Object = target.Name, Before = Localizer.T("service.start." + current.StartType), After = Localizer.T("service.start." + target.StartType) });
            return result;
        }

        internal static OperationResult Restore(RestorePreview preview, CancellationToken token, IServiceManager manager = null)
        {
            manager = manager ?? new WindowsServiceManager();
            var result = new OperationResult();
            if (token.IsCancellationRequested) { result.Cancelled = true; return result; }
            if (ArtifactJson.Hash(preview.Entry.FilePath) != preview.Hash || manager.Read(preview.Service.Name).Snapshot != preview.CurrentService.Snapshot) throw new InvalidOperationException("op.changed");
            if (preview.Differences.Count == 0) return result;
            result.Backups.Add(SaveBackup(preview.CurrentService));
            var item = new ItemResult { Path = preview.Service.Name }; result.Items.Add(item);
            try
            {
                if (token.IsCancellationRequested) { result.Cancelled = true; item.Status = ItemStatus.Cancelled; item.Reason = "op.cancelled"; return result; }
                if (manager.Read(preview.Service.Name).Snapshot != preview.CurrentService.Snapshot) throw new InvalidOperationException("op.changed");
                manager.SetStartType(preview.Service.Name, preview.Service.StartType);
                var after = manager.Read(preview.Service.Name);
                if (after.StartType != preview.Service.StartType || after.Identity != preview.Service.Identity || !after.Stopped) throw new IOException();
                item.Status = ItemStatus.Applied; item.Reason = "op.applied";
            }
            catch (InvalidOperationException) { item.Status = ItemStatus.Skipped; item.Reason = "op.changed"; }
            catch { item.Status = ItemStatus.Failed; item.Reason = "op.failed"; }
            return result;
        }
    }
}
