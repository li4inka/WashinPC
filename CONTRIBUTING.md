# Contributing to WashinPC

Bug reports, translations and focused pull requests are welcome. Use the [issue templates](https://github.com/li4inka/WashinPC/issues/new/choose) and include the application version, Windows build and reproduction steps. Remove personal paths and account details from attachments.

Use Windows, a .NET SDK and the .NET Framework 4.8 targeting pack. Restore uses the tracked `.nuget/offline-feed`; application runtime dependencies remain Windows/.NET Framework.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/smoke.ps1 -Configuration Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/package.ps1 -Architecture x64
```

Fixture tests create unique temporary directories and unique `WashinPC.Tests-*` branches under `HKCU\Software\Classes\*\shell` and `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall`. They remove their fixtures in cleanup. Settings tests use memory only. They never clean installed application data. Run registry/file-handle tests on a normal Windows account outside a restrictive filesystem sandbox.

Keep WPF/.NET Framework 4.8 compatibility. Preserve the existing visual design and cached pages. Filesystem and registry work belongs outside the UI thread. Test changed-object rejection, backup failure and cancellation when changing destructive operations; do not substitute a mocked success-only test for these checks.

New UI strings belong in both languages in `Localizer`. Registry expert mode does not waive eligibility checks. Never enable a diagnostic category without positive and negative evidence tests.

Use synthetic data for screenshots. `scripts/measure-ui.ps1 -Screenshots` renders actual WPF views without opening a window or executing cleanup. Its figures are development-host measurements, not OS qualification.

Contributions are submitted under GPL-3.0. See [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).
