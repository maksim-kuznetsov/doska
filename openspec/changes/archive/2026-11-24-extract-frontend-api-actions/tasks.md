# Tasks

## 1. Общий слой

- [x] 1.1 Создать `frontend/src/api/errors.ts` с функцией `messageOf(cause, fallback)` и проверить сборку: `pnpm --dir frontend build`.
- [x] 1.2 Создать `frontend/src/composables/useApiAction.ts` с `run(action, success, fallback)` и необязательным `reload`; покрыть unit-тестом `frontend/src/composables/__tests__/useApiAction.spec.ts` и проверить `pnpm --dir frontend test`.

## 2. Перевод представлений

- [x] 2.1 Заменить локальный `messageOf` в `AdminCitiesView`, `AdminCategoriesView`, `AdminUsersView`, `AdminOutboxView` на общий; `pnpm --dir frontend test` — спеки этих представлений зелёные.
- [x] 2.2 Перевести `run(...)` на `useApiAction` в `AdminCitiesView`, `AdminCategoriesView`, `AdminUsersView`, `MyAdsView`, `ModerationQueueView`; `pnpm --dir frontend test`.
- [x] 2.3 Заменить инлайновые тернарники `cause instanceof ApiError ? ...` в остальных представлениях (auth, `HomeView`, `AdDetailView`, `AdFormView`, `AdEditView`, `MyAdsView`, `ModerationQueueView`, `ComplaintsQueueView`) на `messageOf`; `pnpm --dir frontend test`.

## 3. Проверка

- [x] 3.1 Убедиться, что `instanceof ApiError ?` вне `frontend/src/api/errors.ts` не осталось (поиск по `frontend/src`), и что сборка проходит: `pnpm --dir frontend build`.

## Workflow follow-up

- Архивировать изменение после проверок.
