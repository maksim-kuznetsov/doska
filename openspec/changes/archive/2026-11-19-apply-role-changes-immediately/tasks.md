# Tasks

## 1. Проверка сессии

- [x] 1.1 Добавить в cookie-аутентификацию `OnValidatePrincipal`: загрузка пользователя по `sub`, отказ при отсутствии/блокировке, замена principal актуальными `email` и `role`. Проверка: `dotnet build`.
- [x] 1.2 Убедиться, что заблокированный пользователь теряет cookie и получает 401. Проверка: API-тест.

## 2. Тесты

- [x] 2.1 Тест: повышение роли в активной сессии сразу открывает админский эндпоинт, понижение — сразу закрывает. Проверка: `Doska.Tests.exe`.
- [x] 2.2 Тест: блокировка активной сессии даёт 401 без повторного входа.

## 3. Проверка

- [x] 3.1 `dotnet build backend/Doska.sln`, бэкенд-тесты, `pnpm --dir frontend test`, `pnpm --dir frontend build`.
- [x] 3.2 `openspec validate apply-role-changes-immediately`.