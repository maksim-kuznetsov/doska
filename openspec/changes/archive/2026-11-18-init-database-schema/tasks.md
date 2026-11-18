# Tasks

## 1. Инструменты EF Core

- [x] 1.1 Создать локальный манифест инструментов в `backend` (`dotnet new tool-manifest`) и установить `dotnet-ef` версии 10.x (`dotnet tool install dotnet-ef --version 10.*`), чтобы миграции создавались одной командой без глобальной установки. Проверка: из `backend` команда `dotnet tool run dotnet-ef --version` печатает версию 10.x.

## 2. Сущности домена

- [x] 2.1 Добавить перечисления `UserRole`, `AdStatus`, `ComplaintStatus`, `TokenPurpose` в `backend/src/Doska.Domain/` с русскими XML-комментариями; значения — как в разделе «Модель данных» `design.md`. Проверка: `dotnet build backend/Doska.sln` проходит без ошибок и предупреждений анализатора.
- [x] 2.2 Добавить сущности `User` и `UserToken` с полями из `design.md` (`blocked_at`, `is_trusted`, `approved_ads_count`, `token_hash`, `purpose`, `expires_at`, `used_at`). Проверка: `dotnet build` проходит, состав полей совпадает с таблицей `design.md`.
- [x] 2.3 Добавить сущности `City` и `Category` (самоссылка `parent_id → categories.id`, `sort_order`) с правилом «подраздел не может быть родителем» в виде метода сущности. Проверка: `dotnet build` проходит, тест `CategoryTests` подтверждает, что добавление третьего уровня отклоняется.
- [x] 2.4 Добавить сущности `Ad`, `AdPhoto`, `AdComplaint` с полями из `design.md`; `published_at` — начало текущего срока публикации, `reminder_sent_at` — отметка напоминания. Проверка: `dotnet build` проходит, состав полей совпадает с `design.md`.
- [x] 2.5 Добавить тест `DomainAssemblyTests`, который проверяет, что `Doska.Domain` не ссылается ни на один проект `Doska.*` (требование `platform` о направлении зависимостей). Проверка: `dotnet test backend/Doska.sln --filter DomainAssemblyTests` проходит.

## 3. Конфигурации EF Core и `AppDbContext`

- [x] 3.1 Добавить по файлу `IEntityTypeConfiguration<T>` для каждой из семи сущностей в `backend/src/Doska.Infrastructure/Persistence/Configurations/`: явные связи и поведение удаления (`ads → ad_photos` и `ads → ad_complaints` каскад, `ads → categories/cities/users` запрет), `numeric(12,2)` для цены, `timestamptz` для времён, перечисления строками. Проверка: `dotnet build` проходит; тест `ModelConfigurationTests` находит в модели все семь типов.
- [x] 3.2 Добавить в схеме ограничения: `CHECK (price >= 0)`, `CHECK` на значения `role`, `status`, `purpose`, `sort_order BETWEEN 1 AND 5`, уникальные индексы `users.email`, `cities.name`, `user_tokens.token_hash`, `ad_complaints (ad_id, author_id)`, `ad_photos (ad_id, sort_order)`, индексы ленты и выборок (`ads (status, published_at)`, `ads (category_id, status)`, `ads (city_id)`, `ads (author_id, created_at)`, `ad_complaints (status, created_at)`, `user_tokens (user_id, purpose)`). Проверка: тест `ModelConfigurationTests` проверяет наличие каждого ограничения и индекса через `IReadOnlyModel`.
- [x] 3.3 Заполнить `AppDbContext`: семь `DbSet`, `ApplyConfigurationsFromAssembly`, `OnModelCreating` без логики в `Program.cs`, заполнение `created_at`/`updated_at` в `SaveChanges` через `TimeProvider`. Проверка: тест `SaveChangesTests` сохраняет сущность в базу из `docker compose` и подтверждает, что времена заполнены, а `updated_at` меняется при изменении.
- [x] 3.4 Добавить тест соответствия перечислений и `CHECK`-ограничений: каждое значение `UserRole`, `AdStatus`, `ComplaintStatus`, `TokenPurpose` присутствует в тексте своего ограничения. Проверка: `dotnet test backend/Doska.sln --filter EnumConstraintTests` проходит.

## 4. Первая миграция

- [x] 4.1 Добавить `AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>` в `Doska.Infrastructure`: строка подключения из переменной окружения `ConnectionStrings__Default`, иначе — значения `appsettings.Development.json`. Проверка: из `backend` команда `dotnet tool run dotnet-ef dbcontext info --project src/Doska.Infrastructure --startup-project src/Doska.Api` печатает имя `AppDbContext` и провайдер Npgsql, а не запускает API.
- [x] 4.2 Создать миграцию `InitialSchema` (`dotnet tool run dotnet-ef migrations add InitialSchema --project src/Doska.Infrastructure --startup-project src/Doska.Api --output-dir Migrations`) и просмотреть сгенерированный файл: только создание таблиц, ключей, индексов и ограничений, без ручных правок. Проверка: в `backend/src/Doska.Infrastructure/Migrations` есть пара `<timestamp>_InitialSchema.cs` + `AppDbContextModelSnapshot.cs`, миграция содержит `CreateTable` для семи таблиц.
- [x] 4.3 Применить миграцию на чистой базе из `docker compose` (`docker compose up -d postgres`, затем `dotnet tool run dotnet-ef database update --project src/Doska.Infrastructure --startup-project src/Doska.Api`). Проверка: `docker compose exec postgres psql -U doska -d doska -c "\dt"` показывает `users`, `cities`, `categories`, `ads`, `ad_photos`, `ad_complaints`, `user_tokens` и `__EFMigrationsHistory`.
- [x] 4.4 Проверить, что имена в базе — нижний регистр с подчёркиваниями, а не PascalCase: сгенерировать SQL (`dotnet tool run dotnet-ef migrations script --project src/Doska.Infrastructure --startup-project src/Doska.Api --output migrations.sql`) и убедиться в отсутствии кавычек вокруг имён таблиц и столбцов; файл `migrations.sql` не коммитить. Проверка: в SQL есть `CREATE TABLE ads` и нет `"Ads"`.

