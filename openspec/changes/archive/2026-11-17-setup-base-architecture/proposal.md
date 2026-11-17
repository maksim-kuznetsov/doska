# Proposal

## Why

В репозитории есть только документация OpenSpec: ни решения .NET, ни приложения Vue, ни локальной базы данных. Начать первую задачу разработки негде — нет каталогов под код и нет запускаемой среды с PostgreSQL. Нужен базовый каркас, на который будут опираться все последующие изменения.

## What Changes

- **Бэкенд**: решение `backend/Doska.sln` с проектами `src/Doska.Domain`, `src/Doska.Application`, `src/Doska.Infrastructure`, `src/Doska.Api` и тестами `tests/Doska.Tests`; Web API на `net10.0` с провайдером Npgsql, Swagger/OpenAPI в окружении `Development` и пустым `AppDbContext` (без сущностей и миграций).
- **Интерфейс**: приложение Vue 3 (Composition API, TypeScript, Vite, Pinia) в `frontend/` с раскладкой `src/views`, `src/components`, `src/layouts`, `src/router`, `src/stores`, `src/assets` и проксированием `/api` на бэкенд в `vite.config.ts`.
- **Локальная среда**: CORS на бэкенде для dev-сервера Vite и `docker-compose.yml` в корне проекта с одним контейнером PostgreSQL (том для данных, healthcheck, публикация на порту хоста 5433 — порт 5432 занят стеком предыдущей версии проекта).
- **Проверочный эндпоинт**: `GET /api/health` возвращает окружение и версию сборки. Бизнес-логики не несёт и нужен, чтобы проверить запуск API, CORS и проксирование без обращения к базе.
- **BREAKING** (внутри проекта): структура репозитория отклоняется от `openspec/specs/platform/spec.md` — вместо `Doska.sln` в корне и каталога `web` появляются `backend/` и `frontend/`. Требование «Структура репозитория» приводится в соответствие дельтой спеки; названия сценариев в блоке `MODIFIED` сохраняются дословно, поэтому один сценарий остаётся с прежним названием («Состав каталога web»), описывая уже `frontend/`.
- **Допущение**: решение размещается в `backend/Doska.sln` вместе с проектами (`backend/src/Doska.*`, `backend/tests/Doska.Tests`), интерфейс — в `frontend/`; каталог `web` из текущей спецификации не создаётся.

## Capabilities

### New Capabilities

Нет: изменение не добавляет новой возможности системы, а разворачивает каркас уже описанной платформы.

### Modified Capabilities

- `platform`: требование «Структура репозитория» переходит на каталоги `backend/` и `frontend/`, требование «Тестирование» — на команды из этих каталогов; добавляется требование «Локальная среда разработки» (CORS для dev-сервера, прокси `/api`, PostgreSQL в `docker compose`).
- `user-accounts`, `static-assets`, `design-system`: в требованиях обновляются пути к файлам интерфейса (`web/...` → `frontend/...`), иначе спецификации противоречат новой структуре репозитория.

## Impact

- `backend/` — новое решение: `Doska.sln`, `src/Doska.Domain`, `src/Doska.Application`, `src/Doska.Infrastructure` (EF Core, `AppDbContext`), `src/Doska.Api` (хост, Swagger, CORS), `tests/Doska.Tests`.
- Зависимости бэкенда: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Swashbuckle.AspNetCore`, тестовый стек (`xunit`, `Microsoft.AspNetCore.Mvc.Testing`).
- `frontend/` — новое приложение Vite: `package.json`, `vite.config.ts` (прокси `/api`), `tsconfig`, `index.html`, точка входа `src/main.ts`, пустой маршрутизатор и хранилище Pinia, Vitest для тестов.
- `docker-compose.yml` в корне — сервис `postgres` для локальной разработки.
- `.gitignore` в корне — исключает артефакты сборки .NET (`bin/`, `obj/`), кэш инструментов окружения (`.tools/`) и зависимости интерфейса, чтобы `git status` показывал только файлы каркаса.
- `README.md` в корне — требования к окружению и команды запуска базы, API, dev-сервера и тестов.
- `openspec/specs/` — дельтами обновляются `platform`, `user-accounts`, `static-assets`, `design-system`.
- `openspec/config.yaml` — в блоке `context` упоминания каталога `web` и команды из него заменяются на `frontend`.
- Риски: занятые порты (API 5000, Vite 5173, PostgreSQL 5432 — 5432 оказался занят, контейнер публикуется на 5433) и отсутствие `pnpm` в системе — снимаются проверкой в задачах и установкой `pnpm` через `corepack` (сам `corepack` ставится через `npm`, так как в Node 26.13.1 он не поставляется).

## Вне рамок

- Сущности предметной области, миграции EF Core и наполнение `AppDbContext` — каркас создаётся пустым.
- Бизнес-сценарии, аутентификация, страницы и компоненты интерфейса, бизнес-эндпоинты сверх проверочного.
- Полное окружение `docker compose` для развёртывания (api + frontend/nginx + postgres) и продакшн-настройки — отдельное изменение.
- CI, HTTPS-сертификаты, брокер сообщений, внешний поисковый движок, S3-совместимое хранилище.
