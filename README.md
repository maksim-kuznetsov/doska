# Doska

Доска объявлений: бэкенд — ASP.NET Core Web API (.NET 10) с PostgreSQL, интерфейс — Vue 3 (Composition API, TypeScript, Vite, Pinia).

## Структура репозитория

- `backend/` — решение `Doska.sln`: `src/Doska.Domain`, `src/Doska.Application`, `src/Doska.Infrastructure`, `src/Doska.Api`, тесты `tests/Doska.Tests`.
- `frontend/` — приложение Vue 3 с проксированием `/api` на бэкенд.
- `openspec/` — спецификации и изменения.
- `docker-compose.yml` — локальная PostgreSQL.

## Требования к окружению

- .NET SDK 10.0.
- Node.js и pnpm: в Node 26 `corepack` не поставляется, поэтому `npm install -g corepack`, затем `corepack enable pnpm`.
- Docker с плагином `docker compose`.

## Локальный запуск

1. База данных:

   ```
   docker compose up -d postgres
   ```

   PostgreSQL 17 публикуется на порту **5433** (порт 5432 занят стеком предыдущей версии проекта), база и пользователь — `doska`, пароль — `doska`.

2. API:

   ```
   dotnet run --project backend/src/Doska.Api
   ```

   Адрес — http://localhost:5000, Swagger — http://localhost:5000/swagger (только в окружении `Development`).

3. Dev-сервер интерфейса:

   ```
   pnpm --dir frontend dev
   ```

   Адрес — http://localhost:5173; запросы к `/api` проксируются на http://localhost:5000.

## Проверка каркаса

- `curl.exe http://localhost:5000/api/health` — прямой ответ API.
- `curl.exe http://localhost:5173/api/health` — ответ через прокси Vite.

## Тесты

- Бэкенд: `dotnet test backend/Doska.sln`.
- Интерфейс: `pnpm --dir frontend test`.

## Остановка

- `docker compose down` — остановить контейнеры, данные остаются в томе `postgres-data`.
- `docker compose down -v` — остановить контейнеры и удалить том с данными.

## Производственное развёртывание

Развёртывание выполняется через `docker compose` на одном сервере и включает три сервиса:
PostgreSQL, API и веб-сервер nginx, который отдаёт собранный интерфейс и проксирует `/api`.

1. Подготовьте секреты: скопируйте `.env.example` в `.env` и задайте значения.
   Файл `.env` в репозиторий не попадает.

   ```
   Copy-Item .env.example .env
   ```

2. Соберите и запустите сервисы:

   ```
   docker compose -f docker-compose.prod.yml up -d --build
   ```

3. Интерфейс доступен на `http://localhost` (порт задаётся переменной `WEB_PORT`).

Секреты и параметры подключения передаются переменными окружения и не хранятся в репозитории.

## Окружения и настройки

- Окружение задаётся переменной `ASPNETCORE_ENVIRONMENT`: `Development` для локальной работы,
  `Production` для развёртывания. В `Production` недоступны Swagger и страница-гайд дизайн-системы.
- Настройки читаются из `appsettings*.json` и переопределяются переменными окружения
  (двойное подчёркивание вместо двоеточия, например `ConnectionStrings__Default`).
- Миграции EF Core применяются при старте API. Если миграция завершается ошибкой, приложение
  останавливается и не начинает обработку запросов — развёртывание не переключается на новую версию.
- Медиафайлы хранятся в томе `media-data`; в `Production` их отдаёт nginx по внутреннему
  перенаправлению приложения, в `Development` — само приложение.

## Откат релиза

1. Верните предыдущую ревизию кода:

   ```
   git checkout <предыдущий-коммит-или-тег>
   ```

2. Повторно соберите и запустите сервисы:

   ```
   docker compose -f docker-compose.prod.yml up -d --build
   ```

Данные остаются в томах `postgres-data` и `media-data`; миграции аддитивны, поэтому предыдущая
версия приложения совместима с текущей схемой.
