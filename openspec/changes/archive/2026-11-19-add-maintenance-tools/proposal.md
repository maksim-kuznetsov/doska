# Proposal

## Why

После реализации всех возможностей остались эксплуатационные мелочи, которые мешают локальной работе: осиротевшие файлы медиа после сбоев никто не убирает, сквозной сценарий проверяется вручную, а в Development ключи Data Protection эфемерны — после перезапуска API сессии и antiforgery-токены теряются. Это техдолг, а не изменение поведения продукта.

## What Changes

- **Очистка медиа.** Добавляется идемпотентный сценарий `IMediaMaintenance`: удаляет каталоги `media/photos/<adId>` без объявления и файлы, не привязанные к `ad_photos`. Доступен командой `dotnet run --project backend/src/Doska.Api -- cleanup-media`.
- **Сквозной смоук.** Добавляется `scripts/smoke.ps1`, который на живом стенде проходит путь: регистрация → подтверждение email через psql → подача с фото → очередь модерации → одобрение → лента → «Продано» → удаление. Скрипт сам убирает за собой данные.
- **Стабильные dev-сессии.** В Development ключи Data Protection сохраняются в `media/keys`, поэтому сессии и antiforgery-токены переживают перезапуск API; предупреждение об эфемерном хранилище исчезает.
- **Тесты и документация.** Юнит-тест очистки медиа; README описывает команду и смоук.

## Capabilities

### New Capabilities

Нет.

### Modified Capabilities

Нет. Требования `openspec/specs/` не меняются; change добавляет обслуживание и инструменты проверки, поэтому объявляет `skip_specs: true`.

## Impact

- `backend/src/Doska.Application/Abstractions` — контракт обслуживания медиа.
- `backend/src/Doska.Infrastructure/Media` — реализация очистки.
- `backend/src/Doska.Api/Program.cs` — команда `cleanup-media` и ключи Data Protection в Development.
- `backend/tests/Doska.Tests` — тест очистки.
- `scripts/smoke.ps1`, `scripts/fixtures/photo.png`, `README.md`.
- Схема БД, поведение API и интерфейс не меняются.

## Вне рамок

- Периодический фоновый запуск очистки (достаточно команды обслуживания).
- Playwright и браузерный e2e: смоук выполняется по HTTP.
- Удаление приёмочных данных из dev-БД: они остаются демонстрационным примером.