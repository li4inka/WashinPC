# User guide — 1.2.0-preview.2

Run WashinPC as administrator in the account whose data you want to maintain. Five sections are retained: Cleaner, Privacy, Registry, Context Menu, Settings.

## Scan and selection

Scan first, then select groups. No group is automatically checked on the first file scan; Prefetch remains optional. Cleaner scans temporary folders and Recent shortcuts. Privacy separates history, cookies and cache for Chrome, Edge, Firefox, Opera, Opera GX, Brave and Yandex Browser across standard profiles. Custom portable locations are not included. Close browsers before privacy scans; SQLite recovery files are preserved.

Cleanup processes only selected objects retained from that scan. Newly created files require another scan. Changed, busy, linked and unsupported files are skipped. Browser history and cookies are cleared transactionally inside their databases; Firefox bookmarks, passwords, autofill and sessions are preserved. Clearing cookies signs you out of websites. File deletion and emptying the Recycle Bin cannot be undone by WashinPC. Results distinguish deleted files, cleaned records and deleted file bytes; database size is not counted as freed space.

## Results and cancellation

Work runs in the background. Cancel is available below the current page. Cancellation is checked between file/registry steps; a blocking OS call or restore-point creation may need to return first. Already completed changes remain applied. The result shows applied, skipped and failed counts and successfully deleted file bytes; this is not a free-space measurement. Details groups reasons without exposing personal paths.

Pages retain their results and selection when navigating. Another modifying operation cannot start until the active one ends. Closing while busy requests cancellation; close again after completion.

## Registry

All scan categories support repair. Expert mode enables additional categories for scanning and repair. Before each repair the app checks the affected registry data and whether the finding still applies, then creates a verified backup. The scan distinguishes missing targets from unavailable or ambiguous paths.

A restore point is attempted before fixes. You can cancel when it fails; continuing still requires a successful per-object backup. The backup option cannot be disabled.

Windows services is a separate Dangerous category, available only in expert mode and unchecked by default. It disables stopped own-process services with a confirmed missing EXE through the Service Control Manager. Drivers, shared processes, running services and ambiguous paths are skipped. Services and their files are not deleted.

## Context Menu

Refresh the list. Off reflects disabled state; change it to disable/enable. Del selects deletion. Applying deletion requires confirmation. Entries are revalidated and backed up first. Successfully processed rows leave the current result; Refresh reloads the current state. Machine-level extension blocks cannot be lifted by the current-user toggle.

## Backups and preferences

Backups opens the backup catalog. Select a backup, click Compare, review the displayed changes, then Restore and confirm. Current affected objects are backed up before writing. Legacy `.reg` backups and `.reg.txt` view information are supported; choose x86/x64 explicitly if the view is unknown. New backups include versioned metadata, account identity and a checksum. Service backups restore startup configuration through Service Control Manager. Use Open folder to access Documents/WashinPC_Backups/Registry. Context-menu toggle operations may have several separate backup files; restore the relevant value backups as well as the key backup. Restoration preserves objects absent from the selected backup, except explicit deletion directives in undo backups. ACLs and registry key metadata are not restored. Cleaned files have no recovery copies.

Preferences remain under HKCU/Software/WashinPC; old backups are not moved. English/Russian changes apply to labels immediately. Operation messages use the language in effect when the operation completes. No installer is required; preserve backups separately when deleting the portable folder.

Settings offers System, Light and Dark themes. System follows the Windows application theme while WashinPC is running and remains selected when settings are reopened. Systems without a supported theme preference use light colors.
