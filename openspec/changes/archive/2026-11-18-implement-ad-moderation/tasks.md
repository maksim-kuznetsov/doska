# Tasks

## 1. Абстракции прикладного слоя

- [x] 1.1 Добавить в `Doska.Application` ссылку на `Microsoft.EntityFrameworkCore` и абстракции `IAppDbContext` (набор `DbSet` + `SaveChangesAsync`), `IPasswordHasher`, `IEmailSender`. Проверка: `dotnet build backend/Doska.sln` без ошибок.
- [x] 1.2 Реализовать `AppDbContext : IAppDbContext` (без изменения модели), `PasswordHasher` (PBKDF2), `LoggingEmailSender` и `SmtpEmailSender` в `Doska.Infrastructure`. Проверка: `dotnet build backend/Doska.sln`.

## 2. Правила домена

- [x] 2.1 Добавить в `Ad` методы `Approve(now)`, `Reject(reason)`, `Hide(reason)` с проверкой текущего статуса и обязательности причины. Проверка: `dotnet test --filter ModerationDomainTests`.
- [x] 2.2 Добавить в `AdComplaint` метод `Close(moderatorId, now)` с проверкой, что жалоба открыта. Проверка: доменные тесты.
- [x] 2.3 Добавить в `User` методы `RegisterApprovedAd()` (счётчик и доверие после трёх одобрений), `GrantTrust()`, `RevokeTrust()`, `Block(now)`, `Unblock()`. Проверка: доменные тесты.

## 3. Сценарии модерации

- [x] 3.1 `ModerationService`: очередь объявлений «на проверке» по возрастанию даты подачи. Проверка: API-тест очереди.
- [x] 3.2 `ModerationService.ApproveAsync`: перевод в «активно», учёт одобрения для доверия. Проверка: API-тест одобрения.
- [x] 3.3 `ModerationService.RejectAsync`: обязательная причина, сохранение причины и письмо автору. Проверка: API-тест отклонения и письма.
- [x] 3.4 `ModerationService.HideAsync`: причина, скрытие активного объявления и письмо автору. Проверка: API-тест скрытия.
- [x] 3.5 `TrustService`: ручная выдача и отзыв доверия. Проверка: API-тест доверия.

## 4. Сценарии жалоб

- [x] 4.1 `ComplaintService.FileAsync`: запрет жалобы на своё объявление и повторной жалобы, приём только на активное объявление, подтверждённый email. Проверка: API-тесты жалоб.
- [x] 4.2 `ComplaintService.ResolveAsync`: закрытие жалобы с оставлением или скрытием объявления. Проверка: API-тест решения по жалобе.

## 5. Аутентификация, роли, ограничение частоты

- [x] 5.1 `AuthService`: регистрация (хеш пароля, требования сложности), вход, выход, `me`, подтверждение email. Проверка: API-тесты входа и прав.
- [x] 5.2 Cookie-аутентификация, политики ролей (`ModeratorOrAdmin`), antiforgery и ограничение частоты в `Program.cs`. Проверка: тесты 401/403 и скрытия прав.
- [x] 5.3 `AuthController` + `GET /api/auth/csrf` для выдачи antiforgery-токена. Проверка: API-тест изменяющего запроса без токена.

## 6. Подача объявлений

- [x] 6.1 `AdService.CreateAsync`: статус по доверию, суточный лимит 5 для непроверенного автора, валидация обязательных полей. Проверка: API-тесты подачи.
- [x] 6.2 `AdService.GetMineAsync`: список своих объявлений со статусом и причиной. Проверка: API-тест «Мои объявления».

## 7. API и контракты

- [x] 7.1 DTO и контроллеры `Auth`, `Ads`, `Moderation`, `Complaints`, `Admin` (доверие/блокировка) без переиспользования сущностей БД. Проверка: `dotnet build backend/Doska.sln`.
- [x] 7.2 Единый маппинг ошибок в `ProblemDetails` (400/401/403/404/409). Проверка: API-тесты кодов ответов.

## 8. Демо-данные

- [x] 8.1 Seed демо-пользователей (`user@`, `moderator@`, `admin@example.com`, пароль `123789`) и демо-городов/категорий по команде `dotnet run --project backend/src/Doska.Api -- seed`, только вне `Production`. Проверка: тест seed.

## 9. Тесты бэкенда

- [x] 9.1 Доменные тесты переходов статусов, жалоб и доверия. Проверка: `dotnet test backend/Doska.sln`.
- [x] 9.2 API-тесты очереди, решений, жалоб, доверия и разграничения прав (с пропуском без PostgreSQL). Проверка: `dotnet test backend/Doska.sln`.

## 10. Интерфейс

- [x] 10.1 Api-клиент с antiforgery и `credentials`, хранилище сессии Pinia. Проверка: `pnpm --dir frontend test`.
- [x] 10.2 Представления: вход/регистрация (`PasswordHints.vue`), очередь проверки, очередь жалоб, «Мои объявления», форма подачи; маршруты и навигация. Проверка: `pnpm --dir frontend build`.
- [x] 10.3 Тесты Vue для подсказок пароля и списка модерации. Проверка: `pnpm --dir frontend test`.

## 11. Итоговая проверка

- [x] 11.1 `dotnet test backend/Doska.sln` и `pnpm --dir frontend test` — без падающих тестов.
- [x] 11.2 `openspec validate implement-ad-moderation` — change корректен.
