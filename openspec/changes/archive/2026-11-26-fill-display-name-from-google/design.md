# Design

## Context

См. `proposal.md` — Why. Текущее состояние, важное для решения:

- `GoogleAuthentication.OnCreatingTicket` уже читает из userinfo `sub`, `email`, `email_verified` и кладёт их в пользовательские claim'ы (`google_subject`, `google_email`, `google_email_verified`). Scope `profile` уже запрошен в `Program.cs`, поэтому поле `name` присутствует в ответе userinfo, но игнорируется.
- `GoogleAuthService.SignInAsync`/`LinkAsync` принимают только subject, email и признак подтверждения; `User.CreateWithGoogle` и `LinkGoogle` имя не пишут.
- Столбец `display_name varchar(100), null` уже существует (миграция `AddUserProfile`), `ProfileService.MaxDisplayNameLength = 100`. Схему менять не нужно.

## Goals / Non-Goals

**Goals:**

- Заполнять отображаемое имя из имени Google во всех точках появления привязки (создание, автопривязка, явная привязка), не перезаписывая уже заданное.
- Единое правило нормализации и длины имени, независимое от формата userinfo.
- Сохранить совместимость с откатом и passwordless-поведением.

**Non-Goals:**

- Синхронизация имени при повторных входах и перезапись ручного значения.
- Иные поля Google (`picture`, `locale`, части имени).
- Изменение схемы БД и публичного контракта API.

## Decisions

### 1. Имя передаётся отдельным claim `google_name`

`OnCreatingTicket` читает `name` через существующий `ReadString(context.User, "name")` и кладёт его в новый claim `google_name` тем же `ReplaceClaim`. `OnTicketReceived` читает claim и передаёт значение в сервис.

Альтернатива — брать имя из `context.Principal.FindFirstValue(ClaimTypes.Name)`: Google-хендлер не гарантирует маппинг `name` в `ClaimTypes.Name`, а формат userinfo может меняться. Явный пользовательский claim повторяет уже принятый в проекте подход (`google_subject` и др.) и прямо тестируем.

### 2. Заполнение имени — доменный способ `ApplyGoogleDisplayName`

В `User` добавляется `public void ApplyGoogleDisplayName(string? displayName, DateTimeOffset now)`: нормализует значение тем же `Normalize`, игнорирует пустое и значение длиннее `MaxDisplayNameLength`, и меняет `DisplayName` только если он сейчас `null`. При изменении вызывается `MarkUpdated`.

Альтернативы: расширять `CreateWithGoogle` и `LinkGoogle` отдельными параметрами — дублирует правило; переиспользовать `UpdateProfile` — перезаписывает контакты и не различает ручное и внешнее значение. Отдельный метод выражает инвариант «не перезаписывать заданное» в одном месте.

Предел длины выносится в доменную константу `User.MaxDisplayNameLength = 100`; `ProfileService` переиспользует её вместо собственной, чтобы правило длины было единым.

### 3. Порядок применения и приоритеты

`GoogleAuthService.SignInAsync(subject, email, displayName, emailVerified)`:

1. существующая привязка по subject — только `EnsureNotBlocked` (имя не трогаем);
2. аккаунт по email — привязка subject, подтверждение email, затем `ApplyGoogleDisplayName`;
3. новый аккаунт — `CreateWithGoogle` и затем `ApplyGoogleDisplayName`.

`LinkAsync(..., displayName, ...)` вызывает `ApplyGoogleDisplayName` после `LinkGoogle`. Во всех случаях `displayName` необязателен: пустое или слишком длинное имя не мешает входу и не вызывает ошибок валидации (в отличие от обязательных `sub`/`email`).

### 4. Тесты

- Домен: `ApplyGoogleDisplayName` заполняет пустое имя, не перезаписывает заданное, игнорирует пустое и слишком длинное.
- Сервис: создание аккаунта с именем, автопривязка с заполнением имени, явная привязка, повторный вход не перезаписывает.

## Risks / Trade-offs

- **Расхождение имени на разных языках/форматах** → значение берётся как есть из userinfo (после обрезки), без догадок о структуре.
- **Список сценариев Google нестабилен** → имя опционально и не влияет на успех входа; при отсутствии поля поведение прежнее.
- **Изменение сигнатур сервиса** → компилятор укажет всех вызывающих; тесты обновляются в том же изменении.
- **Откат релиза** → схема не менялась; старая версия просто не заполняет имя, данные не портятся.

## Migration Plan

1. Выпустить код: схема БД не меняется, миграции не нужны.
2. Проверить вход нового Google-пользователя (имя подставляется) и вход существующего с заданным именем (не перезаписывается).
3. Откат приложения безопасен: столбец `display_name` сохраняется, старый код его не заполняет.
