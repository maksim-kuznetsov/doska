# Tasks

## 1. Новый путь загрузки

- [x] 1.1 Перевести построение путей оригинала и миниатюры в `src/Doska.Infrastructure` (`IFileStorage`) на префикс `photos/`; проверить тестом, что при сохранении сущности `AdPhoto` оригинал получает имя `photos/<ad_id>/...`, а миниатюра — `photos/<ad_id>/thumbs/...`, и прогнать `dotnet test --filter AdPhotoPathTests`.

## 2. Перенос существующих файлов

- [x] 2.1 Добавить миграцию EF Core `MoveMediaToPhotos` (`dotnet ef migrations add MoveMediaToPhotos --project src/Doska.Infrastructure --startup-project src/Doska.Api`), которая через `IFileStorage` копирует файлы из `ads/` в `photos/`, обновляет имена `image` и `thumbnail`, удаляет исходники, пропускает отсутствующие файлы и повторный запуск; проверить тестом перенос оригинала и миниатюры, идемпотентность и обратную миграцию, прогнать `dotnet test --filter MoveMediaToPhotosTests`.
- [x] 2.2 Применить миграцию на локальном PostgreSQL командой `dotnet ef database update --project src/Doska.Infrastructure --startup-project src/Doska.Api` и проверить существующие два объявления: файлы и миниатюры лежат в `/data/media/photos/`, имена в базе начинаются с `photos/`, адреса новых файлов отвечают `200`, а старые адреса `/media/ads/...` продолжают отдавать файлы; проверить `dotnet ef migrations list --project src/Doska.Infrastructure --startup-project src/Doska.Api`.

## 3. Проверка интеграции

- [x] 3.1 Пересобрать `web` и открыть ленту, карточку объявления и «Мои объявления» в браузере с включённым блокировщиком: все фотографии загружаются, в консоли нет ошибок и заблокированных запросов.
- [x] 3.2 Убедиться, что представления формируют ссылки только на `/media/photos/`, а `deploy/nginx.conf` сохраняет совместимость прежних адресов `/media/ads/...`; проверить, что README и `deploy/DEPLOY.md` не ссылаются на устаревший путь, и прогнать полный `dotnet test` без регрессий.

## Workflow follow-up

- Archive the change after review is complete.
