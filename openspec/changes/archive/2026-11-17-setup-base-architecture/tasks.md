# Tasks

## 1. Решение и проекты бэкенда

- [x] 1.1 Проверить окружение: `dotnet --list-sdks` и `Get-NetTCPConnection -LocalPort 5000,5173,5432 -ErrorAction SilentlyContinue`. Проверка: в списке есть SDK версии 10.x; свободные порты зафиксированы, занятые — сообщены владельцу до продолжения.
- [x] 1.2 Создать решение: `dotnet new sln -n Doska -o backend`. Проверка: `Test-Path backend/Doska.sln` возвращает `True`, `dotnet sln backend/Doska.sln list` выполняется без ошибок.
- [x] 1.3 Создать проекты: `dotnet new classlib -o backend/src/Doska.Domain -f net10.0`, так же `Doska.Application` и `Doska.Infrastructure`; `dotnet new webapi -o backend/src/Doska.Api -f net10.0`. Проверка: все четыре `.csproj` созданы и нацелены на `net10.0`.
- [x] 1.4 Создать тестовый проект: `dotnet new xunit -o backend/tests/Doska.Tests -f net10.0`. Проверка: `Test-Path backend/tests/Doska.Tests/Doska.Tests.csproj` возвращает `True`.
- [x] 1.5 Добавить проекты в решение и настроить ссылки: `Domain` ← `Application` ← `Infrastructure` ← `Api`, тесты ссылаются на `Api`. Проверка: `dotnet build backend/Doska.sln` проходит; `dotnet list backend/src/Doska.Domain/Doska.Domain.csproj reference` не выводит ни одной ссылки.
- [x] 1.6 Добавить пакеты: `Npgsql.EntityFrameworkCore.PostgreSQL` и `Microsoft.EntityFrameworkCore.Design` — в `Doska.Infrastructure`, `Swashbuckle.AspNetCore` — в `Doska.Api`, `Microsoft.AspNetCore.Mvc.Testing` — в `Doska.Tests`. Проверка: `dotnet restore backend/Doska.sln` завершается без ошибок и предупреждений о несовместимых версиях.

## 2. Конфигурация API и доступ к данным

- [x] 2.1 Перевести `Doska.Api` на контроллеры: удалить образец шаблона (`WeatherForecast` и Minimal API в `Program.cs`), подключить `AddControllers()`, `AddProblemDetails()`, `MapControllers()`, открыть `Program` для тестов (`public partial class Program`). Проверка: `dotnet build backend/Doska.sln` проходит, поиск `WeatherForecast` в каталоге `backend` не находит совпадений.
- [x] 2.2 Добавить пустой `AppDbContext` (наследник `DbContext`, без наборов сущностей) в `Doska.Infrastructure`. Проверка: `dotnet build backend/Doska.sln` проходит.
- [x] 2.3 Зарегистрировать `AppDbContext` через `AddDbContext` с `UseNpgsql(connectionString, o => o.UseSnakeCaseNamingConvention())`, строку брать из `ConnectionStrings:Default`. Проверка: при остановленной PostgreSQL `dotnet run --project backend/src/Doska.Api` в окружении `Development` стартует и отвечает на `GET http://localhost:5000/api/health` — значит, подключение к базе не выполняется на старте.
- [x] 2.4 Подключить Swagger только в разработке: `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI` под `if (app.Environment.IsDevelopment())`. Проверка: в `Development` ответ `GET http://localhost:5000/swagger/v1/swagger.json` содержит путь `/api/health`; в `Production` тот же адрес отвечает 404.
- [x] 2.5 Добавить контроллер `HealthController` с `GET /api/health`, возвращающим окружение и версию сборки. Проверка: `Invoke-RestMethod http://localhost:5000/api/health` возвращает JSON с названием окружения.
- [x] 2.6 Настроить конфигурацию: строка подключения к локальной базе — в `backend/src/Doska.Api/appsettings.Development.json`, http-профиль на порту 5000 и `ASPNETCORE_ENVIRONMENT=Development` — в `launchSettings.json`; в `appsettings.json` строки подключения нет. Проверка: `Select-String -Path backend/src/Doska.Api/appsettings.json -Pattern 'ConnectionStrings'` не находит совпадений, а запуск сообщает `Now listening on: http://localhost:5000`.
- [x] 2.7 Добавить тест API: `WebApplicationFactory<Program>` запрашивает `GET /api/health` и ожидает 200 с JSON, запущенная база не требуется. Проверка: `dotnet test` из каталога `backend` — все тесты пройдены.

## 3. CORS для локальной разработки

- [x] 3.1 Добавить политику `FrontendDev`: явные источники `http://localhost:5173` и `http://127.0.0.1:5173`, `AllowCredentials`, `AllowAnyHeader`, `AllowAnyMethod`; политика подключается только при `IsDevelopment()`. Проверка: запрос к `GET http://localhost:5000/api/health` с заголовком `Origin: http://localhost:5173` получает в ответе `Access-Control-Allow-Origin`, а с `Origin: http://example.com` — не получает.
- [x] 3.2 Покрыть CORS тестами: разрешённый источник и чужой источник. Проверка: `dotnet test` из каталога `backend` — тесты CORS пройдены.

## 4. Локальная PostgreSQL