## 5. Применение миграций при старте

- [x] 5.1 В `Program.cs` применить миграции до `MapControllers()`: получить scope, вызвать `Database.Migrate()` при `Database:MigrateOnStartup` (по умолчанию `true`); отсутствие настройки в `appsettings.json` означает включённое поведение. Проверка: запуск `dotnet run --project src/Doska.Api` на пустой базе создаёт таблицы, повторный запуск ничего не применяет (в логе нет новых миграций).
- [x] 5.2 В `tests/Doska.Tests/ApiFactory.cs` выключить `Database:MigrateOnStartup`, чтобы существующие тесты API не требовали PostgreSQL. Проверка: `dotnet test backend/Doska.sln` проходит без запущенной базы.
- [x] 5.3 Проверить отказ при недоступной базе: остановить контейнер (`docker compose stop postgres`) и запустить API — приложение завершается с ошибкой подключения и не начинает обслуживать запросы. Проверка: в выводе есть сообщение об ошибке Npgsql, процесс завершается с ненулевым кодом.
- [x] 5.4 Задокументировать в XML-комментариях `AppDbContext` и `AppDbContextFactory` порядок работы со схемой (только миграции, ручные правки запрещены) и команды создания миграции. Проверка: комментарии видны в IntelliSense, `dotnet build` проходит.

## 6. Итоговая проверка

- [x] 6.1 Проверить полный цикл на чистой базе: удалить локальные данные (`docker compose down -v` — уничтожает dev-базу), поднять `docker compose up -d postgres`, запустить API и убедиться, что схема создана. Проверка: `\dt` в `psql` показывает восемь таблиц, `GET /api/health` отвечает успешно.
- [x] 6.2 Прогнать весь набор тестов бэкенда и убедиться, что падений нет. Проверка: из `backend` команда `dotnet test` завершается успешно; результат записан в ответе.
- [x] 6.3 Проверить артефакты изменения валидатором OpenSpec. Проверка: `openspec validate init-database-schema` завершается без ошибок.

## Workflow follow-up

- Показать артефакты изменения владельцу проекта на ревью; реализацию начинать только после отдельного запроса.
- До реализации подтвердить у владельца открытые вопросы из `design.md`: считается ли удалённое объявление израсходованной квотой суточного лимита и подходят ли выбранные длины строк.
- Архивировать change после выполнения задач: коммит «Архивировать change init-database-schema».

## Отклонения при выполнении

- 1.1: `dotnet tool restore` и `dotnet tool run` в этой среде недоступны (ложное срабатывание MOTW на `backend/.config/dotnet-tools.json`); манифест создан и сохранён, а сам инструмент 10.0.4 установлен локально в `backend/.tools/dotnet-ef/` (каталог в `.gitignore`). Версия подтверждена командой `backend\.tools\dotnet-ef\dotnet-ef.exe --version` → 10.0.4.
- 4.1/4.2: в `Doska.Api.csproj` добавлен `Microsoft.EntityFrameworkCore.Design` (он нужен командам с `--startup-project src/Doska.Api`); генерация выполнялась после отдельной сборки с `--no-build`, а `TEMP`/`TMP` перенаправлялись в `.tmp` из-за ограничений среды.
- 4.3/4.4: SQL в `psql` передавался через stdin (`docker compose exec -T postgres psql`), чтобы PowerShell не искажал кавычки в именах; `migrations.sql` удалён после проверки и не коммитится.
- 5.1/5.3/6.1: вместо `dotnet run` запускался собранный `Doska.Api.exe` (так достоверно снимается код возврата) с `Logging__EventLog__LogLevel__Default=None`: в этой среде запись в журнал событий Windows (источник `.NET Runtime`) запрещена, и любая запись уровня Error прерывает операцию, маскируя исходную ошибку.
- 6.1: после `docker compose up -d` нужно дождаться не только статуса `healthy`, но и завершения `initdb`: временный сервер PostgreSQL принимает TCP-соединение и тут же закрывает его, поэтому первый запуск API на свежем томе может упасть с `EndOfStreamException` при SSL-рукопожатии.
- 5.2/6.2: в этой среде `dotnet test` падает на именованном канале MSBuild-моста Microsoft.Testing.Platform, поэтому набор выполнялся прямым запуском `backend\tests\Doska.Tests\bin\Debug\net10.0\Doska.Tests.exe`: Total 68, Errors 0, Failed 0, Skipped 0.
