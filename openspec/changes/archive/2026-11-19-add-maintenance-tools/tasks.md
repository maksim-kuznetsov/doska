# Tasks

## 1. Очистка медиа

- [x] 1.1 Добавить `IMediaMaintenance` и `MediaCleanupResult` в Application.
- [x] 1.2 Реализовать `MediaMaintenanceService` в Infrastructure: удаление каталогов без объявления и непривязанных файлов.
- [x] 1.3 Добавить команду `cleanup-media` в `Program` и регистрацию в DI.
- [x] 1.4 Тест очистки: привязанные файлы остаются, осиротевшие каталоги и файлы удаляются.

## 2. Dev-сессии

- [x] 2.1 Сохранять ключи Data Protection в `media/keys` в Development.
- [x] 2.2 Проверить, что сессия переживает перезапуск API.

## 3. Сквозной смоук

- [x] 3.1 Добавить `scripts/smoke.ps1` и фикстуру `scripts/fixtures/photo.png`.
- [x] 3.2 Смоук проходит регистрацию/подтверждение, подачу с фото, одобрение, ленту и «Продано», убирает данные.
- [x] 3.3 Описать команду очистки и смоук в README.

## 4. Проверка

- [x] 4.1 `dotnet build backend/Doska.sln`, тесты бэкенда, `pnpm test`, `pnpm build`.
- [x] 4.2 `openspec validate add-maintenance-tools`.