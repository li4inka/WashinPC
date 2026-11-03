# Draft release: WashinPC 1.2.0-preview.1

The familiar WashinPC interface, with stricter checks before cleanup and mandatory registry backups.

## Downloads

- `WashinPC-1.2.0-preview.1-win-x64.zip`
- `WashinPC-1.2.0-preview.1-win-x86.zip`
- Matching `.sha256` files

Requires .NET Framework 4.8 and administrator rights. Extract the complete archive and launch `WashinPC.exe`. Packages are unsigned; no Authenticode certificate has been applied.

File deletion is permanent. The Backups button opens registry/context-menu backups; it cannot recover cleaned files. Follow the accompanying view-specific import instructions and compare current values before restoring.

Only missing zero-count SharedDLL entries and missing HelpFile values are automatically fixable in this preview. Other registry findings are diagnostic even in expert mode. Chrome and Edge must be closed for privacy cleanup.

See [CHANGELOG](../CHANGELOG.md) and [local validation and limitations](validation/README.md). Builds and automated rendering do not establish compatibility with every target Windows version or display scale.

This text is prepared locally. No public release or tag has been published.
