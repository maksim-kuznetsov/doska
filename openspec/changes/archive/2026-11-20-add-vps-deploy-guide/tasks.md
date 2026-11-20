# Tasks

## 1. HTTPS и порядок запуска

- [x] 1.1 Добавить в `docker-compose.prod.yml` сервис `caddy` с портами 80/443 и томами `caddy-data`/`caddy-config`, убрать публикацию порта у `web` (оставить `expose: ["80"]`), добавить переменные `DOMAIN` и `ACME_EMAIL`; проверить `docker compose -f docker-compose.prod.yml config` с тестовыми значениями в окружении.
- [x] 1.2 Создать `deploy/Caddyfile` с доменом из `{$DOMAIN}`, проксированием на `web:80` и перезаписью `X-Forwarded-For`/`X-Forwarded-Proto`/`X-Forwarded-Host`; проверить `docker run --rm -e DOMAIN=example.com -v "${PWD}/deploy/Caddyfile:/etc/caddy/Caddyfile" caddy:2-alpine caddy validate --config /etc/caddy/Caddyfile`.
- [x] 1.3 В `deploy/nginx.conf` пробросить входящие `X-Forwarded-Proto`/`X-Forwarded-For`/`X-Forwarded-Host` с откатом на `$scheme`/`$remote_addr`/`$host`; проверить `docker run --rm -v "${PWD}/deploy/nginx.conf:/etc/nginx/conf.d/default.conf" nginx:1.27-alpine nginx -t`.
- [x] 1.4 Включить в API `UseForwardedHeaders` для `XForwardedFor`/`XForwardedProto` вне `Development` с очисткой известных сетей; добавить тест, что при `X-Forwarded-Proto: https` сессионная cookie получает `Secure`, и проверить `dotnet test backend/Doska.sln`.
- [x] 1.5 Добавить `curl` в runtime-образ API, healthcheck для `api` (`/api/health`) и `web`, условия `depends_on` по готовности (`postgres` → `api` → `web` → `caddy`); проверить `docker compose -f docker-compose.prod.yml config` и `docker run --rm <образ-api> curl --version` после сборки.
- [x] 1.6 Дополнить `frontend/src/__tests__/deployment.spec.ts` проверками сервиса `caddy`, переменных `DOMAIN`/`ACME_EMAIL`, отсутствия публикации порта у `web` и наличия `Caddyfile`; проверить `pnpm --dir frontend test`.
- [x] 1.7 Добавить в `deploy/DEPLOY.md` разделы «Требования» и «Инструкция 1. Чистая виртуалка» до первого запуска: подготовка сервера, firewall, DNS, клонирование, `.env`, запуск, проверка HTTPS и health; проверить, что каждая команда выполняется как написана, а `docker compose -f docker-compose.prod.yml config` проходит с приведённым `.env`.

## 2. Почта и абсолютные ссылки

- [x] 2.1 Добавить в Application абстракцию `IPublicUrlProvider`, в Infrastructure — реализацию из настройки `App:PublicBaseUrl` (по умолчанию `http://localhost:5173` в `Development`), зарегистрировать в DI; проверить тестом, что провайдер возвращает заданный адрес без завершающего слэша.
- [x] 2.2 Перевести ссылки подтверждения email и сброса пароля в `AuthService` на абсолютный адрес из `IPublicUrlProvider`; проверить тестом, что тело письма содержит `https://<домен>/confirm-email?token=` и `https://<домен>/reset-password?token=`.
- [x] 2.3 Добавить проверку на старте: в `Production` без `App:PublicBaseUrl` приложение завершается с понятным сообщением; проверить тестом валидатора настроек и запуском `dotnet run` с `ASPNETCORE_ENVIRONMENT=Production` без переменной (ожидается ошибка).
- [x] 2.4 Дополнить `.env.example` переменными `DOMAIN`, `ACME_EMAIL`, `App__PublicBaseUrl`, `Email__Mode`, `Email__Host`, `Email__Port`, `Email__UserName`, `Email__Password`, `Email__From`, `Email__UseStartTls`; проверить `docker compose -f docker-compose.prod.yml config` с этими значениями.
- [x] 2.5 Добавить в `deploy/DEPLOY.md` раздел «Почта в production»: настройка SMTP, бесплатные провайдеры (Brevo, Resend, MailerSend, SendGrid) с актуальными лимитами и пометкой проверять тариф, SPF/DKIM/DMARC, проверка письма и диагностика «письма в журнале»; проверить, что перечисленные переменные совпадают с `EmailOptions` и что шаги повторяют реальную настройку тестового ящика.

