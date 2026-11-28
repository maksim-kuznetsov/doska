# Развёртывание на VPS

Документ описывает два сценария:

- **Инструкция 1** — установка с нуля на чистую виртуалку.
- **Инструкция 2** — выпуск изменений на уже работающий сервер и откат.

Общие разделы — переменные окружения, почта, резервное копирование и диагностика — находятся после инструкций.

## 1. Требования

- VPS с Ubuntu 24.04 LTS (подойдёт и 22.04), 1–2 vCPU, 2 ГБ RAM, 10 ГБ диска и больше.
- Домен или поддомен, например `doska.example.com`, и доступ к управлению DNS.
- Доступ по SSH с правами `sudo`.
- Открытые порты 22 (SSH), 80 и 443. Порт 80 нужен Let's Encrypt для проверки домена и перенаправления на HTTPS.
- Docker Engine и плагин `docker compose` (ставятся на шаге 1).

Схема production:

```
Интернет
   |
   | 80/443
   v
+-----------------+     +-------------------+     +----------------+     +------------+
| Caddy           |---->| web (nginx)       |---->| api (.NET 10)  |---->| PostgreSQL |
| HTTPS, ACME     |     | SPA, /api, media  |     |                |     | 17         |
+-----------------+     +-------------------+     +----------------+     +------------+
                              |                        |                      |
                              +---- media-data --------+                 postgres-data
```

Сертификат выпускает Caddy автоматически. Наружу опубликованы только порты 80 и 443; API и база доступны только внутри сети compose.

## 2. Инструкция 1. Чистая виртуалка

### Шаг 1. Подготовка сервера

Подключитесь к серверу от `root` и создайте рабочего пользователя:

```bash
ssh root@<IP-сервера>
adduser deploy
usermod -aG sudo deploy
rsync --archive --chown=deploy:deploy ~/.ssh /home/deploy
```

Если `rsync` не установлен, скопируйте ключи вручную:

```bash
mkdir -p /home/deploy/.ssh
cp ~/.ssh/authorized_keys /home/deploy/.ssh/
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh
chmod 600 /home/deploy/.ssh/authorized_keys
```

Выйдите и подключитесь уже как `deploy`. Дальше все команды выполняются от этого пользователя.

Обновите систему и закройте лишние порты:

```bash
sudo apt update
sudo apt upgrade -y
sudo apt install -y ufw git curl ca-certificates
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
sudo ufw status
```

Установите Docker по официальной инструкции:

```bash
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo $VERSION_CODENAME) stable" \
  | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker deploy
```

Переподключитесь по SSH, чтобы применилась группа `docker`, и проверьте:

```bash
docker --version
docker compose version
docker run --rm hello-world
```

На сервере с 2 ГБ RAM желательно добавить swap:

```bash
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### Шаг 2. Домен и DNS

Создайте A-запись домена на IP сервера:

| Тип | Имя | Значение |
| --- | --- | --- |
| A | `doska.example.com` | `<IP-сервера>` |

Если у сервера есть IPv6, добавьте AAAA-запись. Не добавляйте CNAME на корень домена.

Проверьте, что запись уже видна:

```bash
dig +short doska.example.com
```

Ответ должен совпадать с IP сервера. Сертификат не выпустится, пока DNS не обновился, поэтому дождитесь ответа до первого запуска.

### Шаг 3. Код

```bash
sudo mkdir -p /srv/doska
sudo chown deploy:deploy /srv/doska
git clone https://github.com/maksim-kuznetsov/doska.git /srv/doska
cd /srv/doska
```

Если репозиторий приватный, добавьте deploy-ключ сервера в настройках GitHub и клонируйте по SSH-адресу.

### Шаг 4. Секреты

```bash
cd /srv/doska
cp .env.example .env
chmod 600 .env
nano .env
```

Заполните файл:

```dotenv
POSTGRES_DB=doska
POSTGRES_USER=doska
POSTGRES_PASSWORD=<длинный случайный пароль>
DOMAIN=doska.example.com
ACME_EMAIL=admin@example.com
App__PublicBaseUrl=https://doska.example.com
# Внешний вход необязателен; значения добавляются после создания OAuth-клиента (шаг 8).
# Google__ClientId=<client id>
# Google__ClientSecret=<client secret>
# Yandex__ClientId=<client id>
# Yandex__ClientSecret=<client secret>
Email__Mode=File
Email__OutboxPath=/srv/doska/outbox
# SMTP-параметры — после подтверждения домена отправителя и переключения на Email__Mode=Smtp.
# Email__Host=<SMTP-хост провайдера>
# Email__Port=587
# Email__UserName=<логин SMTP>
# Email__Password=<пароль или API-ключ>
# Email__From=no-reply@doska.example.com
# Email__UseStartTls=true
```

Пароль базы сгенерируйте командой `openssl rand -hex 24`. Значения с символами `$`, `#` и кавычками заключайте в одинарные кавычки: Docker Compose подставляет переменные в `.env`.

