# Registry safety

The preview permits only missing zero-count SharedDLL and missing HelpFile repairs. All other categories are diagnostic, even in expert mode. Missing means the immediate parent is readable and the target is confirmed absent; permission errors, missing parents and remote/relative paths are unknown.

Each eligible result retains a registry snapshot. Before mutation the app verifies the snapshot and missing target, writes a unique UTF-16 `.reg` backup with the original value types, verifies the saved content, then checks eligibility again. Failure blocks that change. This is not a transactional registry engine; a concurrent writer can still race the final check. Use a quiet test system for qualification.

The backup companion file identifies the hive, key and x86/x64 view and gives a view-specific import command. Manual import may overwrite newer values, does not restore ACLs, and is not conflict-aware recovery. Context-menu toggles include value backups with deletion directives for values that were originally absent. Extension deletion does not clean up shared blocked-list entries.

A system restore point is attempted before registry fixes. Its failure requires Continue/Cancel; Continue never disables backup requirements. Old backups stay in place. Review the actual target OS qualification in [validation](../validation/README.md).
