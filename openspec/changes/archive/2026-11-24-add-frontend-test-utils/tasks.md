# Tasks

## 1. Общие хелперы

- [x] 1.1 Создать `frontend/src/__tests__/helpers/apiClientMock.ts` (класс `ApiError` и фабрика модуля-заглушки) и убедиться, что файл не подхватывается как тест: `pnpm --dir frontend test` даёт прежнее число тестов.
- [x] 1.2 Создать `frontend/src/__tests__/helpers/mount.ts` с `mountWithRouter`; проверить переносом одного спека и запуском `pnpm --dir frontend test`.
- [x] 1.3 Создать хелперы `read()`/`repoRoot`, `createInstallPromptEvent`, `photo()` и заглушку `HTMLDialogElement`; проверить переносом одного спека на каждый и запуском `pnpm --dir frontend test`.

## 2. Перевод спеков

- [x] 2.1 Перевести девять спеков с моком `ApiError` (`AdminCitiesView`, `AdminUsersView`, `AdminOutboxView`, `AdminCategoriesView`, `AdDetailView`, `AdEditView`, `ConfirmEmailView`, `ResetPasswordView`, `ResendConfirmationView`) на общий хелпер; `pnpm --dir frontend test`.
- [x] 2.2 Перевести спеки с паттерном `mountView` (`ItemCard`, `CategoryStrip`, `CategoryMenu`, `UserMenu`, `PromoBanner`, `AdDetailView`, `AdEditView`, `ResetPasswordView`, `ConfirmEmailView`, `ResendConfirmationView`) на `mountWithRouter`; `pnpm --dir frontend test`.
- [x] 2.3 Перевести конфигурационные тесты (`deployment`, `pwaConfig`, `staticAssets`) и PWA/фото-тесты (`pwa`, `PwaPrompts`, `PhotoViewer`, `ImageGallery`) на общие хелперы; `pnpm --dir frontend test`.

## 3. Проверка

- [x] 3.1 Убедиться, что локальных копий моков и фабрик не осталось (поиск по `frontend/src`), и что `pnpm --dir frontend test` зелёный с прежним числом тестов.

## Workflow follow-up

- Архивировать изменение после проверок.