## 3. Первичная инициализация production

- [x] 3.1 Вынести создание городов и категорий из `DemoDataSeeder` в `ReferenceDataSeeder`, переиспользовать его в `seed`; проверить `dotnet test backend/Doska.sln` без регрессий.
- [x] 3.2 Добавить команду `init-data`: идемпотентно наполняет справочники в любом окружении и не создаёт демонстрационных пользователей; проверить тестом двойного запуска и отсутствием пользователей после команды.
- [x] 3.3 Добавить команду `create-admin`: читает `Admin__Email`/`Admin__Password`, проверяет пароль, создаёт подтверждённого администратора или повышает существующего, не создаёт дубликат; проверить тестами создания, повторного запуска и повышения роли.
- [x] 3.4 Добавить в `deploy/DEPLOY.md` шаги инициализации после первого запуска (`init-data`, `create-admin`, смена пароля) и предупреждение, что `seed` в `Production` не работает; проверить, что имена команд совпадают с обработкой аргументов в `Program.cs` (`Select-String -Path backend/src/Doska.Api/Program.cs -Pattern 'init-data|create-admin'`).

## 4. Резервное копирование и восстановление

- [x] 4.1 Создать `deploy/backup.sh`: дамп базы через `docker compose exec -T postgres pg_dump`, архив медиа через `docker compose exec -T api tar`, ротация по `BACKUP_KEEP_DAYS`; проверить `bash -n deploy/backup.sh` и запуск на локальном compose с появлением двух файлов в каталоге копий.
- [x] 4.2 Создать `deploy/restore.sh`: подтверждение, остановка `web`/`caddy`, восстановление базы и медиа, запуск сервисов; проверить `bash -n deploy/restore.sh` и восстановление локального стенда с совпадением числа объявлений до и после.
- [x] 4.3 Добавить в CI джоб проверки скриптов (`bash -n deploy/*.sh`); проверить прогон workflow на push или `act`-совместимым локальным запуском, а при недоступности — командой `docker run --rm -v "${PWD}:/w" -w /w bash:5 bash -n deploy/backup.sh` для каждого скрипта.
- [x] 4.4 Добавить в `deploy/DEPLOY.md` разделы «Резервное копирование», «Восстановление» и пример cron с выносом копий (`rclone`/`rsync`); проверить, что команды из документа совпадают с аргументами скриптов (`deploy/backup.sh --help` или чтение заголовка файла).

## 5. Выпуск изменений и откат

- [x] 5.1 Добавить в `deploy/DEPLOY.md` «Инструкция 2. Выпуск изменений»: бэкап, `git pull`, пересборка, ожидание health, проверка сайта и версии, очистка образов, откат `git checkout` + пересборка, поведение томов и миграций; проверить прохождение шагов на локальном compose (кроме `git pull`) и `docker compose -f docker-compose.prod.yml config`.
- [x] 5.2 Переписать производственный раздел `README.md` в короткую ссылку на `deploy/DEPLOY.md`, сохранив требования HTTPS и откат; обновить `deployment.spec.ts`, чтобы он проверял «Откат релиза» в `DEPLOY.md`, и проверить `pnpm --dir frontend test`.

## 6. Интеграционная проверка

- [x] 6.1 Прогнать полный набор: `docker compose -f docker-compose.prod.yml config` с тестовым `.env`, `dotnet build backend/Doska.sln`, `dotnet test backend/Doska.sln`, `pnpm --dir frontend test`, `pnpm --dir frontend build`, проверка синтаксиса скриптов; зафиксировать результат в отчёте задачи.
- [x] 6.2 Проверить `deploy/DEPLOY.md` на полноту: инструкция 1 заканчивается работающим HTTPS-сайтом с администратором и почтой, инструкция 2 — обновлением и откатом; проверить `openspec validate add-vps-deploy-guide --strict`.

## Workflow follow-up

- Пройти инструкцию 1 на реальной чистой виртуалке (DNS, сертификат, письмо, восстановление из копии) и записать найденные расхождения в `deploy/DEPLOY.md`.
- Архивировать change после проверки и предложить коммит «Архивировать change add-vps-deploy-guide».