`App__PublicBaseUrl` — адрес сайта без завершающего слэша. Из него строятся ссылки в письмах подтверждения email и сброса пароля. Без этой настройки API в `Production` не запустится. Почта по умолчанию работает в режиме `Email__Mode=File`: письма сохраняются в `Email__OutboxPath` и доступны администратору в разделе «Письма (outbox)». Когда домен отправителя подтверждён у провайдера, заполните SMTP-параметры, верните `Email__Mode=Smtp` и пересоздайте API.

### Шаг 5. Первый запуск

```bash
cd /srv/doska
docker compose -f docker-compose.prod.yml up -d --build
```

Первая сборка занимает несколько минут. Затем проверьте состояние:

```bash
docker compose -f docker-compose.prod.yml ps
```

Все сервисы должны быть `running`, а `postgres`, `api` и `web` — `healthy`. Смотрите журнал Caddy, если сертификат не выпускается:

```bash
docker compose -f docker-compose.prod.yml logs -f caddy
```

Проверки:

```bash
curl -I https://doska.example.com
curl -fsS https://doska.example.com/api/health
curl -I http://doska.example.com
```

Ожидается: HTTPS отвечает `200` и заголовком `Strict-Transport-Security`, `/api/health` возвращает `200`, HTTP перенаправляет на HTTPS.

### Шаг 6. Справочники и администратор

На пустой базе создайте города, категории и первого администратора. Команда `seed` в `Production` намеренно не работает: она создаёт демонстрационные учётные записи с общеизвестным паролем.

```bash
cd /srv/doska
docker compose -f docker-compose.prod.yml run --rm api init-data
docker compose -f docker-compose.prod.yml run --rm api seed-catalog
docker compose -f docker-compose.prod.yml run --rm \
  -e Admin__Email=admin@example.com \
  -e Admin__Password='<длинный пароль>' \
  api create-admin
```

`seed-catalog` — расширенный справочник: административные центры субъектов РФ и крупнейшие города (94 города), дерево категорий по образцу avito.ru и вариант «Другой» в каждом списке. Команды можно запускать повторно: справочники не дублируются, существующие записи не перезаписываются, а `create-admin` обновляет роль и пароль существующего пользователя.

После создания войдите в интерфейс и смените пароль через личный кабинет. Пароль из переменной окружения не должен остаться в истории команд: при необходимости очистите её (`history -c`).

### Шаг 7. Почта

По умолчанию в `.env` включён файловый режим: письма не уходят пользователям, а сохраняются в `/srv/doska/outbox` и читаются администратором на странице «Письма (outbox)». Этого достаточно, пока домен отправителя не подтверждён у провайдера: ссылку подтверждения или причину решения передавайте вручную.

Когда домен подтверждён, заполните в `.env` `Email__Host`, `Email__Port`, `Email__UserName`, `Email__Password`, `Email__From`, задайте `Email__Mode=Smtp` и пересоздайте API:

```bash
docker compose -f docker-compose.prod.yml up -d api
```

Проверка: зарегистрируйте тестовый аккаунт, письмо должно прийти на указанный адрес, а ссылка подтверждения — открываться по адресу `https://doska.example.com/confirm-email?token=...`. Подробности и список бесплатных провайдеров — в разделе «Почта в production».

### Шаг 8. Вход через Google и Яндекс

Внешний вход необязателен: без кредов приложение работает как раньше, а кнопки входа скрыты. Провайдеры настраиваются независимо.

Google:

1. В Google Cloud Console откройте **APIs & Services → Credentials → Create credentials → OAuth client ID**.
2. Тип приложения — **Web application**, в **Authorized redirect URIs** укажите:
   `https://<домен>/api/auth/google/callback`.

