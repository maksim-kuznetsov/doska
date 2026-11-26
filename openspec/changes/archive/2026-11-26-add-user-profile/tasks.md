# Tasks

## 1. Домен и схема

- [x] 1.1 Добавить в `User` необязательные `DisplayName` и `Contacts` и доменный метод `UpdateProfile(displayName, contacts, now)` (обрезка пробелов, пусто → `null`, вызов `MarkUpdated`); проверить юнит-тестом домена в `tests/Doska.Tests` (`dotnet test`).
- [x] 1.2 Настроить столбцы в `UserConfiguration` (`display_name` ≤ 100, `contacts` ≤ 500, nullable) и создать миграцию EF Core `AddUserProfile`; проверить `dotnet build` и наличием файла миграции с новыми столбцами.

## 2. Приложение

- [x] 2.1 Расширить `AuthenticatedUser` полями `DisplayName` и `Contacts` и обновить `AuthenticatedUser.From(User)`; проверить сборкой `dotnet build`.
- [x] 2.2 Добавить `ProfileService` (`UpdateAsync`, `ChangePasswordAsync`) с проверкой длин (контакты ≤ 500, имя ≤ 100) и текущего пароля через `IPasswordHasher`, переиспользуя `AuthService.ValidatePassword`; проверить тестами (`dotnet test`).

## 3. API

- [x] 3.1 Дополнить `UserResponse` полями `DisplayName` и `Contacts`; проверить тестом, что `GET /api/auth/me` возвращает их.
- [x] 3.2 Добавить `AccountController` (`PUT /api/account/profile`, `POST /api/account/password`) с `[Authorize]` и `RateLimitPolicies.Sensitive`; проверить тестами API: успех, 400 при длине контактов и слабом пароле, 401 без входа, отказ при неверном текущем пароле.

## 4. Интерфейс: API-клиент и хранилище

- [x] 4.1 Добавить `displayName`/`contacts` в тип `CurrentUser` и методы `api.account.updateProfile`/`changePassword`; проверить тестом `api/__tests__/client.spec.ts` (`pnpm test`).
- [x] 4.2 Обновлять `user` в `stores/auth.ts` из ответа после сохранения профиля; проверить тестом хранилища.

## 5. Интерфейс: страница профиля

- [x] 5.1 Создать `ProfileView.vue` с формами профиля и смены пароля (переиспользуя `PasswordHints`), маршрут `/profile` с `requiresAuth` и пункт «Профиль» в `UserMenu.vue`; проверить `pnpm test` и сборкой `pnpm build`.
- [x] 5.2 Покрыть `ProfileView` тестом (`views/__tests__/ProfileView.spec.ts`): сохранение профиля, смена пароля и показ ошибок.

## 6. Интерфейс: предзаполнение объявления

- [x] 6.1 В `AdFormView.vue` подставлять в пустое поле «Контакты» отображаемое имя и контакты профиля, соединённые одним пробелом (пустые части пропускаются); проверить тестом, что значение подставляется в формате «имя контакты» и остаётся редактируемым.

## 7. Интеграционная проверка

- [x] 7.1 Прогнать `dotnet test` из `backend` и `pnpm test` из `frontend/`; убедиться, что все тесты зелёные.
- [x] 7.2 Проверить сценарий вручную через `docker compose`: сохранить контакты в профиле, открыть форму нового объявления и убедиться, что поле «Контакты» предзаполнено.

## Workflow follow-up

- Архивировать change после ревью.
- Предложить коммит на русском языке.
