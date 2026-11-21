#!/usr/bin/env bash
# Выпуск новой версии production: резервная копия, git pull, пересборка, проверка и откат.
#
# Запуск на сервере из каталога проекта:  bash deploy/release.sh
# Параметры окружения:
#   COMPOSE_FILE    файл compose (по умолчанию docker-compose.prod.yml)
#   HEALTH_TIMEOUT  секунд ожидания здоровья API (по умолчанию 180)
#   SKIP_BACKUP=1   не делать резервную копию перед обновлением
set -euo pipefail

show_help() {
    sed -n '2,9p' "$0" | sed 's/^# \{0,1\}//'
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    show_help
    exit 0
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-$ROOT_DIR/docker-compose.prod.yml}"
COMPOSE_DIR="${COMPOSE_DIR:-$ROOT_DIR}"
HEALTH_TIMEOUT="${HEALTH_TIMEOUT:-180}"

if [[ ! -f "$COMPOSE_FILE" ]]; then
    echo "Не найден compose-файл: $COMPOSE_FILE" >&2
    exit 1
fi

if [[ ! -f "$ROOT_DIR/.env" ]]; then
    echo "Не найден $ROOT_DIR/.env: параметры берутся из окружения compose." >&2
    exit 1
fi

compose() {
    docker compose -f "$COMPOSE_FILE" --project-directory "$COMPOSE_DIR" "$@"
}

domain="$(grep -E '^DOMAIN=' "$ROOT_DIR/.env" | tail -n1 | cut -d= -f2- | tr -d '\r' || true)"

wait_api_healthy() {
    local deadline=$((SECONDS + HEALTH_TIMEOUT)) cid status
    while (( SECONDS < deadline )); do
        cid="$(compose ps -q api 2>/dev/null || true)"
        if [[ -n "$cid" ]]; then
            status="$(docker inspect --format \
                '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' \
                "$cid" 2>/dev/null || true)"
            [[ "$status" == "healthy" ]] && return 0
        fi
        sleep 5
    done
    return 1
}

rollback() {
    echo >&2
    echo "Ошибка обновления — откат на предыдущую ревизию $prev_rev." >&2
    git -C "$ROOT_DIR" checkout "$prev_rev"
    compose up -d --build
    echo "Откат завершён. Для возврата на ветку: git checkout master && git pull --ff-only" >&2
}

# 1. Резервная копия
if [[ "${SKIP_BACKUP:-0}" == "1" ]]; then
    echo "Резервная копия пропущена (SKIP_BACKUP=1)."
else
    echo "Резервная копия..."
    bash "$ROOT_DIR/deploy/backup.sh"
fi

# 2. Текущая ревизия (для отката)
prev_rev="$(git -C "$ROOT_DIR" rev-parse --short HEAD)"
echo "Текущая ревизия: $prev_rev"

# 3. Новый код
echo "Получение кода..."
if ! git -C "$ROOT_DIR" pull --ff-only; then
    echo "git pull не удался. Разберитесь с локальными правками; reset --hard на сервере не делайте." >&2
    exit 1
fi
new_rev="$(git -C "$ROOT_DIR" rev-parse --short HEAD)"
echo "Новая ревизия: $new_rev"

# 4. Пересборка и запуск
echo "Пересборка и запуск (несколько минут, возможен короткий простой)..."
if ! compose up -d --build; then
    rollback
    exit 1
fi

# 5. Проверка здоровья API
echo "Ожидание готовности API (до ${HEALTH_TIMEOUT} с)..."
if ! wait_api_healthy; then
    compose logs --tail=100 api >&2 || true
    rollback
    exit 1
fi
echo "API здоров."

if [[ -n "$domain" ]]; then
    if curl -fsS "https://$domain/api/health" >/dev/null; then
        echo "Проверка https://$domain/api/health пройдена."
    else
        echo "Предупреждение: https://$domain/api/health не отвечает, проверьте вручную." >&2
    fi
fi

# 6. Очистка
echo "Очистка неиспользуемых образов..."
docker image prune -f >/dev/null || true

echo
echo "Готово: $prev_rev → $new_rev."
echo "Проверьте сайт и операции, затронутые изменением."
