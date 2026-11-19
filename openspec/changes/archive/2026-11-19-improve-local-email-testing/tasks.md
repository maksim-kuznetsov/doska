# Tasks

## 1. Режимы отправки

- [x] 1.1 Добавить `Email:Mode` и `Email:OutboxPath` в `EmailOptions`.
- [x] 1.2 Реализовать `FileEmailSender`, сохраняющий письма как `.eml` в каталог исходящих.
- [x] 1.3 Выбирать отправителя в `Program` по режиму с обратной совместимостью и fail-fast при `Smtp` без хоста.
- [x] 1.4 Development по умолчанию — `File`.

## 2. Локальный SMTP

- [x] 2.1 Добавить сервис `mailpit` (1025/8025) в `docker-compose.yml`.
- [x] 2.2 Описать в README варианты `File` и `Mailpit` и переключение на реальный SMTP.

## 3. Тесты и проверка

- [x] 3.1 Юнит-тест `FileEmailSender`: создаётся `.eml` с адресом и темой.
- [x] 3.2 Локально: письмо появляется в `media/outbox` в режиме `File` и в Mailpit в режиме `Smtp`.
- [x] 3.3 `dotnet build`, тесты бэкенда, `pnpm test`, `pnpm build`.
- [x] 3.4 `openspec validate improve-local-email-testing`.