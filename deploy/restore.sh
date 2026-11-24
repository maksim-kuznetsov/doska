#!/usr/bin/env bash
# Восстановление production из резервной копии. Перезаписывает базу и медиафайлы.
#
# Запуск:  bash deploy/restore.sh backups/db-<дата>.sql.gz backups/media-<дата>.tgz
# Параметр окружения:
#   COMPOSE_FILE  файл compose (по умолчанию docker-compose.prod.yml)
set -euo pipefail

show_help() {
    sed -n '2,6p' "$0" | sed 's/^# \{0,1\}//'
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    show_help
    exit 0
fi

if [[ $# -ne 2 ]]; then
    echo "Использование: bash deploy/restore.sh <дамп-базы.sql.gz> <архив-медиа.tgz>" >&2
    exit 1
fi

DB_FILE="$1"
MEDIA_FILE="$2"

for file in "$DB_FILE" "$MEDIA_FILE"; do
    if [[ ! -f "$file" ]]; then
        echo "Файл не найден: $file" >&2
        exit 1
    fi
done

ENV_MISSING_SUFFIX="."
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"

echo "Восстановление перезапишет базу данных и медиафайлы."
echo "  база:  $DB_FILE"
echo "  медиа: $MEDIA_FILE"
read -r -p "Продолжить? Введите yes: " answer
if [[ "$answer" != "yes" ]]; then
    echo "Отменено."
    exit 1
fi

echo "Остановка сайта..."
compose stop caddy web api

echo "Очистка схемы public..."
compose exec -T postgres sh -c \
    'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" -c "DROP SCHEMA public CASCADE; CREATE SCHEMA public;"'

echo "Восстановление базы..."
gunzip -c "$DB_FILE" \
    | compose exec -T postgres sh -c 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'

echo "Восстановление медиафайлов..."
compose run --rm --no-deps -T --entrypoint sh api \
    -c 'rm -rf /var/www/media/* && tar -xzf - -C /var/www/media' < "$MEDIA_FILE"

echo "Запуск сайта..."
compose up -d

echo "Готово. Проверьте состояние и здоровье:"
echo "  docker compose -f \"$COMPOSE_FILE\" ps"
echo "  curl -fsS https://<домен>/api/health"