# Tasks

## 1. Runtime-образ

- [x] 1.1 Установить `libgssapi-krb5-2` в runtime-слой `backend/Dockerfile`. Проверка: пересборка образа без ошибок.
- [x] 1.2 Добавить `.dockerignore` с исключением сгенерированных и локальных каталогов. Проверка: контекст сборки уменьшился, сборка успешна.

## 2. Ключи Data Protection

- [x] 2.1 Добавить именованный том `data-protection-keys` и смонтировать его в `/root/.aspnet/DataProtection-Keys` сервиса `api`. Проверка: том создан, каталог доступен.

## 3. Проверка

- [x] 3.1 Пересобрать API и поднять production-стек; `/api/health` отвечает, в логах нет `libgssapi_krb5`.
- [x] 3.2 Проверить, что после пересоздания контейнера API активная cookie-сессия сохраняется.
- [x] 3.3 `openspec validate harden-production-runtime`.