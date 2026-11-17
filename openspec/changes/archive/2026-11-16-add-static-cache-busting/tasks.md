# Tasks

## 1. Сборка ассетов

- [x] 1.1 Настроить `web/vite.config.ts` на имена файлов с хешем содержимого и манифест `web/dist/.vite/manifest.json`; проверить `pnpm build` в каталоге web без ошибок.
- [x] 1.2 Убедиться, что тесты (`pnpm test` в каталоге web) проходят без предварительного `pnpm build`; при необходимости скорректировать окружение Vitest; проверить прогон `pnpm test` в каталоге web.
- [x] 1.3 Добавить `web/src/assets/__tests__/staticAssets.spec.ts` с тестом: чтение `web/dist/.vite/manifest.json` после `pnpm build`, имена ассетов `base.<hash>.css` содержат хеш и файл есть в `web/dist/`; проверить `pnpm test` в каталоге web.
- [x] 1.4 Проверить вручную, что в окружении Development dev-сервер Vite отдаёт адреса без хеша: запустить `pnpm dev` в каталоге web и убедиться, что ссылка на `base.css` не содержит хеша.

## 2. nginx

- [x] 2.1 Добавить в `deploy/nginx.conf` `map` по `$upstream_http_content_type` и `add_header Cache-Control $doska_html_cache_control always;` в `location /`; проверить синтаксис командой `docker compose exec -T nginx nginx -t` (после `docker compose up -d`).
- [x] 2.2 Повторить те же правки в `deploy/nginx.https.conf` (он копируется поверх основного на сервере); проверить, что оба файла содержат одинаковый блок, поиском `Select-String -Path deploy/nginx*.conf -Pattern 'doska_html_cache_control'`.
- [x] 2.3 Добавить в `web/src/assets/__tests__/staticAssets.spec.ts` тест, что оба конфига nginx содержат `doska_html_cache_control` и `add_header` в проксирующем `location /`; проверить `pnpm test` в каталоге web.
- [x] 2.4 Добавить в `deploy/release.sh` и `deploy/rollback.sh` после `docker compose up -d --build` перезагрузку nginx (`docker compose exec -T nginx nginx -s reload`); проверить, что скрипты выполняются с `bash -n deploy/release.sh` и `bash -n deploy/rollback.sh` без ошибок.

## 3. Документация

- [x] 3.1 Дополнить `deploy/DEPLOY.md` (разделы 6 и 10): после обновления nginx перезагружается автоматически, проверка — `curl -sI https://домен/ | grep -i cache-control` даёт `no-cache`, а HTML-оболочка ссылается на `base.<hash>.css`; проверить, что команды совпадают с реальным выводом после локального `docker compose up -d`.
- [x] 3.2 Дополнить `deploy/DEPLOY.md` строкой диагностики: старая версия ассетов при обновлении → проверить наличие хеша в адресе и заголовок `Cache-Control`; проверить, что раздел 11 читается и ссылка на проверку не битая.

## 4. Интеграционная проверка

- [x] 4.1 Запустить `dotnet test` и `pnpm test` в каталоге web и убедиться, что все тесты проходят.
- [x] 4.2 Поднять `docker compose up -d --build`, перезагрузить nginx и проверить `curl -sI http://127.0.0.1/` — в ответе есть `Cache-Control: no-cache`; в окружении Production HTML-оболочка ссылается на `base.<hash>.css` (локально dev-сервер Vite намеренно отдаёт адреса без хеша, поэтому хеш проверяется в собранном `web/dist/.vite/manifest.json`).
- [x] 4.3 Выполнить `openspec validate "add-static-cache-busting" --strict` и убедиться, что ошибок нет.

## Workflow follow-up

- Архивировать change после проверки на сервере.
- Предложить коммит в стиле «Добавить версионирование статики и no-cache для HTML».
