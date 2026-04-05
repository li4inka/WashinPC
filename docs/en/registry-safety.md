# Registry Safety

## Scope

This document defines practical safety guarantees for registry operations on Windows 7/8/10/11.

Absolute mathematical guarantee of "never break OS" is impossible. WashinPC uses conservative controls to reduce risk.

## Safety Controls

1. Safe-only default profile.
2. Advanced/Dangerous categories visible but disabled by default.
3. Restore point attempt before mutation.
4. Continue/cancel confirmation when restore point fails.
5. Optional per-key backups during fix.
6. Session result counters: fixed/failed/skipped.

## Fix Pipeline

1. Preflight selected issues.
2. Try creating restore point.
3. If failed: ask user continue/cancel.
4. Apply fixes with explicit fix context.
5. Produce session summary and logs.

## Auto-fix Rule

Auto-fix should be restricted to entries with verified broken target references (missing file/path).
Heuristic or ambiguous cases must be skipped by default.

## Rollback Path

- Use Windows System Restore if restore point exists.
- Use exported backup keys when available.
- Re-scan and validate issue reduction after each apply cycle.
