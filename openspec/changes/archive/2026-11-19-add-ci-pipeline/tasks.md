# Tasks

## 1. Workflow

- [x] 1.1 Создать `.github/workflows/ci.yml` с триггерами push/PR/ручной запуск, правами только на чтение и отменой устаревших запусков.
- [x] 1.2 Backend-джоба: PostgreSQL 17 на 5433, .NET 10, кэш NuGet, сборка, `migrations has-pending-model-changes`, `database update`, `dotnet test`.
- [x] 1.3 Frontend-джоба: Node 22, pnpm 10, кэш, `pnpm install --frozen-lockfile`, `pnpm test`, `pnpm build`.
- [x] 1.4 Docker-джоба после зелёных тестов: `docker compose -f docker-compose.prod.yml config` и сборка образов.

## 2. Документация

- [x] 2.1 Добавить в README раздел о CI и локальном воспроизведении проверок.

## 3. Проверка

- [x] 3.1 Локально: `docker compose -f docker-compose.prod.yml config` с тестовым паролем, сборка и тесты.
- [x] 3.2 `openspec validate add-ci-pipeline`.