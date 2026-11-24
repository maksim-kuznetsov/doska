# Tasks

> Изменение ломающее для внутреннего API кода: группы идут по зависимостям (Domain → Application → Infrastructure → тесты), и полный `dotnet test` собирается только после четвёртой группы. Поэтому проверка каждой группы — сборка своего проекта, интеграционная проверка — в конце.

## 1. Домен: конструкторы, доменные методы, владение временем

- [x] 1.1 `Entity`: `Id`/`CreatedAt`/`UpdatedAt` закрыть (`private set`), добавить защищённый способ проставить обе отметки при создании; проверить сборку `dotnet build backend/src/Doska.Domain/Doska.Domain.csproj`
- [x] 1.2 `Ad` и `AdPhoto`: параметризованный конструктор с обязательными данными и логикой статуса по доверию автора; закрыть сеттеры; добавить `now` в `Approve/Reject/Hide/CompleteEdit/MarkSold/ReturnToSale/Archive/Renew`; добавить `UpdateDetails(...)` и `MarkReminderSent(now)`; проверить сборку проекта Domain
- [x] 1.3 `User` и `UserToken`: параметризованный конструктор; закрыть сеттеры; добавить `now` в `RegisterApprovedAd/GrantTrust/RevokeTrust/Block/Unblock`; добавить `ChangeRole/SetPassword/ConfirmEmail` и `UserToken.MarkUsed(now)`; проверить сборку проекта Domain
- [x] 1.4 `City` и `Category`: параметризованные конструкторы; закрыть сеттеры; добавить `now` в `Rename/SetSortOrder/Activate/Deactivate/SetParent` и сохранить правило двух уровней; проверить сборку проекта Domain
- [x] 1.5 `AdComplaint`: параметризованный конструктор, закрыть сеттеры, `Close(moderatorId, now)` уже есть; проверить сборку проекта Domain
- [x] 1.6 Обновить доменные тесты под конструкторы и новые сигнатуры: `AdLifecycleDomainTests`, `ModerationDomainTests`, `CityDomainTests`, `CategoryTests` (сущности создаются конструктором напрямую); проверить, что тесты компилируются после интеграционной сборки

## 2. Приложение: перевод сервисов на методы и конструкторы

- [x] 2.1 `AdService`: создание объявления — через конструктор `Ad`; `UpdateAsync` — через `Ad.UpdateDetails`; `MarkReminderSent` в фоновой задаче; проверить сборку `dotnet build backend/src/Doska.Application/Doska.Application.csproj`
- [x] 2.2 `AuthService` и `UserService`: создание `User`/`UserToken` конструктором; `ConfirmEmail`, `SetPassword`, `MarkUsed`, `ChangeRole` вместо прямой записи; проверить сборку проекта Application
- [x] 2.3 `CityService`, `CategoryService`, `ModerationService`, `ComplaintService`, `TrustService`: перевод на методы (`Rename`, `SetSortOrder`, `SetParent`, `Approve`, `Close`, `Block`) с передачей `now` из `clock`; проверить сборку проекта Application

## 3. Инфраструктура: убрать штамповку времени

- [x] 3.1 `AppDbContext`: убрать `TimeProvider`, `ApplyTimestamps` и оба override `SaveChanges`; упростить `AppDbContextFactory` и конструирование контекста до `new AppDbContext(options)`; проверить сборку `dotnet build backend/src/Doska.Infrastructure/Doska.Infrastructure.csproj`
- [x] 3.2 Сидеры (`AdminSeeder`, `ReferenceDataSeeder`, `ExtendedCatalogSeeder`, `DemoDataSeeder`): принимать `TimeProvider`, создавать сущности конструктором и доменными методами; проверить сборку проекта Infrastructure

## 4. Тесты: билдеры и перевод

- [x] 4.1 Добавить общий тестовый билдер сущностей (создание с заданными полями и моментом времени) и перевести на него API-тесты, которые строят сущности напрямую (`ClassifiedAdsApiTests`, `AdSearchApiTests`, `ModerationApiTests`, `CityManagementApiTests`, `CategoryManagementApiTests`, `ExpiryReminderTests`, `MediaMaintenanceTests`, `UserAccountApiTests`, `AdminOutboxApiTests`, `ForwardedHeadersTests`); проверить сборку тестового проекта
- [x] 4.2 Переписать `SaveChangesTests` под владение временем доменом (отметки проставляет домен, а не `SaveChanges`); проверить сборку тестового проекта
- [x] 4.3 Проверить, что состояние сущностей не имеет публичного доступа на запись (обзор свойств `Doska.Domain` или тест на рефлексии); проверить тестом

## 5. Интеграционная проверка

- [x] 5.1 Прогнать `dotnet test` из каталога `backend` — все тесты зелёные; при поднятом `docker compose up -d postgres` дополнительно проходят `SaveChangesTests` и сидерные тесты

## Workflow follow-up

- Заархивировать изменение после прохождения проверки (`openspec-archive-change`).
- Предложить коммит на русском в стиле «Инкапсулировать доменные сущности».
