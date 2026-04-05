# Диагностика и QA-матрица

## Типовые проблемы

### Требуются права администратора

Симптом: приложение закрывается при старте.
Действие: запуск от имени администратора.

### Не создана точка восстановления

Симптом: предупреждение перед фиксом реестра.
Действие: для максимальной безопасности выбрать `Отмена`; `Продолжить` — только при осознанном риске.

### Отказ в доступе при применении

Симптом: частичный успех и счетчики skipped/failed.
Действие: повторить с elevation, проверить политики безопасности/антивирус.

## Ручная QA-матрица

| Сценарий | Win7 x64 | Win8 x64 | Win10 x64 | Win11 x64 | Критерий |
|---|---|---|---|---|---|
| Запуск + проверка прав | Pending | Pending | Pending | Pending | Корректный старт, валидный admin-flow |
| Cleaner: scan/clean выбранного | Pending | Pending | Pending | Pending | Удаляется только выбранное |
| Privacy: scan/clean выбранного | Pending | Pending | Pending | Pending | Удаляется только выбранное |
| Registry: safe-only scan/fix | Pending | Pending | Pending | Pending | Корректные счетчики и повторный скан |
| Restore point fail flow | Pending | Pending | Pending | Pending | Ветки Continue/Cancel корректны |
| Context menu: disable/delete/apply | Pending | Pending | Pending | Pending | UI соответствует реальным изменениям |
| Переключение RU/EN и ключи | Pending | Pending | Pending | Pending | Нет «сырых» локализационных ключей |
| Отказоустойчивость (доступ/частичный успех) | Pending | Pending | Pending | Pending | Нет падений, статусы и логи корректны |

## Release Gate

Релиз-кандидат получает `PASS`, только если:

1. Автотесты зеленые.
2. В QA-матрице нет блокеров P0/P1.
3. Выполнены требования logging policy.

Иначе: `FAIL`.
