using System.Collections.Generic;

namespace WashinPC.Engines.Registry
{
    public sealed class RegistryFixContext
    {
        public bool HasRestorePoint { get; set; }
        public bool AllowMutation { get; set; }
        public bool CreateBackup { get; set; }

        public List<string> Backups { get; } = new List<string>();
        public List<string> Logs { get; } = new List<string>();
    }

    public sealed class RegistryFixSessionResult
    {
        public int FixedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public int BackupCount { get; set; }
    }
}
