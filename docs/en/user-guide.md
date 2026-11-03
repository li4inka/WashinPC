# User guide — 1.2.0-preview.1

Run WashinPC as administrator in the account whose data you want to maintain. Five sections are retained: Cleaner, Privacy, Registry, Context Menu, Settings.

## Scan and selection

Scan first, then select groups. No group is automatically checked on the first file scan; Prefetch remains optional. Cleaner scans temporary folders and Recent shortcuts. Privacy separates Chrome/Edge history and cookies across recognized profiles. Close browsers before privacy scans; SQLite recovery files are preserved.

Cleanup deletes only the selected objects retained from that scan. Newly created files require another scan. Changed, busy, linked and unsupported files are skipped. Removing cookie databases signs you out of websites. File deletion and emptying the Recycle Bin cannot be undone by WashinPC.

## Results and cancellation

Work runs in the background. Cancel is available below the current page. Cancellation is checked between file/registry steps; a blocking OS call or restore-point creation may need to return first. Already completed changes remain applied. The result shows applied, skipped and failed counts and successfully deleted file bytes; this is not a free-space measurement. Details groups reasons without exposing personal paths.

Pages retain their results and selection when navigating. Another modifying operation cannot start until the active one ends. Closing while busy requests cancellation; close again after completion.

## Registry

Default repairs are limited to confirmed missing SharedDLL paths with a DWORD zero reference count, and missing HelpFile values. Expert mode enables additional diagnostic categories; it does not enable their repair. The scan distinguishes missing targets from unavailable or ambiguous paths.

A restore point is attempted before fixes. You can cancel when it fails; continuing still requires a successful per-object backup. The backup option cannot be disabled.

## Context Menu

Refresh the list. Off reflects disabled state; change it to disable/enable. Del selects deletion. Applying deletion requires confirmation. Entries are revalidated and backed up first. Successfully processed rows leave the current result; Refresh reloads the current state. Machine-level extension blocks cannot be lifted by the current-user toggle.

## Backups and preferences

Backups opens Documents/WashinPC_Backups/Registry. Read the `.reg.txt` companion file for the original key and registry view. Compare current values before import, and use the indicated `reg.exe import ... /reg:32` or `/reg:64` command in the original account. For a toggle, import the full-key backup and its value backups to restore absent/present toggle values. Import is manual and can overwrite newer changes; ACLs and registry key metadata are not restored by `.reg` files.

Preferences remain under HKCU/Software/WashinPC; old backups are not moved. English/Russian changes apply to labels immediately. Operation messages use the language in effect when the operation completes. No installer is required; preserve backups separately when deleting the portable folder.
