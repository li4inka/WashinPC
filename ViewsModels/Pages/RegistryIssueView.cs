using WashinPC.Engines.Registry;

namespace WashinPC.ViewModels.Pages

{
    public class RegistryIssueView
    {
        public bool IsSelected { get; set; } = true;

        public string Category { get; set; }
        public string KeyPath { get; set; }
        public string ValueName { get; set; }
        public string Details { get; set; }

        public RegistryIssue Source { get; set; }
    }
}
