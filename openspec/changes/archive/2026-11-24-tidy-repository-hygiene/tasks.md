# Tasks

## 1. Скрипты развёртывания

- [x] 1.1 Создать `deploy/_common.sh` с общей преамбулой (корень проекта, `COMPOSE_FILE`/`COMPOSE_DIR`, проверка `.env`, функция `compose`); проверить `bash -n deploy/_common.sh`.
- [x] 1.2 Подключить `_common.sh` из `backup.sh`, `release.sh`, `restore.sh`, убрав дублирование; проверить `bash -n deploy/*.sh` и вывод `--help` каждого скрипта.

## 2. Документация и игнорирование

- [x] 2.1 Заменить `frontend/README.md` на русское описание (запуск, сборка, тесты, PWA) и сверить команды с `frontend/package.json`.
- [x] 2.2 Добавить `/outbox/` в `.dockerignore`, если его там нет; проверить `git check-ignore -v outbox` и сборку compose: `docker compose -f docker-compose.prod.yml config`.

## 3. Проверка

- [x] 3.1 Убедиться, что `deploy/*.sh` проходят `bash -n` и в контроле нет локальных артефактов: `git status --short`.

## Workflow follow-up

- Архивировать изменение после проверок.
