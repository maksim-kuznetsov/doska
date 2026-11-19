# Design

## Context

See `proposal.md` — Why. Результаты smoke-прогона:

- `docker compose -p doska-prod-smoke -f docker-compose.prod.yml up` поднял PostgreSQL, API и nginx; `/api/health` вернул `Production`.
- `/` отдаётся с `Cache-Control: no-cache`, `/assets/index-*.js` — с `public, max-age=31536000, immutable`.
- Миграции применились: в БД есть `ads`, `users`, `ad_photos`, `__EFMigrationsHistory`.
- Загрузка фото через production-стек сработала: SkiaSharp на Linux создал превью, оригинал и превью отдаются nginx (887 и 736 байт).
- Логи API содержат `Cannot load library libgssapi_krb5.so.2` и предупреждение, что ключи Data Protection не переживут контейнер.

## Goals / Non-Goals

**Goals:**

- Убрать ошибку GSSAPI из логов и обеспечить полный набор ожидаемых возможностей Npgsql.
- Сохранять ключи Data Protection между пересозданиями контейнера и откатами.
- Уменьшить контекст Docker-сборки.

**Non-Goals:**

- Внешний провайдер ключей и горизонтальное масштабирование.
- Керberos-аутентификация в PostgreSQL.
- Изменение приложения или схемы.

## Decisions

### 1. Установить `libgssapi-krb5-2` в runtime-образ

Npgsql динамически загружает GSSAPI; в `mcr.microsoft.com/dotnet/aspnet:10.0` библиотеки нет. Установка пакета в runtime-слой (`apt-get install --no-install-recommends libgssapi-krb5-2`) убирает ошибку и оставляет поддержку Kerberos доступной. Альтернатива — отключить GSS в Npgsql — нежелательна: это скрывает возможность и не гарантирует отсутствие загрузки библиотеки.

### 2. Том для ключей Data Protection

Ключи по умолчанию пишутся в `/root/.aspnet/DataProtection-Keys` внутри контейнера. Именованный том `data-protection-keys` монтируется в этот каталог у сервиса `api`, поэтому cookie-сессии и antiforgery-токены переживают `docker compose up --build` и откат. Альтернатива — задать свой путь через `DataProtection__KeyRingPath`; выбран том на существующем пути, чтобы не добавлять настройку приложения.

### 3. `.dockerignore` для контекста

В контекст попадали `frontend/node_modules`, `bin`/`obj`, `.git`, `media`, `openspec`. Исключение этих каталогов ускоряет сборку и не переносит локальные артефакты в образ. Файл не влияет на runtime.

## Risks / Trade-offs

- **Том ключей привязан к хосту** → при переезде на другой сервер сессии инвалидируются; для первой версии это ожидаемо, внешний провайдер — отдельная задача.
- **Дополнительный системный пакет** → незначительный рост runtime-образа.
- **`.dockerignore` может исключить нужный файл** → список ограничен сгенерированными и локальными каталогами; сборка проверяется smoke-прогоном.

## Migration Plan

1. Пересобрать образ API: `docker compose -f docker-compose.prod.yml build api`.
2. Перезапустить стек; убедиться, что ошибка GSSAPI исчезла и `/api/health` отвечает.
3. Войти, пересоздать контейнер API и проверить, что сессия сохраняется.
4. Откат — вернуть предыдущую ревизию; том ключей и данные не затрагиваются.