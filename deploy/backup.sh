#!/usr/bin/env bash
# Резервная копия production: дамп PostgreSQL и архив медиафайлов.
#
# Запуск из любого каталога:  bash deploy/backup.sh
# Параметры окружения:
#   BACKUP_DIR        каталог копий (по умолчанию <корень проекта>/backups)
#   BACKUP_KEEP_DAYS  сколько дней хранить копии (по умолчанию 14)
#   COMPOSE_FILE      файл compose (по умолчанию docker-compose.prod.yml)
set -euo pipefail

show_help() {
    sed -n '2,8p' "$0" | sed 's/^# \{0,1\}//'
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    show_help
    exit 0
fi

ENV_MISSING_SUFFIX=": параметры базы берутся из окружения compose."
source "$(dirname "${BASH_SOURCE[0]}")/_common.sh"
BACKUP_DIR="${BACKUP_DIR:-$ROOT_DIR/backups}"
BACKUP_KEEP_DAYS="${BACKUP_KEEP_DAYS:-14}"

mkdir -p "$BACKUP_DIR"
timestamp="$(date +%Y%m%d-%H%M%S)"
db_file="$BACKUP_DIR/db-$timestamp.sql.gz"
media_file="$BACKUP_DIR/media-$timestamp.tgz"

echo "Дамп базы данных..."
compose exec -T postgres sh -c 'pg_dump --clean --if-exists --no-owner -U "$POSTGRES_USER" -d "$POSTGRES_DB"' \
    | gzip > "$db_file"

echo "Архив медиафайлов..."
compose exec -T api tar -czf - -C /var/www/media . > "$media_file"

echo "Удаление копий старше $BACKUP_KEEP_DAYS дней..."
find "$BACKUP_DIR" -maxdepth 1 -type f \
    \( -name 'db-*.sql.gz' -o -name 'media-*.tgz' \) \
    -mtime "+$BACKUP_KEEP_DAYS" -delete

echo "Готово:"
echo "  $db_file ($(du -h "$db_file" | cut -f1))"
echo "  $media_file ($(du -h "$media_file" | cut -f1))"
echo "Вынесите копии за пределы сервера, например:"
echo "  rclone copy \"$BACKUP_DIR\" remote:doska-backups --max-age 2d"