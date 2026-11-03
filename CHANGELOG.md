# Changelog

## 1.2.0-preview.1 — unreleased

- Keep the five-section WPF interface and light design; fit small work areas with scrolling.
- Delete only selected objects from the retained scan. Verify file identity and paths; preserve linked, special, changed and busy files.
- Recognize Chrome/Edge profiles and separate history/cookies. Preserve running-browser databases and SQLite recovery files.
- Require verified registry backups. Only confirmed missing zero-count SharedDLL entries and HelpFile values are automatically fixable; other findings are diagnostic.
- Back up and revalidate context-menu changes; preserve machine-level blocks.
- Run operations in the background with a shared busy gate, cancellation and accurate partial results.
- Add fixture and WPF regression tests, bilingual documentation, portable packaging and Windows CI.

This is an unsigned preview. Full Windows/physical DPI qualification and real process-startup measurements remain pending. See [validation](docs/validation/README.md).
