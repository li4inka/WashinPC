# User Guide

## Purpose

WashinPC helps remove temporary files, privacy traces, and safe registry garbage while minimizing OS risk.

## Sections

- Cleaner: scans and removes selected file groups.
- Privacy: scans and removes selected browser/privacy artifacts.
- Registry: scans selected categories and applies conservative fixes.
- Context Menu: shows actionable menu entries for disable/delete operations.
- Settings: language and registry expert mode.

## Registry Expert Mode

Default behavior is safe-only. Advanced and Dangerous categories are visible but blocked.
To unlock them:

1. Open `Settings`.
2. Enable `Registry expert mode`.
3. Return to `Registry` and select additional categories.

## Context Menu Workflow

1. Open `Context Menu`.
2. Click `Refresh` to load current entries.
3. Mark entries in `Off` and/or `Del` columns.
4. Click `Apply`.

Notes:

- Some entries support only disable or only delete.
- Unsupported actions are shown as disabled with a reason.

## Safety Notes

- Registry changes attempt to create a restore point before applying fixes.
- If restore point creation fails, user must explicitly confirm continue/cancel.
- Automatic fixes are limited to confirmed broken references where possible.
