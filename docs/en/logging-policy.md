# Logging Policy

## Log Format

Each line uses this normalized format:

`utc_ts | level | subsystem | event_id | operation_id | message`

## Levels

- `Info`: default operational events.
- `Warn`: recoverable problems, partial failures, user cancels.
- `Error`: unexpected failures/exceptions.

## Storage And Rotation

- Log location: `%LOCALAPPDATA%\WashinPC\logs\`.
- Active file: `washinpc.log`.
- Rotation: max 2 MB per file, keep up to 5 archived files.

## Privacy Rules

- No sensitive data by default.
- Detailed/sensitive fragments only in diagnostic mode.
- User profile paths should be masked in non-verbose mode.

## Mandatory Events

- app startup/shutdown
- scan start/done (cleaner/privacy/registry/context-menu)
- apply/fix start/done
- restore point success/fail
- backup success/fail
- user cancel paths
- unhandled exceptions
