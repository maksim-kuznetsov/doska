# Design

## Context

See `proposal.md` — Why. Текущее состояние:

- Решение `backend/Doska.sln` на .NET 10; тесты xUnit v3 на Microsoft.Testing.Platform; интеграционные тесты используют PostgreSQL на `localhost:5433` и строку `ConnectionStrings__Default`.
- Миграции EF Core применяются через локальный инструмент `dotnet-ef` (манифест `backend/.config/dotnet-tools.json`).
- Frontend на Vue 3 + Vite + Vitest, пакетный менеджер pnpm, lockfile в `frontend/pnpm-lock.yaml`.
- Production-развёртывание описано в `docker-compose.prod.yml`, `backend/Dockerfile`, `frontend/Dockerfile`, `deploy/nginx.conf`.
- В песочнице разработчика `dotnet test` падает из-за ограничений named pipe MSBuild; на GitHub-hosted runner такого ограничения нет.

## Goals / Non-Goals

**Goals:**

- Автоматически проверять сборку, миграции и тесты на каждом изменении.
- Проверять, что модель EF Core не разошлась с миграциями.
- Проверять, что production-образы собираются из текущего кода.

**Non-Goals:**

- Публикация и деплой.
- Матрица ОС/версий.
- Сканирование безопасности.

## Decisions

### 1. PostgreSQL как service-контейнер на порту 5433

Backend-джоба поднимает `postgres:17-alpine` с healthcheck и публикует `5433:5432`, чтобы совпасть с локальной строкой подключения и `appsettings.Development.json`. Переменная `ConnectionStrings__Default` задаётся на уровне джобы, поэтому и `dotnet ef`, и тесты используют один адрес. Альтернатива — отдельная тестовая база на 5432: потребовала бы переопределения конфигурации и разошлась бы с локальной средой.

### 2. Проверка модели и применение миграций до тестов

Сначала выполняется `dotnet ef migrations has-pending-model-changes`: если сущности изменили без миграции, джоба падает. Затем `dotnet ef database update` создаёт схему на чистой базе. Это одновременно проверяет миграции и готовит БД для интеграционных тестов, которые стартуют с `MigrateOnStartup=false`.

### 3. `dotnet test` как единая команда

Используется `dotnet test backend/Doska.sln --no-build` (после сборки) с `TESTINGPLATFORM_TELEMETRY_OPTOUT=1`. Это соответствует требованию `platform` о запуске тестов командой `dotnet test`. Если на конкретном runner проявится проблема IPC, запасной вариант — запуск `backend/tests/Doska.Tests/bin/Debug/net10.0/Doska.Tests`, но по умолчанию используется стандартная команда.

### 4. Отдельная docker-джоба после тестов

`docker compose -f docker-compose.prod.yml config` проверяет интерполяцию и структуру, затем `docker compose ... build` собирает оба образа. Джоба зависит от `backend` и `frontend`, поэтому не тратит время при красных тестах и подтверждает, что production-сборка не сломана. Образы не публикуются.

### 5. Кэши и отмена устаревших запусков

`actions/setup-dotnet` кэширует NuGet по `backend/**/*.csproj`; `actions/setup-node` — pnpm-store по `frontend/pnpm-lock.yaml`. `concurrency` отменяет предыдущий запуск для той же ветки/PR, чтобы не проверять устаревший коммит.

## Risks / Trade-offs

- **Версия pnpm в CI может отличаться от локальной** → используется pnpm 10, совместимый с lockfile v9; при расхождении версия фиксируется в workflow.
- **Docker-джоба увеличивает время CI** → запускается только после зелёных тестов; кэш слоёв Docker на runner помогает не полностью, но проверка образов важнее.
- **`dotnet test` на Microsoft.Testing.Platform** → при проблеме на runner остаётся прямой запуск тестового exe, описанный в README.
- **Интеграционные тесты пишут в БД** → база в CI одноразовая, сервис-контейнер удаляется после джобы.

## Migration Plan

1. Добавить workflow и раздел README.
2. Проверить локально: `docker compose -f docker-compose.prod.yml config`, сборка, тесты.
3. После пуша workflow запускается на GitHub; при красном статусе мёржить нельзя.
4. Откат — удалить workflow; код приложения не затрагивается.