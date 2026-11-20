# Proposal

## Why

На проде SMTP ещё не настроен, поэтому включён временный режим `Email:Mode=File`: письма (подтверждение email, сброс пароля, решения модерации) сохраняются в `/srv/doska/outbox`, но пользователям не уходят. Нужен согласованный временный вариант: разрешить `File` в `Production` осознанно и дать администратору читать накопленные письма через админскую страницу, вручную доводя ссылки/причины до пользователей.

## What Changes

- **Временный режим `File` в `Production`.** `ProductionSettings.Validate` перестаёт требовать SMTP, когда выбран `Email:Mode=File`; при этом в `Production` для `File` обязателен абсолютный `Email:OutboxPath`. SMTP остаётся штатным режимом для `Production`, `File` — явный временный откат.
- **Доступ API к каталогу писем.** В `docker-compose.prod.yml` добавляется bind-mount `./outbox:/srv/doska/outbox` и переменная `Email__OutboxPath=/srv/doska/outbox`, чтобы путь на сервере совпадал с уже существующим.
- **Новый админский API чтения outbox.** `GET /api/admin/outbox` — список писем (имя файла, дата изменения, размер), `GET /api/admin/outbox/{fileName}` — содержимое письма. Доступ только роли `Admin`; имя файла проверяется, выход за каталог невозможен.
- **Новая админская страница.** Раздел «Письма (outbox)» в меню администратора, маршрут `/admin/outbox`: таблица писем и просмотр выбранного письма. Только чтение.
- **Обновление документации.** README и `deploy/DEPLOY.md` описывают временный режим `File` в `Production` и чтение писем через админскую страницу.

## Capabilities

### New Capabilities
- `admin-outbox`: чтение администратором писем из файлового outbox (список писем и просмотр содержимого выбранного письма).

### Modified Capabilities
- `platform`: требование «Почта в production» — режим `File` допускается в `Production` как временный вариант (с обязательным абсолютным `Email:OutboxPath`), помимо штатного SMTP.

## Вне рамок

- Реальная отправка писем пользователям (настройка SMTP — отдельная задача).
- Удаление, скачивание, отметка «прочитано» и фильтрация писем: страница только показывает список и содержимое.
- Автоматическая доставка писем из outbox, повторная отправка, интеграция с почтовым клиентом.
- Изменение формата `.eml`, генерируемого `FileEmailSender`.
- Горизонтальное масштабирование и внешнее хранилище outbox.

## Impact

- Бэкенд: `Doska.Api/Infrastructure/ProductionSettings.cs` и его тесты, новый `AdminOutboxController`, абстракция чтения outbox в `Doska.Application` и реализация в `Doska.Infrastructure` (рядом с `FileEmailSender`, `EmailOptions`).
- Интерфейс: новое представление `frontend/src/views/AdminOutboxView.vue`, маршрут `/admin/outbox`, пункт меню в `UserMenu.vue`, методы в `frontend/src/api/client.ts`.
- Развёртывание: `docker-compose.prod.yml` (bind-mount и `Email__OutboxPath`), `.env.example`, `README.md`, `deploy/DEPLOY.md`.
- Тесты: интеграционные тесты API для нового эндпоинта (доступ по ролям, защита от выхода за каталог), тест допустимости `File` в `Production`, тест представления в Vitest.
