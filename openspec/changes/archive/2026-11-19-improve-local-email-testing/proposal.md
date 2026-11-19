# Proposal

## Why

Письма (подтверждение email, сброс пароля, решения модерации, напоминание о сроке) сейчас в Development просто пишутся в журнал API. Без почтовых креденшелов проверить их локально неудобно: нельзя открыть письмо, увидеть ссылку или причину отклонения. Нужен локальный способ читать письма, не заводя реальный SMTP-аккаунт.

## What Changes

- `Email:Mode` выбирает отправителя явно: `Log` (в журнал), `File` (в файлы) или `Smtp` (реальная отправка).
- Новый `FileEmailSender` сохраняет письма как `.eml` в `media/outbox/` — их можно открыть почтовым клиентом или текстовым редактором.
- Development по умолчанию использует `File`, поэтому письма сразу видны без настройки; тесты продолжают подменять `IEmailSender` сборщиком писем.
- В локальный `docker-compose.yml` добавляется **Mailpit**: локальный SMTP без авторизации (порт 1025) с веб-инбоксом на http://localhost:8025. Переключение — `Email:Mode=Smtp`, `Email:Host=localhost`, `Email:Port=1025`, `Email:UseStartTls=false`.
- Если `Email:Mode=Smtp` задан без `Email:Host`, приложение падает на старте с понятным сообщением, а не отправляет письма в никуда.
- README получает раздел «Письма локально» с обоими вариантами и примером переключения на реальный SMTP, когда появятся креды.
- Тесты: `FileEmailSender` действительно создаёт `.eml`; выбор режима сохраняет обратную совместимость (при пустом `Mode` и заданном `Host` используется SMTP, иначе журнал).

## Capabilities

### New Capabilities

Нет.

### Modified Capabilities

Нет. Требования `openspec/specs/` не меняются; change улучшает локальную проверку уже реализованных писем, поэтому объявляет `skip_specs: true`.

## Impact

- `backend/src/Doska.Infrastructure/Email/EmailOptions.cs` и новый `FileEmailSender.cs`.
- `backend/src/Doska.Api/Program.cs` — выбор отправителя и fail-fast.
- `backend/src/Doska.Api/appsettings.Development.json` — `Email:Mode=File`.
- `docker-compose.yml` — сервис `mailpit`.
- `backend/tests/Doska.Tests` — тест файлового отправителя.
- `README.md` — раздел про локальные письма.
- Схема БД и поведение API не меняются.

## Вне рамок

- Доставляемость, DKIM/SPF, обработка bounce и очередь писем с ретраями.
- Внешний провайдер писем и обязательная настройка SMTP в Production.
- Изменение текстов писем и правил уведомлений (`notifications`).