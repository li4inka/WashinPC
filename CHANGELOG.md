# Changelog

## 1.2.0-preview.2 — unreleased

- Keep the five-section WPF interface and light design; fit small work areas with scrolling.
- Delete only selected objects from the retained scan. Verify file identity and paths; preserve linked, special, changed and busy files.
- Recognize Chrome, Edge, Firefox, Opera/GX, Brave and Yandex profiles; separate history/cookies/cache. Clear database records transactionally and preserve Firefox bookmarks, credentials and sessions.
- Require verified registry backups for all category repair handlers; add a restore catalog with comparison, legacy support, view/account/checksum checks and current-state undo copies.
- Add expert-only Windows services repair and restoration through Service Control Manager, with stopped own-process/missing-EXE checks.
- Preserve System theme mode and react to Windows theme changes. Verify registry write results and distinguish disappeared objects from successful repairs.
- Back up and revalidate context-menu changes; preserve machine-level blocks.
- Run operations in the background with a shared busy gate, cancellation and accurate partial results.
- Add fixture and WPF regression tests, bilingual documentation, portable packaging and Windows CI.

This is an unsigned preview. Full Windows/physical DPI qualification and real process-startup measurements remain pending. See [validation](docs/validation/README.md).
