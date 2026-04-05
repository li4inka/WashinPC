# Политика логирования

## Формат

Каждая запись лога хранится в формате:

`utc_ts | level | subsystem | event_id | operation_id | message`

## Уровни

- `Info`: штатные события.
- `Warn`: частичные/восстановимые проблемы, отмены пользователем.
- `Error`: неожиданные ошибки/исключения.

## Хранение и ротация

- Каталог: `%LOCALAPPDATA%\WashinPC\logs\`.
- Активный файл: `washinpc.log`.
- Ротация: 2 MB на файл, до 5 архивов.

## Приватность

- По умолчанию без чувствительных данных.
- Подробные данные — только в диагностическом режиме.
- Пути профиля пользователя маскируются в не-verbose режиме.

## Обязательные события

- startup/shutdown приложения
- scan start/done (cleaner/privacy/registry/context-menu)
- apply/fix start/done
- restore point success/fail
- backup success/fail
- ветки cancel
- необработанные исключения
