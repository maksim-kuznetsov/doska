# Proposal

## Why

Внешний вход сейчас реализован только через Google и прошит в модель Google-специфично: колонка `users.google_subject`, `GoogleAuthService`, статические события `GoogleAuthentication`, флаг `googleEnabled` и эндпоинты `/api/auth/google/*`. Нужен вход через Яндекс ID — Яндекс основной провайдер для российской аудитории. Копирование Google-слоя под Яндекс удвоит специфичный код и сделает каждый следующий провайдер дороже, поэтому сначала вводится единый слой внешних входов, а Яндекс становится первым новым провайдером на нём.

## What Changes

- **BREAKING** (внутренняя схема): идентичности внешних входов выносятся из `users.google_subject` в таблицу `external_logins` (`user_id`, `provider`, `subject`, уникальность по `(provider, subject)` и `(user_id, provider)`). Существующие Google-привязки переносятся в новую таблицу, колонка и её индекс удаляются.
- `GoogleAuthService` заменяется на провайдеро-агностичный `ExternalAuthService`; правила (первый вход, автопривязка по email, конфликт, блокировка, имя) описываются один раз и применяются к любому провайдеру.
- Вводится реестр провайдеров: `GET /api/auth/providers` отдаёт список `[ { id, name, enabled } ]` вместо единственного `googleEnabled`; эндпоинты становятся `GET /api/auth/{provider}/{start,link}`, callback и коды ошибок обобщаются.
- К одному аккаунту можно привязать несколько провайдеров (по одному каждого вида).
- Добавляется провайдер Яндекс ID: OAuth 2.0, обязательный скоуп `login:email`; email из Яндекса считается подтверждённым и используется для автопривязки; в отображаемое имя переносится `real_name`/`display_name`; телефон не запрашивается и не хранится.
- Интерфейс строит кнопки входа по списку провайдеров, профиль показывает состояние привязок; политика конфиденциальности и условия использования дополняются сведениями о Яндексе.
- Добавляются настройки `Yandex__ClientId`/`Yandex__ClientSecret` и шаги подключения в документации деплоя.

## Capabilities

### New Capabilities

Нет — единый слой внешних входов описывается требованиями существующей способности `user-accounts`.

### Modified Capabilities

- `user-accounts`: требования внешнего входа переписываются с Google-специфичных на провайдеро-агностичные; добавляются требования и сценарии для Яндекса (обязательный email, доверие подтверждённости email, заполнение имени, несколько привязок на аккаунт).
- `legal-documents`: политика конфиденциальности и условия использования описывают обработку данных, получаемых от Яндекса, наряду с Google.

## Impact

- `backend/src/Doska.Domain`: сущность `User` — удаление `GoogleSubject`, `CreateWithGoogle`, `LinkGoogle`, `ApplyGoogleDisplayName`; новая сущность `ExternalLogin`.
- `backend/src/Doska.Application`: `GoogleAuthService` → `ExternalAuthService`, `AuthenticatedUser` (привязки вместо `IsGoogleLinked`).
- `backend/src/Doska.Api`: `Program.cs` (регистрация провайдеров), `AuthController`, инфраструктура `GoogleAuthentication`/`GoogleSignInOptions`/`GoogleRedirects`/`GooglePublicHostMiddleware` → провайдеро-агностичные.
- `backend/src/Doska.Infrastructure`: новая миграция — создание `external_logins`, перенос данных из `users.google_subject`, удаление колонки.
- Зависимости: добавляется пакет провайдеров OAuth (`AspNet.Security.OAuth.Providers` / `AspNet.Security.OAuth.Yandex`) под `net10.0`; Google остаётся на встроенном `Microsoft.AspNetCore.Authentication.Google`.
- `frontend/src`: хранилище `auth` (список провайдеров), `LoginView`, `ProfileView`, `PrivacyPolicyView`, `TermsOfServiceView`, маппер кодов ошибок.
- Тесты: `backend/tests/Doska.Tests` (Google API-тесты обобщаются, добавляются Яндекс) и `frontend/src` (Vitest).
- Документация: `README.md`, `deploy/DEPLOY.md`.

## Вне рамок

- Прочие провайдеры (VK ID, Telegram, Сбер ID и другие): слой готовит почву, но подключаются они отдельными изменениями.
- Телефон как идентификатор и хранение номера телефона — не запрашивается.
- Перевод Google на OpenID Connect и отказ от встроенного `AddGoogle`.
- Вход без пароля без внешнего провайдера: магические ссылки, коды по email/SMS, passkeys.
- Отвязка провайдера из интерфейса (в текущем поведении отвязка идёт по обращению на контактный email).
