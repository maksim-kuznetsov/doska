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
