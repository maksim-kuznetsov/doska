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

## Сквозной смоук

`scripts/smoke.ps1` проходит живой стенд целиком: регистрация → подтверждение email через psql →
подача объявления с фото → очередь модерации → одобрение → лента → «Продано» → удаление.
Скрипт создаёт временного пользователя и убирает его и объявление в конце.

```
powershell -File scripts/smoke.ps1
```

Параметры: `-BaseUrl` (по умолчанию `http://localhost:5000`), `-ModeratorEmail`,
`-ModeratorPassword`, `-DatabaseUser`, `-DatabaseName`. Нужны запущенные API, PostgreSQL
из `docker compose` и демо-модератор (`moderator@example.com` / `123789`).

## UI-смоук (Playwright)

```
pnpm --dir frontend e2e
```

Браузерный смоук проверяет публичную ленту и фильтры, вход модератора (очередь и жалобы),
вход администратора (категории, города, пользователи) и страницу-гайд. Нужны запущенные API,
PostgreSQL и демо-данные (`dotnet run --project backend/src/Doska.Api -- seed`); dev-сервер Vite
Playwright поднимает сам. Тест не изменяет данные.

## Очистка медиа

```
dotnet run --project backend/src/Doska.Api -- cleanup-media
```

Удаляет каталоги `media/photos/<adId>`, для которых нет объявления, и файлы, не привязанные
к `ad_photos`. Команда идемпотентна: повторный запуск ничего не меняет.

## Непрерывная интеграция

Workflow `.github/workflows/ci.yml` запускается на push в `master`, на pull request и вручную
и выполняет три джобы:

- **Backend** — поднимает PostgreSQL 17 на порту 5433, собирает решение, проверяет, что модель
  EF Core не разошлась с миграциями (`dotnet ef migrations has-pending-model-changes`), применяет
  миграции к чистой базе и запускает `dotnet test`.
- **Frontend** — устанавливает зависимости по lockfile, запускает `pnpm test` и `pnpm build`.
- **Production images** — после зелёных тестов проверяет `docker-compose.prod.yml` и собирает
  production-образы API и веб-сервера.

Локальное воспроизведение проверок:

```
docker compose up -d postgres
dotnet build backend/Doska.sln
dotnet test backend/Doska.sln
pnpm --dir frontend test
pnpm --dir frontend build
```

Если в вашем окружении `dotnet test` не работает из-за ограничений среды, тесты можно запустить
напрямую: `backend/tests/Doska.Tests/bin/Debug/net10.0/Doska.Tests.exe`.

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

## Письма локально

Письма (подтверждение email, сброс пароля, решения модерации, напоминание о сроке) в Development
по умолчанию сохраняются как файлы `.eml` в `backend/src/Doska.Api/media/outbox/`. Откройте файл
почтовым клиентом или текстовым редактором — внутри ссылка подтверждения/сброса или причина решения.

Режим задаётся настройкой `Email:Mode`:

- `File` — файлы в `Email:OutboxPath` (по умолчанию `media/outbox`); режим Development по умолчанию.
- `Log` — письма пишутся в журнал API.
- `Smtp` — реальная отправка; требует `Email:Host`, иначе приложение не стартует.

### Локальный SMTP с веб-инбоксом (Mailpit)

Без реальных креденшелов письма удобно смотреть в Mailpit:

```
docker compose up -d mailpit
```

Затем запустите API с переключением на SMTP (веб-инбокс — http://localhost:8025):

```
$env:Email__Mode="Smtp"
$env:Email__Host="localhost"
$env:Email__Port="1025"
$env:Email__UseStartTls="false"
dotnet run --project backend/src/Doska.Api
```

### Реальный SMTP

Когда появятся креды, задайте `Email__Mode=Smtp`, `Email__Host`, `Email__Port`, `Email__UserName`,
`Email__Password`, `Email__From` (в `.env` при развёртывании). Код и тесты при этом не меняются.

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
