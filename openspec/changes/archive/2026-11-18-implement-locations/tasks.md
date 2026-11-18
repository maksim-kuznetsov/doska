# Tasks

## 1. Домен

- [x] 1.1 Добавить в `City` методы `Rename`, `Activate`, `Deactivate` и `MaxNameLength`. Проверка: доменные тесты.

## 2. Сценарии

- [x] 2.1 Создать `CityService` со списком для администратора, созданием, переименованием, активацией, деактивацией и удалением. Проверка: `dotnet build`.
- [x] 2.2 Запретить удаление города с объявлениями и дубликаты названий. Проверка: API-тесты 409.

## 3. API

- [x] 3.1 Добавить `AdminCitiesController` и контракты под политикой `Admin`. Проверка: `dotnet build`.
- [x] 3.2 Проверить, что модератор и пользователь получают 403, аноним — 401. Проверка: API-тесты.
- [x] 3.3 Проверить, что деактивированный город исчезает из `/api/catalog/cities`, а объявления с ним остаются. Проверка: API-тест.

## 4. Интерфейс

- [x] 4.1 Добавить методы управления городами в клиент API. Проверка: `pnpm --dir frontend build`.
- [x] 4.2 Создать `AdminCitiesView` и маршрут `/admin/cities`; добавить пункт в меню администратора. Проверка: `pnpm --dir frontend build`.

## 5. Проверка

- [x] 5.1 `dotnet build backend/Doska.sln`, тесты бэкенда, `pnpm --dir frontend test`, `pnpm --dir frontend build`.
- [x] 5.2 `openspec validate implement-locations`.