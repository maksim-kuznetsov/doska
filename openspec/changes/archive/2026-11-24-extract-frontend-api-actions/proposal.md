# Proposal

## Why

Обработка ошибок и однотипных действий над API на клиенте скопирована по всему приложению: `messageOf` продублирован в четырёх admin-представлениях, инлайновое `cause instanceof ApiError ? cause.message : '<текст>'` встречается в 21 месте, а хелпер `run(action, message)` — в пяти представлениях. Любая правка формата ошибки или уведомления требует согласованных изменений в ~25 местах и легко расходится.

## What Changes

- Ввести единый клиентский слой для действий над API: преобразование ошибки в текст (`ApiError` → её `message`, иначе — переданный fallback) и обёртку «выполнить действие, показать уведомление, перечитать данные, показать ошибку».
- Заменить дублирующийся код на новый слой:
  - `messageOf` в `AdminCitiesView`, `AdminCategoriesView`, `AdminUsersView`, `AdminOutboxView`;
  - инлайновые тернарники в 21 месте (auth-представления, `HomeView`, `AdDetailView`, `AdFormView`, `AdEditView`, `MyAdsView`, `ModerationQueueView`, `ComplaintsQueueView`);
  - `run(action, message)` в `AdminCitiesView`, `AdminCategoriesView`, `AdminUsersView`, `MyAdsView`, `ModerationQueueView`.
- Сохранить наблюдаемое поведение: те же тексты fallback и уведомления, тот же порядок «сбросить сообщения → выполнить → показать итог/ошибку».

## Capabilities

Изменение не меняет наблюдаемое поведение системы — это чистый рефакторинг клиентского кода. Спецификации не создаются и не изменяются (`skip_specs: true`).

### New Capabilities

- Нет.

### Modified Capabilities

- Нет.

## Impact

- Код: `frontend/src/views/*` (13 файлов), новый общий модуль (`frontend/src/composables/` или `frontend/src/api/`).
- Тесты: `frontend/src/views/__tests__/*` — проверки сообщений об ошибках остаются без изменений.
- Публичный контракт API и бэкенд не затрагиваются.

## Вне рамок

- Форматирование дат (`formatDate` и расхождение «дата/дата+время») — отдельное изменение.
- Общие тестовые хелперы и моки — изменение `add-frontend-test-utils`.
- Загрузка справочников (города/разделы) в представлениях — отдельная задача.
- Токены и разметка дизайн-системы — изменение `align-design-system-tokens`.
