# Local validation — 2026-11-03

This is development-host evidence for **1.2.0-preview.2**, not full Windows qualification. No cleanup of installed applications or user files was performed.

## Current feature validation

The current suite has 73 cases: 72 pass and one service-mutation integration case is inconclusive on this host because `sc.exe create` returns access denied. The process can read real service configuration through Service Control Manager; disable/restore, stale state, backup failure and cancellation are tested through an injected service adapter. No installed service was changed. An elevated disposable-service test is included and must pass during Windows qualification.

Текущие изменения: 73 проверки, из них 72 успешны и одна проверка реального изменения тестовой службы пропущена из-за отказа Windows в создании службы. Чтение конфигурации через Windows API и логика отключения/восстановления через тестовый адаптер проверены. Проверки браузеров используют искусственные базы, а отрисовка выполняется без видимого окна. Реальные профили браузеров, повышенные права для изменения службы и полный набор целевых ОС требуют отдельной квалификации.

Added coverage includes typed registry restoration, missing keys/values, undo backups, legacy view selection, corruption, stale previews, cancellation and partial write failures; all six Chromium-family profile variants plus Firefox, separated categories, bookmark/credential/session/download preservation, unknown schemas and SQLite rollback; and System theme persistence/manual override. Browser tests use synthetic SQLite databases, not personal browser profiles. These tests do not qualify every live browser schema/version.

The WPF fixture renders the backup catalog at 1000x680 and 700x480, and Privacy/Registry/Settings in RU/EN and light/dark themes. Inspectable PNGs are generated in `artifacts/ui-qa`. This is headless layout and visual inspection, not a physical-DPI or accessibility certification.

Packaging includes the managed SQLite provider and both native architectures. Per-architecture TRX results are retained under `artifacts/test-results/x64` and `x86`. Archives and SHA-256 companions remain under `artifacts/portable`. Complete OS/VM, live-browser and elevated service qualification is still pending.

## Historical functional coverage

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
