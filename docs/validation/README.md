# Local validation — 2026-11-03

This is development-host evidence for **1.2.0-preview.1**, not full Windows qualification. No cleanup of installed applications or user files was performed.

## Functional coverage

39 fixture/UI tests cover existing behavior plus retained selections, changed files, busy files, partial success and cancellation/retry; ancestor rename exclusion; hardlinks and named streams; browser profiles and SQLite sidecars; mandatory backup failure; stale registry snapshots; typed `.reg` export and native Windows import into an isolated fixture; context-menu disable/enable; shared operation ownership; RU/EN messages; and headless rendering of all five pages at 1280×720 and 900×560.

Registry fixtures use unique `WashinPC.Tests-*` keys under `HKCU\Software\Classes\*\shell` and `HKCU\Software\Microsoft\Windows\CurrentVersion\Uninstall`. Only those fixture keys are mutated. File fixtures use unique temporary directories. Settings are in memory. Test cleanup removes only the fixtures. Restrictive sandboxes cannot perform all directory-handle/registry tests; the complete suite is run under a normal host account.

The original baseline built without warnings; 21/22 tests passed, with one stale source path in the theme test. That path and the smoke script path have been corrected.

Release x64 and x86 builds complete without warnings or errors. The packaging script runs the full suite per architecture and checks the PE machine type and preview version. Final local TRX evidence lives under `artifacts/test-results/x64` and `x86`; portable archives and SHA-256 files are under `artifacts/portable`. A 32-bit test process on this 64-bit host does not qualify a 32-bit Windows installation.

## Measurements and limits

Original headless harness: one warm-up and five samples, same host, 1000 files × 64 bytes. [Baseline](baseline-rendered.json) uses the original UI/cleaner source from commit `d741024`, including the pre-existing AppInfo/Utils logging edits. [Hardened measurements](hardened-rendered.json) were recorded before final documentation/label changes. The harness instantiated WPF content and performed layout; it did **not** measure process startup, UAC, a cold disk or visible-window rendering latency.

| Median | Baseline | Hardened |
|---|---:|---:|
| Window construction + headless layout | 4.18 ms | 5.07 ms |
| Cached page navigation + layout | 1.245 ms | 1.094 ms |
| Scan of 1000 small files | 4.13 ms | 89.69 ms |

The additional footer/layout work exceeds the 10% construction target in this microbenchmark (about 0.89 ms absolute). No claim is made that real startup meets the 10% target. Navigation did not regress in that comparison. File scanning now opens and checks file identities, paths and streams; its additional cost is explicit rather than hidden behind a claim of equal scan speed.

The original PowerShell rendering harness was replaced after Windows Defender blocked it. The maintained `scripts/measure-ui.ps1 -Screenshots` runs the compiled WPF fixture, without changing Defender settings. [Final navigation samples](final-navigation.csv) include populated synthetic groups and refreshed bindings; they are a different workload and must not be compared directly to the empty-page baseline. Screenshots are actual final WPF views rendered with synthetic data.

## Qualification still open

| Check | Status |
|---|---|
| Windows 7 SP1 x86/x64 on actual OS/VM | Pending |
| Windows 8.1 x86/x64 on actual OS/VM | Pending |
| Windows 10 x86/x64 on actual OS/VM | Pending |
| Windows 11 end-to-end UAC and real cleanup on disposable fixtures | Pending |
| Physical monitors at 100/125/150/175/200% scale | Pending |
| Real process startup versus baseline; old HDD/low-memory hardware | Pending |
| Concurrent/adversarial registry writers and interrupted manual restore | Pending |
| Authenticode signing | Not signed |
| Remote GitHub Actions execution | Not run; workflow prepared locally |

Registry changes are revalidated but are not transactional. Manual `.reg` import can overwrite newer changes and does not restore ACLs. Unsupported file types are preserved. Cancellation is cooperative between steps; a blocking Windows call can delay completion.

## По-русски

Это результаты локальных тестов, а не подтверждение всех целевых ОС. Очищались только искусственные фикстуры. Сохранённый дизайн и кеширование страниц проверены отрисовкой; физический DPI, реальный запуск с UAC, старый HDD и полный цикл на каждой Windows остаются открытыми.

Проверки безопасности увеличили время анализа 1000 маленьких файлов примерно с 4 до 90 мс. Навигация в сопоставимом тесте не замедлилась; создание и раскладка окна выросли примерно на 0,9 мс и не уложились в ориентир 10%. Эти цифры не являются замером запуска процесса.
