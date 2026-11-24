# Design

## Context

См. `proposal.md` — Why. Слои решения: `Domain` → `Application` → `Infrastructure` → `Api`. Пути хранения задаются в опциях (`MediaOptions`, `EmailOptions`) и используются и в `Infrastructure` (Media, Email), и в `Api` (`MediaController`). Коды HTTP — забота слоя `Api`.

## Goals / Non-Goals

**Goals:**

- Один резолвер пути «настройка → абсолютный путь».
- Явное сопоставление `ServiceErrorKind` → HTTP-код.
- Единое построение claim'ов и принципала сессии.

**Non-Goals:**

- Не убирать зависимость `Doska.Application` от EF Core.
- Не менять HTTP-коды, набор claim'ов и логику проверки сессии.

## Decisions

- **Резолвер пути.** Статический хелпер `PathResolver.Resolve(configured, contentRoot)` в `Doska.Infrastructure` (например, `Common/`). Используют `Infrastructure` (Media/Email) и `Api` (`MediaController`); `Api` уже ссылается на `Infrastructure`. Альтернатива — интерфейс в `Application.Abstractions` отклонена как избыточная для чистой функции.
- **Ошибка → HTTP.** Явное сопоставление `ServiceErrorKind` → `StatusCodes` (по именам членов перечисления) рядом с `ServiceExceptionHandler`. Альтернатива — оставить приведение к `(int)` отклонена: хрупко при изменении порядка enum.
- **Claims.** Единый построитель в существующем `Doska.Api/Infrastructure/ClaimsPrincipalExtensions.cs`; используется в `AuthController` (вход/регистрация) и `Program` (проверка сессии). Альтернатива — отдельный сервис отклонена как избыточная.
- **Проверка пользователя в сессии.** Логику из `OnValidatePrincipal` свести с `AuthService.GetAsync`, чтобы не было двух источников истины; число запросов к базе не увеличивается.

## Risks / Trade-offs

- Тип claim'а роли (`ClaimTypes.Role`) может разойтись → единый источник + существующие тесты аутентификации.
- `PathResolver` для абсолютных путей (production outbox) → покрыть тестом на абсолютный и относительный путь.
- Сведение проверки сессии может изменить поведение при блокировке → сверить с `AuthService.GetAsync` и тестами.