Яндекс ID:

1. В сервисе [Яндекс OAuth](https://oauth.yandex.ru/) создайте приложение типа
   **Для авторизации пользователей** и укажите Redirect URI
   `https://<домен>/api/auth/yandex/callback`.
2. В доступах приложения выберите **Логин, имя и фамилия** (`login:info`) и **Адрес
   электронной почты** (`login:email`): доступ к email обязателен для входа.
3. Пройдите верификацию сервиса у Яндекса: без неё пользователи видят предупреждение на
   странице согласия, и конверсия входа падает.

Добавьте выбранные креды в `.env` на сервере:

   ```dotenv
   Google__ClientId=<client id>
   Google__ClientSecret='<client secret>'
   Yandex__ClientId=<client id>
   Yandex__ClientSecret='<client secret>'
   ```

   Секреты с символами `$`, `#` или кавычками заключите в одинарные кавычки. Значения хранятся только в `.env` и не попадают в репозиторий.

Пересоздайте API:

   ```bash
   docker compose -f docker-compose.prod.yml up -d api
   ```

Проверьте: `curl -fsS https://doska.example.com/api/auth/providers` возвращает список вида
`{"providers":[{"id":"google","name":"Google","enabled":true},{"id":"yandex","name":"Яндекс","enabled":true}]}`,
на странице входа появились кнопки настроенных провайдеров, а в профиле доступна привязка.

Для отключения провайдера удалите его пару значений из `.env` и пересоздайте API.

### Шаг 9. Резервное копирование

Создайте каталог и первую копию:

```bash
cd /srv/doska
mkdir -p backups
bash deploy/backup.sh
ls -lh backups
```

Настройте ежедневный запуск:

```bash
crontab -e
```

Добавьте строку:

```cron
17 3 * * * cd /srv/doska && bash deploy/backup.sh >> backups/backup.log 2>&1
```

Копии нужно выносить за пределы сервера — подробности и пример в разделе «Резервное копирование».

### Шаг 10. Финальные проверки

- Сайт открывается по HTTPS, HTTP перенаправляется.
- В браузере устанавливается PWA (DevTools → Application → Manifest/Service Workers).
- Регистрация проходит, письмо приходит, ссылка подтверждения открывается.
- Вход администратора работает, видны категории и города.
- При настроенных провайдерах работают вход нового пользователя, автоматическая привязка существующего аккаунта и привязка из профиля.
- Подача объявления с фотографией и одобрение модератором проходят.
- `docker compose -f docker-compose.prod.yml ps` не показывает нездоровых сервисов.
- `docker compose -f docker-compose.prod.yml logs --tail=100 api` не содержит ошибок.

## 3. Инструкция 2. Выпуск изменений

Обновление занимает несколько минут. На время пересборки API и веб-сервера возможен короткий простой; база и медиа сохраняются в томах.

Шаги ниже можно выполнить одной командой — `deploy/release.sh` делает резервную копию, `git pull`, пересборку, проверку здоровья API и откат при ошибке:

```bash
cd /srv/doska
bash deploy/release.sh
```

Параметры: `SKIP_BACKUP=1` пропускает резервную копию, `HEALTH_TIMEOUT` задаёт ожидание здоровья API в секундах (по умолчанию 180). Ниже — та же процедура по шагам, для ручного выполнения и разбора.

### Шаг 1. Резервная копия

```bash
cd /srv/doska
bash deploy/backup.sh
```

Убедитесь, что в `backups/` появились свежие дамп базы и архив медиа.

### Шаг 2. Код

```bash
git pull --ff-only
git rev-parse --short HEAD
```

Запишите хеш новой ревизии — он пригодится для отката. Если `git pull` не проходит из-за локальных правок, не делайте `reset --hard` на сервере: сначала разберитесь, что изменено.

### Шаг 3. Пересборка и запуск

```bash
docker compose -f docker-compose.prod.yml up -d --build
```

Миграции EF Core применяются автоматически при старте API. Если миграция завершится ошибкой, контейнер API не начнёт обработку запросов, а старый контейнер останется до следующей успешной попытки.

### Шаг 4. Проверка

```bash
docker compose -f docker-compose.prod.yml ps
curl -fsS https://doska.example.com/api/health
docker compose -f docker-compose.prod.yml logs --tail=100 api
```

Проверьте в браузере главную страницу, вход и одну операцию, затронутую изменением. Если в интерфейсе видна старая версия, обновите страницу: service worker предлагает обновление.

### Шаг 5. Очистка

```bash
docker image prune -f
docker builder prune -f
```

Первую команду можно запускать после каждого релиза, вторую — периодически, например раз в месяц.

### Откат релиза

`release.sh` откатывается сам, если сборка не удалась или API не стал здоровым. Ручной откат, если проблема видна уже после выпуска:

```bash
cd /srv/doska
git checkout <хеш-предыдущей-ревизии>
docker compose -f docker-compose.prod.yml up -d --build
curl -fsS https://doska.example.com/api/health
```

Тома `postgres-data`, `media-data` и `caddy-data` не удаляются, сертификат переиспользуется. Миграции откатом кода не отменяются: схема остаётся новой. Перед откатом проверьте, что предыдущая версия совместима с текущей схемой; миграции в проекте аддитивны.

Чтобы вернуться на актуальную ветку после отката:

```bash
git checkout master
git pull --ff-only
docker compose -f docker-compose.prod.yml up -d --build
```

## 4. Переменные окружения

| Переменная | Обязательна | Назначение |
| --- | --- | --- |
| `POSTGRES_DB` | нет, по умолчанию `doska` | Имя базы. |
| `POSTGRES_USER` | нет, по умолчанию `doska` | Пользователь базы. |
| `POSTGRES_PASSWORD` | да | Пароль базы. Генерируйте случайным. |
| `DOMAIN` | да | Домен для сертификата и сайта. |
| `ACME_EMAIL` | да | Адрес для уведомлений Let's Encrypt. |
| `App__PublicBaseUrl` | да | Публичный адрес сайта для ссылок в письмах и возврата после внешнего входа. |
| `Google__ClientId` | нет | Идентификатор OAuth-клиента Google; вместе с секретом включает вход через Google. |
| `Google__ClientSecret` | нет | Секрет OAuth-клиента Google. Храните только в `.env`. |
| `Yandex__ClientId` | нет | Идентификатор приложения Яндекс OAuth; вместе с секретом включает вход через Яндекс. |
| `Yandex__ClientSecret` | нет | Секрет приложения Яндекс OAuth. Храните только в `.env`. |
| `Email__Mode` | нет, по умолчанию `File` | Режим отправки: `File` (текущий, письма в `Email__OutboxPath`) или `Smtp` (после подтверждения домена). |
| `Email__Host` | да, при режиме `Smtp` | SMTP-хост провайдера. |
| `Email__Port` | нет, по умолчанию `587` | SMTP-порт. |
| `Email__UserName` | зависит от провайдера | Логин SMTP. |
| `Email__Password` | зависит от провайдера | Пароль или API-ключ SMTP. |
| `Email__From` | да, при режиме `Smtp` | Адрес отправителя на вашем домене. |
| `Email__UseStartTls` | нет, по умолчанию `true` | STARTTLS для порта 587. |
| `Email__OutboxPath` | да, при режиме `File` | Абсолютный каталог писем файлового режима. |

Секреты хранятся только в `.env` на сервере. Файл исключён из git и из контекста сборки Docker.

## 5. Почта в production

### Текущий режим: файловый ящик

Пока домен отправителя не подтверждён у провайдера, API работает с `Email__Mode=File`: письма не уходят пользователям, а сохраняются как `.eml` в `Email__OutboxPath` (по умолчанию `/srv/doska/outbox`, каталог `./outbox` смонтирован в API). Администратор читает их на странице «Письма (outbox)» (меню пользователя → служебные разделы) и вручную передаёт ссылку подтверждения или причину решения.

Переключение на реальную отправку: подтвердите домен у провайдера, заполните SMTP-параметры в `.env`, задайте `Email__Mode=Smtp` и пересоздайте API — `docker compose -f docker-compose.prod.yml up -d api`.

Очистка обработанных писем (файлы создаёт контейнер, поэтому может понадобиться `sudo`):

```bash
cd /srv/doska
sudo rm -f outbox/*.eml
```

Каталог `outbox/` исключён из git и не входит в резервные копии базы и медиа.

### Бесплатные SMTP-провайдеры

Лимиты бесплатных тарифов меняются. Проверяйте актуальные условия на сайте провайдера перед выбором.

| Провайдер | Ориентир бесплатного тарифа | SMTP |
| --- | --- | --- |
| Brevo | около 300 писем в сутки | `smtp-relay.brevo.com`, порт 587 |
| Resend | около 3000 писем в месяц, 100 в сутки | `smtp.resend.com`, порт 587 |
| MailerSend | около 3000 писем в месяц | `smtp.mailersend.net`, порт 587 |
| SendGrid | около 100 писем в сутки | `smtp.sendgrid.net`, порт 587, логин `apikey` |

Для доски объявлений с подтверждением email и уведомлениями модерации обычно достаточно Brevo или Resend. Не используйте личный Gmail или почтовый ящик хостера для боевой отправки: низкие лимиты и плохая доставляемость.

### Настройка домена

1. Зарегистрируйтесь у провайдера и добавьте домен `doska.example.com` (или корневой домен).
2. Провайдер покажет DNS-записи. Добавьте их у регистратора:
   - SPF: `TXT`, значение вида `v=spf1 include:<провайдер> ~all`;
   - DKIM: `TXT` с именем и значением от провайдера;
   - DMARC: `TXT` для `_dmarc`, например `v=DMARC1; p=none; rua=mailto:admin@example.com`.
3. Дождитесь проверки домена в панели провайдера.
4. В `.env` укажите SMTP-хост, порт, логин, пароль и `Email__From` на вашем домене.
5. Пересоздайте API: `docker compose -f docker-compose.prod.yml up -d api`.

Проверка SPF и DKIM:

```bash
dig +short TXT doska.example.com
dig +short TXT <селектор-dkim>._domainkey.doska.example.com
```

### Проверка отправки

1. Зарегистрируйте тестовый аккаунт с реальным ящиком.
2. Письмо должно прийти в течение минуты; проверьте папку «Спам».
3. Ссылка в письме должна быть абсолютной: `https://doska.example.com/confirm-email?token=...`.
4. Если письма нет, посмотрите журнал:

```bash
docker compose -f docker-compose.prod.yml logs --tail=200 api
```

Типовые причины:

- в журнале строка «письмо записано в журнал» — не задан `Email__Host` или режим не `Smtp`;
- ошибка авторизации — неверный логин или ключ;
- ошибка TLS — порт 465 вместо 587 или неверный `Email__UseStartTls`;
- письмо в спаме — не настроены SPF/DKIM/DMARC или адрес отправителя не на домене.

Проверить репутацию письма можно на `mail-tester.com`: отправьте письмо на выданный адрес и посмотрите оценку.

## 6. Резервное копирование и восстановление

`deploy/backup.sh` создаёт дамп базы и архив медиа в каталоге `backups/`, удаляет копии старше `BACKUP_KEEP_DAYS` (по умолчанию 14 дней) и напоминает вынести копии с сервера.

```bash
cd /srv/doska
bash deploy/backup.sh
```

Параметры:

```bash
BACKUP_DIR=/srv/backups BACKUP_KEEP_DAYS=30 bash deploy/backup.sh
```

Вынос копий за пределы сервера — обязательный шаг. Например, через `rclone` в S3-совместимое хранилище (у Backblaze B2 есть бесплатный объём) или на другой сервер:

```bash
rclone copy /srv/doska/backups remote:doska-backups --max-age 2d
```

Добавьте вынос в cron после копирования:

```cron
27 3 * * * rclone copy /srv/doska/backups remote:doska-backups --max-age 2d
```

### Восстановление

Восстановление перезаписывает базу и медиа. Сначала остановите сайт, чтобы никто не писал в базу:

```bash
cd /srv/doska
docker compose -f docker-compose.prod.yml stop caddy web
bash deploy/restore.sh backups/db-<дата>.sql.gz backups/media-<дата>.tgz
```

Скрипт запросит подтверждение. После завершения проверьте `docker compose -f docker-compose.prod.yml ps`, `curl -fsS https://doska.example.com/api/health` и несколько объявлений с фотографиями.

Раз в квартал проверяйте восстановление на тестовой среде: копия, которая никогда не восстанавливалась, не считается резервной.

## 7. Диагностика

```bash
cd /srv/doska
docker compose -f docker-compose.prod.yml ps
docker compose -f docker-compose.prod.yml logs --tail=200 api
docker compose -f docker-compose.prod.yml logs --tail=200 web
docker compose -f docker-compose.prod.yml logs --tail=200 caddy
```

| Симптом | Что проверить |
| --- | --- |
| Сайт не открывается | `ufw status`, `docker compose ps`, логи Caddy, DNS-запись. |
| Сертификат не выпускается | A-запись, порт 80, лимиты Let's Encrypt, `docker compose logs caddy`. |
| `502 Bad Gateway` | API не здоров: `docker compose ps`, `docker compose logs api`. |
| Фотографии не открываются | том `media-data`, права, строка `X-Accel-Redirect` в логах API. |
| `413` при загрузке фотографий | Лимит тела запроса на внешнем nginx (`client_max_body_size 32m`), журнал nginx `client intended to send too large body`, явные лимиты Caddy (`request_body`) и CDN/WAF; заголовок `Server` показывает слой. |
| Письма не приходят | `Email__*` в `.env`, логи API, SPF/DKIM, папка «Спам». |
| Интерфейс показывает старую версию | service worker предлагает обновление; проверьте, что `web` пересобран. |
| Мало места | `df -h`, `docker system df`, `docker image prune -f`, старые копии в `backups/`. |
| API не стартует после обновления | `docker compose logs api`, ошибка миграции, откат по инструкции 2. |

### Лимит размера запроса при загрузке фотографий

Приложение принимает до 5 фотографий по 5 МБ, поэтому каждый слой, принимающий тело запроса, должен допускать не менее 32 МБ. Внутренний nginx из `deploy/nginx.conf` уже настроен (`client_max_body_size 32m`), но production использует внешний обратный прокси — его нужно настроить отдельно.

Для внешнего nginx добавьте директиву в `http`, `server` или `location`, который проксирует запросы к сайту:

```nginx
client_max_body_size 32m;
```

Проверьте конфигурацию и перезагрузите nginx:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

Caddy по умолчанию не ограничивает размер тела запроса; если перед сайтом стоит Caddy, проверьте явные ограничения (`request_body { max_size ... }`). Если используется CDN или WAF, проверьте его лимит на размер запроса.

Проверка: подайте объявление с фотографией 3–5 МБ. В DevTools → Network ответ `POST /api/ads` должен быть `201 Created`, а объявление — появиться в списке. Если `413` сохраняется, найдите слой по заголовку `Server` и журналу nginx:

```bash
sudo grep 'client intended to send too large body' /var/log/nginx/error.log
```

Проверить сертификаты Caddy:

```bash
docker compose -f docker-compose.prod.yml exec caddy caddy list-certificates
```

## 8. Правовые страницы и проверка OAuth

После публикации проверьте, что политика и условия открываются без входа:

```bash
curl -fsS https://<публичный-домен>/privacy | grep -q '<div id="app">'
curl -fsS https://<публичный-домен>/terms | grep -q '<div id="app">'
```

Обе страницы отдаёт nginx по SPA-fallback, отдельная настройка не нужна. Если ответ `404`,
проверьте `location /` и `try_files` в `deploy/nginx.conf` и что контейнер `web` пересобран.

В Google Cloud Console → **APIs & Services** → **OAuth consent screen**:

1. **Privacy policy URL** — `https://<публичный-домен>/privacy`.
2. **Terms of service URL** — `https://<публичный-домен>/terms`.
3. Убедитесь, что страницы открываются без входа, и отправьте приложение на проверку.

При регистрации приложения Яндекс OAuth те же адреса указываются в пунктах политики
конфиденциальности и условий использования.

Реквизиты и даты редакции меняются в `frontend/src/legal/details.ts`; после изменения текстов
пересоберите и выложите `web` обычным релизом.

## 9. Обслуживание сервера

- Обновляйте пакеты ОС: `sudo apt update && sudo apt upgrade -y`, затем при необходимости перезагрузка.
- Контейнеры с `restart: unless-stopped` поднимаются после перезагрузки автоматически.
- Раз в месяц проверяйте `docker system df` и очищайте неиспользуемые образы.
- Следите за сроком действия домена и за копиями: они должны быть не только на сервере.
- Не храните `.env` в репозитории и не передавайте его в чатах.