# Troubleshooting And QA Matrix

## Common Issues

### App requires admin privileges

Symptom: app exits on startup.
Action: run as Administrator.

### Restore point cannot be created

Symptom: warning before registry fix.
Action: choose `Cancel` for maximum safety, or `Continue` only if you accept risk.

### Access denied during apply

Symptom: partial success with skipped/failed counters.
Action: re-run with elevated rights, check endpoint security policies.

## QA Matrix (Manual)

Backup restoration requires a readable supported backup and an explicit registry view when legacy metadata is missing. Re-run Compare if the object changed. Privacy preserves unsupported database schemas and running-browser databases. The disposable-service mutation integration test requires an elevated Windows process; a sandbox permission override alone does not provide a Windows administrator token.

| Scenario | Win7 x64 | Win8.1 x64 | Win10 x64 | Win11 x64 | Result criteria |
|---|---|---|---|---|---|
| Launch + admin check | Pending | Pending | Pending | Pending | Starts cleanly, admin flow works |
| Cleaner scan/clean selected groups | Pending | Pending | Pending | Pending | Only selected groups are removed |
| Privacy scan/clean selected groups | Pending | Pending | Pending | Pending | Only selected groups are removed |
| Registry safe-only scan/fix | Pending | Pending | Pending | Pending | Counters and retained results are consistent |
| Restore point fail flow | Pending | Pending | Pending | Pending | Continue/Cancel both behave correctly |
| Context menu disable/delete/apply | Pending | Pending | Pending | Pending | UI actions match applied registry changes |
| RU/EN switch and key coverage | Pending | Pending | Pending | Pending | No raw localization keys in critical UI |
| Error resilience (access denied, partial failures) | Pending | Pending | Pending | Pending | No crash, clear status/log output |

## Release Gate

A release candidate is `PASS` only when:

1. All auto-tests are green.
2. QA matrix has no P0/P1 failures.
3. Logging policy checklist is fully satisfied.

Otherwise: `FAIL`.


Local automated evidence and qualification limits: [validation](../validation/README.md). Backup failure blocks a change; all scan categories support repair, with snapshot and applicability checks.
