# Proposal

## Why

Production-smoke (`docker compose -f docker-compose.prod.yml`, отдельный проект и порт) прошёл: HTML отдаётся с `no-cache`, хешированные ассеты — с `immutable`, миграции применяются, фото загружается, уменьшается и отдаётся через `X-Accel-Redirect`. При этом в логах API видны два эксплуатационных дефекта: Npgsql не находит `libgssapi_krb5.so.2` в runtime-образе, а ключи Data Protection лежат внутри контейнера и теряются при пересоздании — все сессии становятся недействительными после деплоя.

## What Changes

- В runtime-образ API добавляется системная библиотека `libgssapi-krb5-2`, чтобы Npgsql не писал ошибку при инициализации GSSAPI.
- Каталог ключей Data Protection выносится в именованный том `data-protection-keys`, поэтому сессии переживают пересоздание контейнера и откат.
- Добавляется `.dockerignore`, чтобы контекст сборки не включал `node_modules`, `bin`/`obj`, `.git`, медиа и локальные артефакты.
- Проверка: повторная сборка API, запуск production-стека, отсутствие ошибки GSSAPI, сохранение cookie-сессии после пересоздания API.

## Capabilities

### New Capabilities

Нет.

### Modified Capabilities

Нет. Требования `openspec/specs/` не меняются; change устраняет дефекты эксплуатации, поэтому объявляет `skip_specs: true`.

## Impact

- `backend/Dockerfile` — установка системной библиотеки в runtime-слой.
- `docker-compose.prod.yml` — том для ключей Data Protection.
- `.dockerignore` — состав контекста сборки.
- Схема БД, API и интерфейс не меняются.

## Вне рамок

- Переезд ключей Data Protection в общий провайдер (Redis, файловое хранилище вне хоста).
- Настройка Kerberos-аутентификации как таковой: библиотека нужна только чтобы Npgsql не деградировал с ошибкой в логах.
- CI и автоматизация деплоя.