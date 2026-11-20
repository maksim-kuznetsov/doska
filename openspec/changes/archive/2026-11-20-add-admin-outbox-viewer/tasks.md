# Tasks

## 1. Временный режим File в production

- [x] 1.1 Ослабить `ProductionSettings.Validate`: в `Production` разрешить `Smtp` (требует непустой `Email:Host`) и `File` (требует непустой абсолютный `Email:OutboxPath`); `Log` и пустой режим по-прежнему запрещены, текст ошибки перечисляет допустимые режимы и настройки. Проверка: `dotnet test` из `backend` — тесты `ProductionSettingsTests` проходят.
- [x] 1.2 Дополнить `ProductionSettingsTests` сценариями: `File` с абсолютным путём проходит, `File` без пути и с относительным путём отклоняется, `Log` отклоняется. Проверка: `dotnet test` из `backend`.
- [x] 1.3 В `docker-compose.prod.yml` сделать `Email__Host` необязательным (значение по умолчанию пустое), добавить `Email__OutboxPath: ${Email__OutboxPath:-/srv/doska/outbox}` и bind-mount `./outbox:/srv/doska/outbox` к сервису `api`. Проверка: `docker compose -f docker-compose.prod.yml config` с заполненным `.env` показывает переменную и точку монтирования.
- [x] 1.4 Дополнить `.env.example` переменной `Email__OutboxPath=/srv/doska/outbox` и комментарием про временный `Email__Mode=File`; добавить `outbox/` в `.gitignore`. Проверка: `git check-ignore outbox` подтверждает игнор, а `.env.example` содержит новые строки.
- [x] 1.5 Описать в `README.md` и `deploy/DEPLOY.md` временный режим `File` в `Production` (включение, откат на `Smtp`) и очистку обработанных писем. Проверка: разделы читаются и повторяют реальные переменные `EmailOptions`.

## 2. Чтение outbox в API

- [x] 2.1 Добавить в `Doska.Application/Abstractions` контракт `IEmailOutbox` (список писем и чтение письма по имени файла) и тип элемента списка (имя файла, размер, дата изменения, получатель, тема, дата письма). Проверка: решение в `backend` собирается (`dotnet build`).
- [x] 2.2 Реализовать `FileEmailOutbox` в `Doska.Infrastructure/Email`: читает каталог `Email:OutboxPath`, отдаёт письма от новых к старым с ограничением числа, разбирает заголовки `To/Subject/Date` до пустой строки, отклоняет имена с разделителями и выход за каталог, при отсутствующем каталоге возвращает пустой список. Зарегистрировать в DI в `Program.cs`. Проверка: `dotnet test` из `backend`.
- [x] 2.3 Покрыть `FileEmailOutbox` юнит-тестами: порядок и ограничение списка, разбор заголовков, пустой/отсутствующий каталог, отказ обхода каталога, `null` для несуществующего файла. Проверка: `dotnet test` из `backend`.
- [x] 2.4 Добавить `AdminOutboxController` с `GET /api/admin/outbox` и `GET /api/admin/outbox/{fileName}` под политикой `AuthorizationPolicies.Admin` и DTO в `Doska.Api/Contracts` (без сущностей, camelCase). Проверка: `dotnet test` из `backend`.
- [x] 2.5 Дополнить `ApiFactory` тестовым `Email:OutboxPath` и написать интеграционные тесты: аноним — 401, модератор/пользователь — 403, администратор видит список и читает письмо, обход каталога — 400, несуществующий файл — 404. Проверка: `dotnet test` из `backend`.

## 3. Админская страница чтения писем

- [x] 3.1 Добавить в `frontend/src/api/client.ts` типы письма и методы `adminOutbox.list`/`adminOutbox.read`. Проверка: `pnpm test` в `frontend` и сборка типов проходят.
- [x] 3.2 Создать `frontend/src/views/AdminOutboxView.vue` (только чтение): список писем (получатель, тема, дата) и панель содержимого выбранного письма, состояния загрузки/ошибки/пустого ящика на существующих компонентах. Проверка: `pnpm test` в `frontend`.
- [x] 3.3 Добавить `frontend/src/views/__tests__/AdminOutboxView.spec.ts`: отрисовка списка, открытие письма, пустой ящик, показ ошибки. Проверка: `pnpm test` в `frontend`.
- [x] 3.4 Добавить маршрут `admin/outbox` с `meta.requiresAdmin` в `frontend/src/router/index.ts` и пункт «Письма (outbox)» в `frontend/src/components/UserMenu.vue`. Проверка: `pnpm test` в `frontend` (маршруты и меню).

## 4. Интеграционная проверка

- [x] 4.1 Полный прогон: `dotnet test` из `backend` и `pnpm test` в `frontend` — все тесты проходят.
- [x] 4.2 Локальная проверка в режиме `File`: запустить API, зарегистрировать пользователя, войти администратором и открыть `/admin/outbox` — в списке есть письмо подтверждения, оно открывается с рабочей ссылкой. Проверка: наблюдение в браузере.

## Workflow follow-up

- Архивировать изменение после прохождения ревью.
- После архивации предложить коммит на русском в стиле «Архивировать change add-admin-outbox-viewer».
