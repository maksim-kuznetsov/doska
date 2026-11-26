# Tasks

## 1. Домен и схема данных

- [x] 1.1 Добавить в `User` признак `HasPassword`, nullable `GoogleSubject`, доменные методы `LinkGoogle`/`SetPassword` и фабрику `CreateWithGoogle`; проверить `dotnet test --filter UserProfileDomainTests` и новыми доменными тестами: аккаунт без пароля, подтверждённый email, привязка, запрет перепривязки.
- [x] 1.2 Обновить `UserConfiguration`: `google_subject` длиной 255 с уникальным индексом, `has_password` обязательным; сгенерировать миграцию `AddGoogleSignIn` командой `dotnet tool run dotnet-ef migrations add AddGoogleSignIn --project src/Doska.Infrastructure --startup-project src/Doska.Api --output-dir Migrations` из `backend` и проверить, что в файле миграции есть обе колонки и индекс.
- [x] 1.3 Расширить `ModelConfigurationTests` проверкой уникальности `GoogleSubject` и значения по умолчанию `HasPassword = true`; проверка: `dotnet test --filter ModelConfigurationTests`.

## 2. Прикладная логика аккаунтов

- [x] 2.1 Создать `Doska.Application.Auth.GoogleAuthService` с разрешением идентичности в порядке subject → email → новый аккаунт, проверками `email_verified`, блокировки, конфликтов и обработкой гонки через уникальные индексы; покрыть `GoogleAuthServiceTests` сценарии: новый пользователь, авто-привязка, вход по subject при изменившемся email, заблокированный, чужой subject, неподтверждённый email.
- [x] 2.2 Расширить `AuthenticatedUser` полями `HasPassword` и `IsGoogleLinked`, обновить `UserResponse.From`; проверить `ClaimsPrincipalExtensionsTests` и `UserAccountApiTests`.
- [x] 2.3 Запретить вход по паролю при `HasPassword = false` с общим сообщением; проверить интеграционным тестом в `UserAccountApiTests` на аккаунте, созданном через Google.
- [x] 2.4 Научить `ProfileService.ChangePasswordAsync` устанавливать первый пароль без текущего, а при наличии пароля по-прежнему требовать текущий; обновить `ChangePasswordRequest` и проверить `UserProfileApiTests` (успех без текущего, успех с текущим, неверный текущий, слабый пароль).
- [x] 2.5 Зарегистрировать `GoogleAuthService` в DI и обновить `TestData`/тестовые конструкторы `AuthenticatedUser`; проверка: `dotnet build backend/Doska.sln`.

## 3. Google OAuth в API

- [x] 3.1 Подключить пакет `Microsoft.AspNetCore.Authentication.Google` к `Doska.Api`, добавить `GoogleOptions` (`ClientId`, `ClientSecret`, `IsConfigured`), условную регистрацию Google-хендлера с временной cookie-схемой `GoogleExternal` и correlation-cookie `SameSite=Lax`/`HttpOnly`; проверка: `dotnet build backend/Doska.sln` и запуск API без кредов не падает.
- [x] 3.2 Реализовать извлечение `sub`/`email`/`email_verified` из Google-ответа и `OnTicketReceived`: разрешение пользователя, вход в `doska.session`, привязка в режиме `link`, явная проверка текущей сессии через `AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)` и совпадения с `linkUserId`, `HandleResponse()`; проверить сценарии через `GoogleAuthServiceTests` и тест стартового challenge.
- [x] 3.3 Добавить безопасный разбор `redirect` (только локальный путь, отказ от `//`, `/\`, внешних URL) и коды ошибок `denied/blocked/conflict/unverified/invalid/session/failed`; покрыть юнит-тестами `GoogleRedirectTests` и проверить, что внешний адрес заменяется на `/`.
- [x] 3.4 Добавить в `AuthController` публичный `GET /api/auth/providers`, `GET /api/auth/google/start` и `GET /api/auth/google/link`; при выключенном провайдере старт отвечает 503 с ProblemDetails; проверить `GoogleAuthApiTests`: `providers.google=false`, старт 503, анонимная привязка 401.
- [x] 3.5 Применить `RateLimitPolicies.Sensitive` к обоим стартовым эндпоинтам; добавить тест с пониженным лимитом на ответ 429 и проверить настройкой тестовой фабрики.
- [x] 3.6 Добавить тестовую фабрику с `Google:ClientId`/`Google:ClientSecret` и проверить, что `GET /api/auth/google/start` отвечает 302 на `accounts.google.com`, а `GET /api/auth/google/link` для вошедшего пользователя также начинает challenge.

## 4. Интерфейс

- [x] 4.1 Расширить `CurrentUser` и `api.auth` методами `providers`, добавить `frontend/src/api/googleAuth.ts` с построением URL старта/привязки и переводом кодов ошибок; проверить `pnpm test` новым `googleAuth.spec.ts`.
- [x] 4.2 Добавить в `auth` store `googleEnabled`, параллельную загрузку `/auth/me` и `/auth/providers` с независимой обработкой ошибок; проверить `auth.spec.ts` (провайдер выключен/недоступен, вход не ломается).
- [x] 4.3 Добавить на `LoginView` кнопку «Войти через Google» только при `googleEnabled`, ссылку с текущим `redirect` и сообщение об ошибке из query; проверить новым `LoginView.spec.ts`.
- [x] 4.4 Добавить в `ProfileView` статус привязки, ссылку «Привязать Google» при доступном провайдере, обработку `?google=linked/error`, а при `hasPassword=false` — форму «Задать пароль» без поля текущего пароля и перезагрузку сведений о пользователе после успеха; проверить `ProfileView.spec.ts`.
- [x] 4.5 Обновить фикстуры `CurrentUser` во всех тестах интерфейса новыми полями; проверка: `pnpm test` и `pnpm build` в `frontend/`.

## 5. Документация и развёртывание

- [x] 5.1 Добавить `Google__ClientId`/`Google__ClientSecret` в `.env.example` и `docker-compose.prod.yml`; проверить `docker compose -f docker-compose.prod.yml config` с заданными обязательными переменными — новые ключи присутствуют в окружении `api`.
- [x] 5.2 Описать в README локальную настройку Google: креды через переменные окружения PowerShell, redirect URI `http://localhost:5000/api/auth/google/callback`, поведение прокси; проверить, что команды и адреса совпадают с `vite.config.ts` и `Program.cs`.
- [x] 5.3 Дописать в `deploy/DEPLOY.md` создание OAuth-клиента, production redirect URI `https://<домен>/api/auth/google/callback`, переменные окружения, перезапуск API и безопасное хранение секрета; проверить таблицу переменных и раздел настройки Google.

## 6. Интеграционная проверка

- [x] 6.1 Запустить `dotnet test` из `backend` и убедиться, что все тесты проходят, включая новые сценарии Google.
- [x] 6.2 Запустить `pnpm test` и `pnpm build` в `frontend/` и убедиться, что тесты и сборка проходят.
- [x] 6.3 Проверить запуск `docker compose up -d postgres`, миграцию на чистой базе и старт API без Google-кредов; приложение должно работать, а `/api/auth/providers` возвращать `google=false`.

## Workflow follow-up

- После получения тестовых Google-кредов вручную проверить полный поток: вход нового пользователя, автоматическая привязка существующего и явная привязка из профиля.
- Архивировать изменение после проверки и предложить коммит на русском в стиле «Архивировать change add-google-sign-in».