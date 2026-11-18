# Design

## Context

See `proposal.md` — Why. Текущее состояние, задающее подход:

- В репозитории есть предметные сущности (`User`, `Ad`, `AdComplaint`, `Category`, `City`, `UserToken`), конфигурации EF Core, миграция `InitialSchema` и шаблон API с `MapControllers()`; прикладного слоя и контроллеров, кроме `HealthController`, нет.
- `AppDbContext` уже реализует подстановку `created_at`/`updated_at` через `TimeProvider` и `UseSnakeCaseNamingConvention`; роли, статусы и причины уже есть в схеме, поэтому менять миграции не требуется.
- Тесты `dotnet test` используют `Microsoft.Testing.Platform`; часть тестов пропускается, если PostgreSQL недоступен.

## Goals / Non-Goals

**Goals:**

- Довести требования `ad-moderation` до исполняемых: очередь, одобрение, отклонение, скрытие, жалобы, доверие.
- Дать минимально достаточные опоры (`user-accounts`, часть `classified-ads`, `notifications`), чтобы сценарий проходил целиком.
- Сохранить роли и правила предметной области на сервере; клиент только отражает доступность.

**Non-Goals:**

- Новые таблицы и миграции: схема уже покрывает все нужные поля.
- Фотографии, автоархив по расписанию, напоминания о сроке, полноценный `ad-search`, `design-system`, `static-assets`.
- Сброс пароля по ссылке и подтверждение email письмом в полном объёме.

## Decisions

### 1. Аутентификация: зашифрованный cookie-тикет, а не таблица сессий

Используется `Microsoft.AspNetCore.Authentication.Cookies`: claims `sub` (id) и `role`; cookie `httpOnly`, `SameSite=Lax`, `SecurePolicy=SameAsSitePolicy`, срок 7 дней со скольжением. Альтернатива — таблица сессий (`user_sessions`) с непрозрачным идентификатором: точнее управляется отзыв, но требует миграции и новых таблиц. Тикет на Data Protection удовлетворяет требованию платформы о cookie-сессии (`httpOnly`, `SameSite=Lax`) и не требует схемы; отзыв обеспечивается блокировкой пользователя, проверяемой при входе и в защищённых сценариях.

### 2. Antiforgery: глобальный фильтр + эндпоинт выдачи токена

`AddAntiforgery` и `AutoValidateAntiforgeryTokenAttribute` на уровне MVC. Интерфейс получает токен `GET /api/auth/csrf` и отправляет его в заголовке `X-CSRF-TOKEN`; cookie с токеном ставится тем же ответом. Безопасные методы (`GET`/`HEAD`/`OPTIONS`) не проверяются. Это соответствует требованию `platform` отклонять изменяющие запросы без antiforgery-токена.

### 3. Хеширование пароля: PBKDF2 средствами BCL

`Rfc2898DeriveBytes` (SHA-256, 100 000 итераций, 16-байтовая соль, 32-байтовый ключ), кодирование `iterations.salt.hash`. Не подключается ASP.NET Core Identity: нужна только проверка пароля, а Identity приносит лишние зависимости и свою схему хранения.

### 4. Слоистость: `IAppDbContext` в Application

Сценарии в `Doska.Application` работают через абстракцию `IAppDbContext` с `DbSet<T>` и `SaveChangesAsync`; реализация — существующий `AppDbContext`. **Отклонение от `platform`**: `Doska.Application` получает ссылку на `Microsoft.EntityFrameworkCore` (абстракции), тогда как «доступ к данным» остаётся в `Doska.Infrastructure` (DbContext, конфигурации, миграции). Выбрано вместо набора репозиториев, чтобы не плодить методы под каждую выборку очереди и жалоб; предметные правила при этом остаются в `Doska.Domain`, а не в сценариях.

### 5. Правила переходов — в домене, сценарии — оркестрация

`Ad.Approve/Reject/Hide`, `AdComplaint.Close`, `User.RegisterApprovedAd/GrantTrust/RevokeTrust/Block/Unblock` — методы сущностей, бросающие `InvalidOperationException`/`ArgumentException` при нарушении инварианта. Сценарии (`ModerationService`, `ComplaintService`, `TrustService`) загружают сущности, вызывают методы, сохраняют и отправляют письма. Контракт ошибок единый: `ProblemDetails` (400/403/404/409), маппинг — в контроллерах/фильтре.

### 6. Статус при подаче зависит от доверия

`AdService.CreateAsync`: неподтверждённый email — отказ; без доверия — `OnModeration` и суточный лимит 5 объявлений (подсчёт за последние 24 ч по `AuthorId`); с доверием — `Active` и `PublishedAt=now`.

### 7. Письма: абстракция + реализация с записью в журнал

`IEmailSender.SendAsync(email, subject, body)`. Реализация `SmtpEmailSender` через `System.Net.Mail.SmtpClient` при заданном `Email:Host`; иначе `LoggingEmailSender` пишет письмо в журнал (локальная разработка и тесты). Тесты подменяют `IEmailSender` сборщиком писем, чтобы проверять факт и адресата без SMTP.

### 8. Интерфейс: тонкий слой поверх API

Api-клиент — обёртка над `fetch` с `credentials: 'include'` и заголовком antiforgery; хранилище `auth` в Pinia; представления: вход/регистрация (`PasswordHints.vue`), очередь проверки, очередь жалоб, «Мои объявления», форма подачи. Роль берётся из `/api/auth/me`; сервер всё равно проверяет права.

## Risks / Trade-offs

- **Cookie-тикет не отзывается мгновенно.** → При блокировке пользователь не входит заново, а защищённые сценарии проверяют свежую запись пользователя; при необходимости мгновенного отзыва позже добавляется `SecurityStamp`.
- **Antiforgery усложняет API-тесты.** → В тестах токен берётся через `GET /api/auth/csrf` тем же клиентом; для доменных тестов фильтр не участвует.
- **`IAppDbContext` тянет EF Core в Application.** → Осознанное отклонение; при росте проекта сценарии можно перевести на репозитории без изменения домена.
- **Часть `classified-ads` (фото) не реализована.** → Подача работает без фото; требование о 1–5 фотографиях остаётся к отдельному изменению и явно вынесено «Вне рамок».
- **Демо-пароль `123789` из seed.** → Seed запускается командой и не выполняется в `Production`.

## Migration Plan

1. Собрать и прогнать тесты: `dotnet test backend/Doska.sln`, `pnpm --dir frontend test`.
2. Схема не меняется — миграции не создаются и не применяются.
3. Запуск: `docker compose up -d postgres`, затем `dotnet run --project backend/src/Doska.Api`; демо-данные — `dotnet run --project backend/src/Doska.Api -- seed`.
4. Откат — возврат предыдущей версии приложения: изменения схемы отсутствуют, данные совместимы.
