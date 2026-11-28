# Doska

Доска объявлений: бэкенд — ASP.NET Core Web API (.NET 10) с PostgreSQL, интерфейс — Vue 3 (Composition API, TypeScript, Vite, Pinia).

## Структура репозитория

- `backend/` — решение `Doska.sln`: `src/Doska.Domain`, `src/Doska.Application`, `src/Doska.Infrastructure`, `src/Doska.Api`, тесты `tests/Doska.Tests`.
- `frontend/` — приложение Vue 3 с проксированием `/api` на бэкенд.
- `openspec/` — спецификации и изменения.
- `docker-compose.yml` — локальная PostgreSQL.

## Требования к окружению

- .NET SDK 10.0.
- Node.js и pnpm: в Node 26 `corepack` не поставляется, поэтому `npm install -g corepack`, затем `corepack enable pnpm`.
- Docker с плагином `docker compose`.

## Локальный запуск

1. База данных:

   ```
   docker compose up -d postgres
   ```

   PostgreSQL 17 публикуется на порту **5433** (порт 5432 занят стеком предыдущей версии проекта), база и пользователь — `doska`, пароль — `doska`.

2. API:

   ```
   dotnet run --project backend/src/Doska.Api
   ```

   Адрес — http://localhost:5000, Swagger — http://localhost:5000/swagger (только в окружении `Development`).

3. Dev-сервер интерфейса:

   ```
   pnpm --dir frontend dev
   ```

   Адрес — http://localhost:5173; запросы к `/api` проксируются на http://localhost:5000.

### Внешний вход локально (Google и Яндекс)

Вход через внешнего провайдера необязателен: без настроек приложение работает как раньше,
а кнопка входа скрыта. Провайдеры настраиваются независимо и появляются в списке
`/api/auth/providers`.

Google:

1. В Google Cloud Console создайте OAuth-клиент типа **Web application**.
2. В поле **Authorized redirect URIs** добавьте `http://localhost:5173/api/auth/google/callback`.

Яндекс ID:

