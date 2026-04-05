namespace WashinPC.Engines.ContextMenu
{
    public enum ContextMenuTarget
    {
        AnyFile,
        Folder,
        DirectoryBackground,
        DesktopBackground,
        Shortcut,
        Drive,
        Unknown
    }

    public enum ContextMenuKind
    {
        ShellVerb,     // ...\shell\<verb>
        ShellExHandler // ...\shellex\ContextMenuHandlers\<handler>
    }

    public enum ContextMenuScope
    {
        CurrentUser,   // HKCU\Software\Classes
        LocalMachine   // HKLM\Software\Classes
    }

    public sealed class ContextMenuEntry
    {
        public string Name { get; set; }          // Display-ish name (best effort)
        public ContextMenuTarget Target { get; set; }
        public ContextMenuKind Kind { get; set; }
        public ContextMenuScope Scope { get; set; }

        // Unique id used for disable/enable (for ShellEx it's usually CLSID; for verbs we use full key path)
        public string RegistryPath { get; set; }
        public string RelativeKeyPath { get; set; }
        public string BlockedId { get; set; }

        public string Source { get; set; }        // e.g. "HKCU" / "HKLM"
        public bool IsEnabled { get; set; }
    }
}
