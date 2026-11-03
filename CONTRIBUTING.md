# Contributing to WashinPC

Bug reports, translations and focused pull requests are welcome. Use the [issue templates](https://github.com/li4inka/WashinPC/issues/new/choose) and include the application version, Windows build and reproduction steps. Remove personal paths and account details from attachments.

Use Windows, a .NET SDK and the .NET Framework 4.8 targeting pack. Restore uses the tracked `.nuget/offline-feed`. Portable packages include the managed SQLite provider and native x86/x64 libraries; Windows/.NET Framework remain prerequisites.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoke.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1 -Architecture x64
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1 -Architecture x86
```

Fixture tests create unique temporary directories, synthetic browser databases and unique `WashinPC.Tests-*` registry branches under HKCU. They remove their fixtures in cleanup. Settings tests use memory only. They never clean installed application data. Run registry/file-handle tests outside a restrictive filesystem sandbox. The native service mutation test creates and removes only a disposable `WashinPC.Tests-*` service; it requires an elevated Windows process and reports inconclusive when Windows denies service creation. Other service tests use an injected adapter.

Keep WPF/.NET Framework 4.8 compatibility. Preserve the existing visual design and cached pages. Filesystem and registry work belongs outside the UI thread. Test changed-object rejection, backup failure and cancellation when changing destructive operations; do not substitute a mocked success-only test for these checks.

New UI strings belong in both languages in `Localizer`. Registry expert mode does not waive eligibility checks or verified backups. All scan categories have repair handlers; changes to a handler need positive and negative evidence tests. Test backup comparison/restoration, missing values, stale previews and partial failures. Browser cleanup must preserve passwords, bookmarks, autofill and sessions; test SQLite rollback and include both native architectures in packaging.

Use synthetic data for screenshots. `scripts/measure-ui.ps1 -Screenshots` renders actual WPF views without opening a window or executing cleanup. Its figures are development-host measurements, not OS qualification.

Contributions are submitted under GPL-3.0. See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).
