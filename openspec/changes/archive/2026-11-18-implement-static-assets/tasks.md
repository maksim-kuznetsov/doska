# Tasks

## 1. Заголовки кеширования

- [x] 1.1 Добавить в `deploy/nginx.conf` правило `Cache-Control: no-cache` для `index.html`. Проверка: тест конфигурации.
- [x] 1.2 Добавить долгий кеш и `immutable` для каталога `/assets/`. Проверка: тест конфигурации.

## 2. Тесты

- [x] 2.1 Тест проверяет no-cache для HTML, долгий кеш статики и подключение исходного модуля в dev-режиме. Проверка: `pnpm --dir frontend test`.
- [x] 2.2 Убедиться, что сборка даёт хешированные имена и манифест. Проверка: `pnpm --dir frontend build`.

## 3. Проверка

- [x] 3.1 `dotnet build backend/Doska.sln`, тесты бэкенда, `pnpm --dir frontend test`, `pnpm --dir frontend build`.
- [x] 3.2 `openspec validate implement-static-assets`.