- [x] 4.1 Добавить `docker-compose.yml` в корень: сервис `postgres` (`postgres:17-alpine`), `POSTGRES_DB=doska`, `POSTGRES_USER=doska`, `POSTGRES_PASSWORD=doska`, постоянный том, публикация порта `5433:5432` (5432 на хосте занят старым стеком `doska`), `healthcheck` через `pg_isready`; ключ `version` не указывать. Проверка: `docker compose config` разбирает файл без ошибок.
- [x] 4.2 Поднять базу: `docker compose up -d postgres`. Проверка: `docker compose ps` показывает состояние `healthy`; `docker compose exec postgres psql -U doska -d doska -c "select current_database(), current_user"` возвращает `doska` и `doska`.
- [x] 4.3 Сверить строку подключения из `appsettings.Development.json` с `docker-compose.yml` по хосту, порту, базе и пользователю. Проверка: значения совпадают; полная проверка подключения EF Core появится с первой миграцией и в это изменение не входит.

## 5. Каркас интерфейса

- [x] 5.1 Разрешить `pnpm` через corepack: в Node 26.13.1 `corepack` не поставляется, поэтому сначала установить его (`npm install -g corepack`), затем `corepack enable pnpm` и `pnpm --version`. Проверка: команда выводит версию пакетного менеджера; при отказе доступа к каталогу Node работа останавливается и владельцу сообщается о проблеме без обходных установок.
- [x] 5.2 Создать приложение: `pnpm create vite frontend --template vue-ts`, затем `pnpm --dir frontend install`. Проверка: `pnpm --dir frontend build` собирает проект.
- [x] 5.3 Добавить зависимости: `pinia`, `vue-router` и в dev-зависимости `vitest`, `@vue/test-utils`, `jsdom`. Проверка: `pnpm --dir frontend install` проходит, пакеты перечислены в `package.json`, создан `pnpm-lock.yaml`.
- [x] 5.4 Создать каталоги `src/views`, `src/components`, `src/layouts`, `src/router`, `src/stores`, `src/assets` и подключить Pinia и маршрутизатор в точке входа. Проверка: все шесть каталогов существуют, `pnpm --dir frontend build` проходит.
- [x] 5.5 Настроить прокси в `vite.config.ts`: `server.port = 5173` и `server.proxy['/api']` с `target: 'http://localhost:5000'` и `changeOrigin: true`. Проверка: при запущенном API запрос `Invoke-RestMethod http://localhost:5173/api/health` возвращает тот же JSON, что и прямой запрос к API.
- [x] 5.6 Настроить Vitest (`environment: 'jsdom'`, включение `src/**/__tests__/*.spec.ts`) и добавить проверочный тест монтирования корневого компонента с Pinia. Проверка: `pnpm --dir frontend test` — тест пройден.
- [x] 5.7 Убрать образец шаблона (`HelloWorld.vue` и стили-заглушки), оставив минимальную оболочку приложения. Проверка: `pnpm --dir frontend build` и `pnpm --dir frontend test` проходят, поиск `HelloWorld` в `frontend/src` не находит совпадений.

## 6. Документация и рамки проекта

- [x] 6.1 Привести блок `context` в `openspec/config.yaml` к новой структуре: тесты интерфейса запускаются из каталога `frontend`, локальная среда — `docker compose up -d postgres` (полный compose с API и nginx появится отдельным изменением). Проверка: поиск `web` в `openspec/config.yaml` не находит совпадений.
- [x] 6.2 Добавить `README.md` в корень: требования (SDK .NET 10, Node с corepack, Docker), команды запуска базы, API, dev-сервера и тестов. Проверка: каждая команда из README выполняется из корня проекта в записанном виде.

## 7. Интеграционная проверка

- [x] 7.1 Проверить каркас целиком: `docker compose up -d postgres`, `dotnet test` из `backend`, `pnpm test` в `frontend`, затем одновременно запущенные API и dev-сервер. Проверка: тесты бэкенда и интерфейса пройдены, `GET http://localhost:5173/api/health` отвечает через прокси, `GET http://localhost:5000/swagger/v1/swagger.json` отдаёт спецификацию.
- [x] 7.2 Проверить согласованность с OpenSpec: `openspec status --change "setup-base-architecture"` и `openspec validate`. Проверка: все артефакты изменения готовы, проверка проходит без ошибок, дельты четырёх спецификаций применяются.
- [x] 7.3 Убедиться, что внесённые изменения не выходят за рамки: в `backend` нет сущностей и миграций, в `frontend` нет бизнес-страниц, `docker-compose.yml` содержит только сервис `postgres`. Проверка: `git status --short` показывает только файлы каркаса и документации, перечисленные в `proposal.md`.

## Workflow follow-up

- Показать артефакты изменения владельцу проекта на ревью; реализацию начинать только после отдельного запроса.
- После реализации и прохождения проверок закоммитить каркас и заархивировать изменение (`openspec archive`), коммит — «Архивировать change setup-base-architecture».
- Проверить, что после архива дельты влились в `openspec/specs/` и `openspec validate` проходит на основной ветке спецификаций.
- Отдельными изменениями вынести: полный `docker compose` (API + web/nginx + PostgreSQL), первую миграцию `AppDbContext` с применением при старте, наполнение интерфейса страницами.
