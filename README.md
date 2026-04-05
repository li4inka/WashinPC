# WashinPC

WashinPC is a Windows utility focused on safe cleanup and predictable maintenance.

## Current Release Focus

- Hardened registry fix pipeline with explicit safety context
- Restore point pre-check before registry mutation
- Safe-only default for registry categories
- Context menu management with actionable Disable/Delete model
- RU/EN localization parity for critical UI flows
- Structured logs for diagnostics and release support

## Build And Test

```powershell
dotnet build WashinPC.sln
```

```powershell
dotnet test WashinPC.sln
```

Smoke script:

```powershell
.\scripts\smoke.ps1 -Configuration Release
```

## Documentation

### English

- [User Guide](docs/en/user-guide.md)
- [Registry Safety](docs/en/registry-safety.md)
- [Logging Policy](docs/en/logging-policy.md)
- [Troubleshooting + QA Matrix](docs/en/troubleshooting-qa-matrix.md)

### Русский

- [Руководство пользователя](docs/ru/user-guide.md)
- [Безопасность реестра](docs/ru/registry-safety.md)
- [Политика логирования](docs/ru/logging-policy.md)
- [Диагностика и QA-матрица](docs/ru/troubleshooting-qa-matrix.md)

## Release Gate (Summary)

The release candidate is accepted only when:

1. `dotnet build` completes without errors.
2. `dotnet test` is fully green.
3. Manual QA matrix for Win7/8/10/11 is completed with no P0/P1 blockers.
4. Logging policy requirements are met (format, privacy, rotation, mandatory events).
