#!/usr/bin/env bash
# Общая преамбула скриптов развёртывания production.
# Подключается через source из backup.sh, release.sh и restore.sh:
#
#   source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"
#
# Вычисляет ROOT_DIR (корень проекта), читает из окружения COMPOSE_FILE/COMPOSE_DIR,
# проверяет наличие compose-файла и .env и объявляет функцию compose.
#
# Переменные окружения:
#   COMPOSE_FILE        файл compose (по умолчанию <корень>/docker-compose.prod.yml)
#   COMPOSE_DIR         каталог проекта compose (по умолчанию <корень>)
#   ENV_MISSING_SUFFIX  хвост сообщения об отсутствующем .env; задайте до source,
#                       чтобы сохранить сообщение вызывающего скрипта (по умолчанию
#                       «: параметры берутся из окружения compose.»)

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-$ROOT_DIR/docker-compose.prod.yml}"
COMPOSE_DIR="${COMPOSE_DIR:-$ROOT_DIR}"

if [[ ! -f "$COMPOSE_FILE" ]]; then
    echo "Не найден compose-файл: $COMPOSE_FILE" >&2
    exit 1
fi

env_missing_suffix="${ENV_MISSING_SUFFIX-: параметры берутся из окружения compose.}"
if [[ ! -f "$ROOT_DIR/.env" ]]; then
    echo "Не найден $ROOT_DIR/.env$env_missing_suffix" >&2
    exit 1
fi

compose() {
    docker compose -f "$COMPOSE_FILE" --project-directory "$COMPOSE_DIR" "$@"
}
