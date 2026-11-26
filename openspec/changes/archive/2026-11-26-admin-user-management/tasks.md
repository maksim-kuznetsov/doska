# Tasks

## 1. Домен и слой приложения (бэкенд)

- [x] 1.1 Добавить в `Doska.Domain/Entities/User.cs` метод `SetEmailConfirmed(bool isConfirmed, DateTimeOffset now)`, выражающий установку признака подтверждения email, и перевести существующий `ConfirmEmail(now)` на него; добавить доменный тест (в `ModerationDomainTests.cs` рядом с проверками блокировки) на confirm/unconfirm. Проверка: `dotnet test` из `backend` — тест домена проходит, поведение `ConfirmEmail` не изменилось.
- [x] 1.2 Дополнить представление администратора: в `Doska.Application/Accounts/UserService.cs` добавить в `AdminUserView` поля `DisplayName` и `Contacts` и заполнять их в `GetListAsync`. Проверка: проект собирается (`dotnet build`), поля присутствуют в запросe списка.
- [x] 1.3 Добавить в `UserService` методы `GetByIdAsync(userId, ct)` (404, если нет) и `UpdateAsync(...)` для полного набора свойств (роль, доверие, блокировка, подтверждение email, имя, контакты) с применением только изменившихся свойств через `ChangeRole`, `GrantTrust`/`RevokeTrust`, `Block`/`Unblock`, `SetEmailConfirmed`, `UpdateProfile`. Проверка: `dotnet test` (бэкенд) собирается, логика покрыта тестами из 1.5.
- [x] 1.4 В `Doska.Api/Contracts/UserAccountContracts.cs` расширить `AdminUserResponse` (`DisplayName`, `Contacts`), добавить контракт запроса на полную правку; в `AdminUsersController` добавить `GET /api/admin/users/{id}` и `PUT /api/admin/users/{id}`, удалить `PUT /{id}/role`. Проверка: `dotnet build`; маршруты видны в swagger/списке эндпоинтов контроллера.
- [x] 1.5 Обновить `tests/Doska.Tests/UserAccountApiTests.cs`: перевести проверки смены роли и прав на новый `PUT`, добавить тесты полного обновления (роль/доверие/блокировка/подтверждение/имя/контакты отражаются в `GET`), отказ `400` на недопустимую роль, `403` для модератора и `404` для неизвестного пользователя. Проверка: `dotnet test` из `backend` — все тесты бэкенда проходят.

## 2. Компонент дизайн-системы (интерфейс)

- [x] 2.1 Добавить `frontend/src/components/CheckboxField.vue` (подпись, `input[type=checkbox]`, связанные id, подсказка/ошибка) по образцу `TextField.vue`. Проверка: `pnpm test` в `frontend/` проходит; компонент собирается без ошибок типов.
- [x] 2.2 Добавить пример `CheckboxField` на страницу-гайд `frontend/src/views/DesignSystemGuide.vue` и при необходимости обновить `DesignSystemGuide.spec.ts`. Проверка: `pnpm test` в `frontend/` и визуальная проверка на `/design-system` в режиме `Development`.

## 3. Страницы администратора (интерфейс)

- [x] 3.1 В `frontend/src/api/client.ts` добавить `displayName` и `contacts` в `AdminUserView`, заменить `setRole` на `get(id)` и `update(id, payload)`. Проверка: `pnpm test` собирается; типы соответствуют ответу API.
- [x] 3.2 Переписать `frontend/src/views/AdminUsersView.vue` в обзорный список: свойства пользователя и ссылка «Редактировать» на `admin/users/:id`, без полей и кнопок правки; обновить `AdminUsersView.spec.ts` (есть ссылка на редактирование, нет `select`/кнопок действий). Проверка: `pnpm test` в `frontend/`.
- [x] 3.3 Добавить `frontend/src/views/AdminUserEditView.vue` (загрузка по id, форма из роли, имени, контактов и трёх флажков, email только для чтения, сохранение, обработка 404) и маршрут `admin/users/:id` с `meta.requiresAdmin` в `frontend/src/router/index.ts`; добавить `__tests__` на загрузку и сохранение. Проверка: `pnpm test` в `frontend/`; переход по ссылке из списка открывает форму с текущими значениями.
- [x] 3.4 Обновить `frontend/src/router/__tests__/router.spec.ts` проверкой маршрута `admin/users/:id`. Проверка: `pnpm test` в `frontend/`.

## 4. Интеграционная проверка

- [x] 4.1 Прогнать `dotnet test` (из `backend`) и `pnpm test` (из `frontend/`) и убедиться, что оба набора зелёные. Проверка: команды завершаются успешно, отчёт без падающих тестов.
- [x] 4.2 Проверить сценарий вручную на `docker compose`: под администратором список показывает свойства и ссылку, правка пользователя сохраняет роль/доверие/блокировку/подтверждение/имя/контакты, блокировка сразу завершает активную сессию пользователя. Проверка: описанные действия дают ожидаемый результат.