1. В сервисе [Яндекс OAuth](https://oauth.yandex.ru/) создайте приложение типа
   **Для авторизации пользователей** и укажите Redirect URI
   `http://localhost:5173/api/auth/yandex/callback`.
2. В доступах приложения выберите **Логин, имя и фамилия** (`login:info`) и **Адрес
   электронной почты** (`login:email`): доступ к email обязателен для входа.

Запуск API с кредами в переменных окружения:

   ```
   $env:Google__ClientId="<client id>"
   $env:Google__ClientSecret="<client secret>"
   $env:Yandex__ClientId="<client id>"
   $env:Yandex__ClientSecret="<client secret>"
   dotnet run --project backend/src/Doska.Api
   ```

Redirect URI строится из `App:PublicBaseUrl` (`http://localhost:5173` в Development), поэтому обратный вызов проходит через Vite-прокси и открывается на адресе интерфейса. В production для этого используется публичный адрес сайта с портом, если он нестандартный.

## Справочники и сиды

- `init-data` — базовые справочники: три города и три раздела каталога. Используется при первичной инициализации production.
- `seed` — демонстрационные данные для разработки: базовые справочники и три учётные записи. В `Production` демо-аккаунты не создаются.
- `seed-catalog` — расширенный справочник: административные центры субъектов РФ и крупнейшие города (94 города), дерево категорий по образцу avito.ru (12 разделов, 92 подраздела) и вариант «Другой» в списке городов и в разделе «Разное».

```
dotnet run --project backend/src/Doska.Api -- seed-catalog
```

Все команды идемпотентны: создают только отсутствующие записи и не перезаписывают существующие. `seed-catalog` можно запускать и после `init-data`, и на пустой базе — повторный запуск ничего не добавляет.

## Проверка каркаса

- `curl.exe http://localhost:5000/api/health` — прямой ответ API.
- `curl.exe http://localhost:5173/api/health` — ответ через прокси Vite.

## PWA

Интерфейс собирается как устанавливаемое приложение: web-манифест, брендовые иконки,
service worker и офлайн-оболочка. Service worker регистрируется только в production-сборке
и только в защищённом контексте — по HTTPS или на `localhost`.

- Dev-сервер (`pnpm --dir frontend dev`) PWA не включает: service worker создаётся сборкой.
- Локальная проверка:

  ```
  pnpm --dir frontend build
  pnpm --dir frontend preview
  ```

  Откройте http://localhost:4173, затем DevTools → Application: проверьте Manifest и
  Service Workers. Включите offline и перезагрузите страницу — появится офлайн-страница
  с действием «Повторить».

- Иконки пересобираются из `frontend/public/pwa/logo.svg` командой
  `pnpm --dir frontend pwa:assets`.
- В production HTTPS обеспечивает Caddy из `docker-compose.prod.yml`: наружу открыты только
  порты 80 и 443, сертификат Let's Encrypt выпускается автоматически. Без HTTPS сайт работает
  как обычный сайт, но установка и офлайн-режим недоступны.
- После деплоя новой версии приложение предлагает обновление. Откат выполняется по разделу
  «Откат релиза»: service worker не кеширует ответы API, поэтому данные не устаревают.

## Тесты

- Бэкенд: `dotnet test backend/Doska.sln`.
- Интерфейс: `pnpm --dir frontend test`.

## Сквозной смоук

`scripts/smoke.ps1` проходит живой стенд целиком: регистрация → подтверждение email через psql →
подача объявления с фото → очередь модерации → одобрение → лента → «Продано» → удаление.
Скрипт создаёт временного пользователя и убирает его и объявление в конце.

```
powershell -File scripts/smoke.ps1
```

Параметры: `-BaseUrl` (по умолчанию `http://localhost:5000`), `-ModeratorEmail`,
`-ModeratorPassword`, `-DatabaseUser`, `-DatabaseName`. Нужны запущенные API, PostgreSQL
из `docker compose` и демо-модератор (`moderator@example.com` / `123789`).

## UI-смоук (Playwright)

```
pnpm --dir frontend e2e
```

Браузерный смоук проверяет публичную ленту и фильтры, вход модератора (очередь и жалобы),
вход администратора (категории, города, пользователи) и страницу-гайд. Нужны запущенные API,
PostgreSQL и демо-данные (`dotnet run --project backend/src/Doska.Api -- seed`); dev-сервер Vite
Playwright поднимает сам. Тест не изменяет данные.

## Очистка медиа

```
dotnet run --project backend/src/Doska.Api -- cleanup-media
```

Удаляет каталоги `media/photos/<adId>`, для которых нет объявления, и файлы, не привязанные
к `ad_photos`. Команда идемпотентна: повторный запуск ничего не меняет.

## Непрерывная интеграция

Workflow `.github/workflows/ci.yml` запускается на push в `master`, на pull request и вручную
и выполняет три джобы:

- **Backend** — поднимает PostgreSQL 17 на порту 5433, собирает решение, проверяет, что модель
  EF Core не разошлась с миграциями (`dotnet ef migrations has-pending-model-changes`), применяет
  миграции к чистой базе и запускает `dotnet test`.
- **Frontend** — устанавливает зависимости по lockfile, запускает `pnpm test`, `pnpm build`
  и `pnpm check:pwa`.
- **Production images** — после зелёных тестов проверяет `docker-compose.prod.yml` и собирает
  production-образы API и веб-сервера.

Локальное воспроизведение проверок:

```
docker compose up -d postgres
dotnet build backend/Doska.sln
dotnet test backend/Doska.sln
pnpm --dir frontend test
pnpm --dir frontend build
pnpm --dir frontend check:pwa
```

Если в вашем окружении `dotnet test` не работает из-за ограничений среды, тесты можно запустить
напрямую: `backend/tests/Doska.Tests/bin/Debug/net10.0/Doska.Tests.exe`.

## Остановка

- `docker compose down` — остановить контейнеры, данные остаются в томе `postgres-data`.
- `docker compose down -v` — остановить контейнеры и удалить том с данными.

## Производственное развёртывание

Полная инструкция — [`deploy/DEPLOY.md`](deploy/DEPLOY.md): установка с нуля на чистую виртуалку
и выпуск изменений на работающий сервер.

Развёртывание выполняется через `docker compose` на одном сервере: PostgreSQL, API, nginx и
Caddy. Caddy завершает HTTPS и автоматически получает сертификат Let's Encrypt; наружу открыты
только порты 80 и 443. Секреты и параметры подключения передаются переменными окружения из
`.env` и не хранятся в репозитории.

- Первичная установка, справочники, администратор, почта и бэкапы — «Инструкция 1».
- Обновление, проверка и откат — «Инструкция 2».

### Обновление production из OpenCode

Выпуск автоматизирован: `deploy/release.sh` на сервере делает резервную копию, `git pull`,
пересборку, проверку здоровья API и откат при ошибке. В OpenCode есть скилл `deploy` и команда
`/deploy`, которые вызывают этот скрипт по SSH.

Перед использованием задайте переменные окружения (адрес сервера и домен не хранятся в
репозитории):

```powershell
setx DOSKA_PROD_SSH "deploy@<IP-сервера>"      # или алиас из ~/.ssh/config
setx DOSKA_PROD_DOMAIN "doska.example.com"      # без схемы
```

`DOSKA_PROD_SSH` обязательна: это адрес для SSH. `DOSKA_PROD_DOMAIN` нужна только для внешней
проверки здоровья. После `setx` перезапустите терминал и OpenCode. Прод тянет код из GitHub,
поэтому перед выпуском закоммитьте и запушьте изменения в `origin/master`.

Выпуск вручную — та же команда, что использует скилл:

```bash
ssh "$DOSKA_PROD_SSH" 'cd /srv/doska && bash deploy/release.sh'
```

## Правовые страницы и проверка Google OAuth

Политика конфиденциальности доступна по адресу `/privacy`, условия использования — по `/terms`.
Страницы открываются без входа и не обращаются к API, поэтому их можно указывать в Google Cloud
Console при настройке входа через Google и при регистрации приложения Яндекс OAuth.

- Локально: http://localhost:5173/privacy и http://localhost:5173/terms.
- В production: `<публичный адрес сайта>/privacy` и `<публичный адрес сайта>/terms`.

Порядок настройки OAuth-приложения:

1. Откройте Google Cloud Console → **APIs & Services** → **OAuth consent screen**.
2. В поле **Privacy policy URL** укажите `<публичный адрес сайта>/privacy`.
3. В поле **Terms of service URL** укажите `<публичный адрес сайта>/terms`.
4. Проверьте, что страницы открываются в браузере без входа на сайт, и отправьте приложение
   на проверку.

Реквизиты оператора, контакт, публичный адрес и даты редакции задаются в
`frontend/src/legal/details.ts`; тексты документов — в `frontend/src/views/PrivacyPolicyView.vue`
и `frontend/src/views/TermsOfServiceView.vue`. При изменении обработки данных обновите текст
и дату редакции.

## Окружения и настройки

- Окружение задаётся переменной `ASPNETCORE_ENVIRONMENT`: `Development` для локальной работы,
  `Production` для развёртывания. В `Production` недоступны Swagger и страница-гайд дизайн-системы.
- Настройки читаются из `appsettings*.json` и переопределяются переменными окружения
  (двойное подчёркивание вместо двоеточия, например `ConnectionStrings__Default`).
- Миграции EF Core применяются при старте API. Если миграция завершается ошибкой, приложение
  останавливается и не начинает обработку запросов — развёртывание не переключается на новую версию.
- Медиафайлы хранятся в томе `media-data`; в `Production` их отдаёт nginx по внутреннему
  перенаправлению приложения, в `Development` — само приложение.

## Письма локально

Письма (подтверждение email, сброс пароля, решения модерации, напоминание о сроке) в Development
по умолчанию сохраняются как файлы `.eml` в `backend/src/Doska.Api/media/outbox/`. Откройте файл
почтовым клиентом или текстовым редактором — внутри ссылка подтверждения/сброса или причина решения.

Режим задаётся настройкой `Email:Mode`:

- `File` — файлы в `Email:OutboxPath` (по умолчанию `media/outbox`); режим по умолчанию в Development и Production.
- `Log` — письма пишутся в журнал API.
- `Smtp` — реальная отправка; требует `Email:Host`, иначе приложение не стартует.

### Локальный SMTP с веб-инбоксом (Mailpit)

Без реальных креденшелов письма удобно смотреть в Mailpit:

```
docker compose up -d mailpit
```

Затем запустите API с переключением на SMTP (веб-инбокс — http://localhost:8025):

```
$env:Email__Mode="Smtp"
$env:Email__Host="localhost"
$env:Email__Port="1025"
$env:Email__UseStartTls="false"
dotnet run --project backend/src/Doska.Api
```

### Реальный SMTP

Когда появятся креды, задайте `Email__Mode=Smtp`, `Email__Host`, `Email__Port`, `Email__UserName`,
`Email__Password`, `Email__From` (в `.env` при развёртывании). Код и тесты при этом не меняются.

### Файловый режим в production

Пока домен отправителя не подтверждён у провайдера, на боевом стенде работает `Email__Mode=File`:
письма сохраняются в `Email__OutboxPath` (bind-mount `./outbox` → `/srv/doska/outbox`), а
администратор читает их в разделе «Письма (outbox)» и вручную передаёт ссылку подтверждения или
причину решения. Это режим по умолчанию в `.env.example` и `docker-compose.prod.yml`.

Когда домен подтверждён, заполните SMTP-параметры, верните `Email__Mode=Smtp` и пересоздайте API.
Подробности и очистка каталога — в `deploy/DEPLOY.md`, раздел «Почта в production».

## Откат релиза

Порядок описан в [`deploy/DEPLOY.md`](deploy/DEPLOY.md), «Инструкция 2», раздел «Откат релиза»:
верните предыдущую ревизию кода, затем пересоберите сервисы командой
`docker compose -f docker-compose.prod.yml up -d --build`.

Данные остаются в томах `postgres-data`, `media-data` и `caddy-data`; миграции аддитивны,
поэтому предыдущая версия приложения совместима с текущей схемой.
